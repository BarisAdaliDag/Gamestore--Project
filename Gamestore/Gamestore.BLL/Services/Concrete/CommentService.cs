using Gamestore.BLL.DTOs.Comments;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using Gamestore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Gamestore.BLL.Services.Concrete;

public class CommentService(
    IUnitOfWork unitOfWork,
    ILogger<CommentService> logger)
    : ICommentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CommentService> _logger = logger;

    public async Task AddAsync(string gameKey, CreateCommentDto dto)
    {
        _logger.LogInformation(
            "Adding comment to game '{GameKey}' by '{Name}' (Action: {Action}, ParentId: {ParentId})",
            gameKey,
            dto.Comment.Name,
            dto.Action,
            dto.ParentId);

        // 1) Does the resource exist? -> 404
        var game = await _unitOfWork.Games.GetByKeyAsync(gameKey)
            ?? throw new NotFoundException(nameof(Game), gameKey);

        var authorName = dto.Comment.Name.Trim();

        // 2) Authorization: is the user banned? -> 403
        await EnsureUserIsNotBannedAsync(authorName);

        // 3) Are Action and ParentId consistent? -> 400 (no query, in-memory)
        if (dto.Action != CommentAction.None && dto.ParentId is null)
        {
            throw new ValidationException(
                $"ParentId is required when action is '{dto.Action}'.");
        }

        // 4) Is the parent valid (exists + SAME game)? -> 400
        await ValidateParentAsync(dto.ParentId, game.Id);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Name = authorName,
            Body = dto.Comment.Body.Trim(),
            GameId = game.Id,
            ParentCommentId = dto.ParentId,
            Action = dto.Action,
            IsDeleted = false,
        };

        await _unitOfWork.Comments.AddAsync(comment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Comment added: {CommentId} to game {GameId}", comment.Id, game.Id);
    }

    private static bool IsBanActive(Ban? ban, DateTime utcNow)
    {
        // BannedUntil == null => permanent
        return ban is not null && (ban.BannedUntil is null || ban.BannedUntil > utcNow);
    }

    private async Task EnsureUserIsNotBannedAsync(string authorName)
    {
        var ban = await _unitOfWork.Bans.GetSingleAsync(b => b.Name == authorName);

        if (!IsBanActive(ban, DateTime.UtcNow))
        {
            return;
        }

        _logger.LogWarning(
            "Comment rejected - user '{Name}' is banned until {BannedUntil}",
            authorName,
            ban.BannedUntil?.ToString("u") ?? "permanent");

        throw new ForbiddenException($"User '{authorName}' is banned from commenting.");
    }

    private async Task ValidateParentAsync(Guid? parentId, Guid gameId)
    {
        if (parentId is null)
        {
            return;
        }

        var parent = await _unitOfWork.Comments.GetByIdAsync(parentId.Value)
            ?? throw new ValidationException($"Parent comment with id '{parentId}' not found.");

        if (parent.GameId != gameId)
        {
            throw new ValidationException(
                "Parent comment belongs to a different game.");
        }
    }
}