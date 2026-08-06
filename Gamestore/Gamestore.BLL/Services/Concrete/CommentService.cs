using Gamestore.BLL.Constants;
using Gamestore.BLL.DTOs.Comments;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Helpers;
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

    public async Task<IEnumerable<CommentDto>> GetByGameKeyAsync(string gameKey)
    {
        _logger.LogInformation("Getting comments for game '{GameKey}'", gameKey);

        var game = await _unitOfWork.Games.GetByKeyAsync(gameKey)
            ?? throw new NotFoundException(nameof(Game), gameKey);

        var comments = (await _unitOfWork.Comments.GetByGameIdAsync(game.Id)).ToList();

        var tree = BuildTree(comments);

        _logger.LogInformation(
            "Found {Total} comments ({RootCount} root) for game '{GameKey}'",
            comments.Count,
            tree.Count,
            gameKey);

        return tree;
    }

    public async Task DeleteAsync(string gameKey, Guid commentId)
    {
        _logger.LogInformation(
            "Deleting comment {CommentId} from game '{GameKey}'",
            commentId,
            gameKey);

        var game = await _unitOfWork.Games.GetByKeyAsync(gameKey)
            ?? throw new NotFoundException(nameof(Game), gameKey);

        // IMPORTANT: GetByIdAsync (FindAsync) returns a TRACKED entity.
        // GetByGameIdAsync uses AsNoTracking, so it cannot be used for write operations.
        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId)
            ?? throw new NotFoundException(nameof(Comment), commentId);

        // If the two resources in the URL are inconsistent -> 404 (comment does not belong to this game)
        if (comment.GameId != game.Id)
        {
            _logger.LogWarning(
                "Comment {CommentId} does not belong to game '{GameKey}'",
                commentId,
                gameKey);
            throw new NotFoundException(nameof(Comment), commentId);
        }

        // Idempotency: the requested final state is already achieved -> successful no-op
        if (comment.IsDeleted)
        {
            _logger.LogInformation(
                "Comment {CommentId} is already deleted, no action taken",
                commentId);
            return;
        }

        // SOFT delete: the row is not removed. Child comments are preserved and this
        // comment's body becomes a "deleted" message during read (see ComposeBody).
        comment.IsDeleted = true;

        _unitOfWork.Comments.Update(comment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Comment {CommentId} soft-deleted", commentId);
    }

    public async Task BanAsync(BanUserDto dto)
    {
        var userName = dto.User.Trim();

        _logger.LogInformation(
            "Banning user '{User}' for '{Duration}'",
            userName,
            dto.Duration);

        // Defense layer: the validator already checks this, but the service should also
        // validate at its own boundary (it may be called from another caller).
        if (!BanDurationHelper.IsSupported(dto.Duration))
        {
            throw new ValidationException($"Unsupported ban duration: '{dto.Duration}'.");
        }

        var bannedUntil = BanDurationHelper.Resolve(dto.Duration, DateTime.UtcNow);

        // UPSERT. NOTE: a check-then-act race is theoretically possible; in that case
        // the IX_Bans_Name UNIQUE index rejects the second INSERT (the request gets 500,
        // but a duplicate record CANNOT be created). Since this is a moderation operation,
        // this is accepted.
        var existing = await _unitOfWork.Bans.GetSingleAsync(b => b.Name == userName);

        if (existing is null)
        {
            await _unitOfWork.Bans.AddAsync(new Ban
            {
                Id = Guid.NewGuid(),
                Name = userName,
                BannedUntil = bannedUntil,
            });
        }
        else
        {
            existing.BannedUntil = bannedUntil;
            _unitOfWork.Bans.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "User '{User}' banned until {BannedUntil}",
            userName,
            bannedUntil?.ToString("u") ?? BanDurationHelper.Permanent);
    }

    public IReadOnlyList<string> GetBanDurations()
    {
        // No I/O -> NOT async. Single source of truth: BanDurationHelper.
        return BanDurationHelper.GetDurations();
    }

    /// <summary>
    /// Converts a flat list into a tree with a single query + O(n).
    /// No recursion is used: depth is unbounded, so there should be no stack overflow risk.
    /// NOTE: Circular references are currently impossible (parent is always older and there
    /// is no comment update endpoint). If parent reassignment is added in the future, cycle
    /// detection will be needed here - otherwise the affected comments may be dropped from
    /// the response.
    /// </summary>
    private List<CommentDto> BuildTree(List<Comment> comments)
    {
        // For accessing parent's Name/Body: O(1) lookup
        var entityById = comments.ToDictionary(c => c.Id);

        // Pass 1 - create all nodes (Body is calculated here)
        var dtoById = new Dictionary<Guid, CommentDto>(comments.Count);
        foreach (var comment in comments)
        {
            dtoById[comment.Id] = new CommentDto
            {
                Id = comment.Id,
                Name = comment.Name,
                Body = ComposeBody(comment, entityById),
            };
        }

        // Pass 2 - connect edges
        var roots = new List<CommentDto>();
        foreach (var comment in comments)
        {
            var dto = dtoById[comment.Id];

            if (comment.ParentCommentId is null)
            {
                roots.Add(dto);
                continue;
            }

            if (dtoById.TryGetValue(comment.ParentCommentId.Value, out var parentDto))
            {
                parentDto.ChildComments.Add(dto);
                continue;
            }

            // Orphan: should not happen per invariant. We log and show it as a root.
            _logger.LogWarning(
                "Orphan comment detected: {CommentId} references missing parent {ParentId}",
                comment.Id,
                comment.ParentCommentId);

            roots.Add(dto);
        }

        return roots;
    }

    private static string ComposeBody(Comment comment, Dictionary<Guid, Comment> entityById)
    {
        // Rule 1: Deleted comment -> Action is not checked
        if (comment.IsDeleted)
        {
            return CommentConstants.DeletedCommentMessage;
        }

        // Rule 2: Plain comment
        if (comment.Action == CommentAction.None || comment.ParentCommentId is null)
        {
            return comment.Body;
        }

        entityById.TryGetValue(comment.ParentCommentId.Value, out var parent);

        var prefix = comment.Action switch
        {
            CommentAction.Reply => ResolveParentName(parent),
            CommentAction.Quote => ResolveParentBody(parent),
            CommentAction.None => null,
            _ => null,
        };

        return prefix is null
            ? comment.Body
            : $"[{prefix}], {comment.Body}";
    }

    private static string? ResolveParentName(Comment? parent)
    {
        // If parent is missing (corrupt data) -> behave as if deleted.
        // If parent IS DELETED, its name is preserved: deletion hides content, not identity
        // (in the US2 response, the deleted comment's own 'name' field also remains visible).
        // If the mentor says "the message should also be visible in replies": add a parent.IsDeleted check.
        return parent is null
            ? CommentConstants.DeletedCommentMessage
            : parent.Name;
    }

    private static string? ResolveParentBody(Comment? parent)
    {
        // If parent is missing OR deleted -> content is not shown.
        // parent.Body is the RAW text (the parent's own prefix is not included) -> prevents
        // text from growing exponentially in nested quotes.
        return parent is null || parent.IsDeleted
            ? CommentConstants.DeletedCommentMessage
            : parent.Body;
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