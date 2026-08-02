using System.Linq.Expressions;
using Gamestore.BLL.Constants;
using Gamestore.BLL.DTOs.Comments;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using Gamestore.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gamestore.Tests.Services;

public class CommentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ILogger<CommentService>> _mockLogger = new();
    private readonly Mock<IGameRepository> _mockGameRepository = new();
    private readonly Mock<ICommentRepository> _mockCommentRepository = new();
    private readonly Mock<IRepository<Ban>> _mockBanRepository = new();
    private readonly CommentService _service;

    public CommentServiceTests()
    {
        _mockUnitOfWork.Setup(u => u.Games).Returns(_mockGameRepository.Object);
        _mockUnitOfWork.Setup(u => u.Comments).Returns(_mockCommentRepository.Object);
        _mockUnitOfWork.Setup(u => u.Bans).Returns(_mockBanRepository.Object);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _service = new CommentService(_mockUnitOfWork.Object, _mockLogger.Object);
    }

    // === AddAsync Tests ===
    [Fact]
    public async Task AddAsyncShouldThrowNotFoundExceptionWhenGameDoesNotExist()
    {
        var dto = new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto { Name = "Ali", Body = "Hello" },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("nonexistent"))
            .ReturnsAsync((Game?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.AddAsync("nonexistent", dto));

        _mockCommentRepository.Verify(r => r.AddAsync(It.IsAny<Comment>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddAsyncShouldThrowForbiddenExceptionWhenUserIsBanned()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var dto = new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto { Name = "BannedUser", Body = "Hello" },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync(new Ban { Name = "BannedUser", BannedUntil = DateTime.UtcNow.AddHours(1) });

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.AddAsync("test", dto));

        _mockCommentRepository.Verify(r => r.AddAsync(It.IsAny<Comment>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddAsyncShouldThrowForbiddenExceptionWhenUserIsPermanentlyBanned()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var dto = new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto { Name = "PermBanned", Body = "Hello" },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync(new Ban { Name = "PermBanned", BannedUntil = null });

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.AddAsync("test", dto));
    }

    [Fact]
    public async Task AddAsyncShouldNotThrowWhenBanIsExpired()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var dto = new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto { Name = "ExpiredBan", Body = "Hello" },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync(new Ban { Name = "ExpiredBan", BannedUntil = DateTime.UtcNow.AddHours(-1) });

        await _service.AddAsync("test", dto);

        _mockCommentRepository.Verify(r => r.AddAsync(It.IsAny<Comment>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddAsyncShouldThrowValidationExceptionWhenReplyHasNoParentId()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var dto = new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto { Name = "Ali", Body = "Hello" },
            Action = CommentAction.Reply,
            ParentId = null,
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync((Ban?)null);

        await Assert.ThrowsAsync<ValidationException>(() => _service.AddAsync("test", dto));

        _mockCommentRepository.Verify(r => r.AddAsync(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task AddAsyncShouldThrowValidationExceptionWhenParentNotFound()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var parentId = Guid.NewGuid();
        var dto = new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto { Name = "Ali", Body = "Hello" },
            Action = CommentAction.Reply,
            ParentId = parentId,
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync((Ban?)null);
        _mockCommentRepository.Setup(r => r.GetByIdAsync(parentId))
            .ReturnsAsync((Comment?)null);

        await Assert.ThrowsAsync<ValidationException>(() => _service.AddAsync("test", dto));
    }

    [Fact]
    public async Task AddAsyncShouldThrowValidationExceptionWhenParentBelongsToDifferentGame()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var parentId = Guid.NewGuid();
        var dto = new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto { Name = "Ali", Body = "Hello" },
            Action = CommentAction.Reply,
            ParentId = parentId,
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync((Ban?)null);
        _mockCommentRepository.Setup(r => r.GetByIdAsync(parentId))
            .ReturnsAsync(new Comment { Id = parentId, Name = "Other", Body = "x", GameId = Guid.NewGuid() });

        await Assert.ThrowsAsync<ValidationException>(() => _service.AddAsync("test", dto));
    }

    [Fact]
    public async Task AddAsyncShouldCreateCommentWhenValid()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var dto = new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto { Name = "Ali", Body = "Hello" },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync((Ban?)null);

        Comment? addedComment = null;
        _mockCommentRepository.Setup(r => r.AddAsync(It.IsAny<Comment>()))
            .Callback<Comment>(c => addedComment = c)
            .Returns(Task.CompletedTask);

        await _service.AddAsync("test", dto);

        Assert.NotNull(addedComment);
        Assert.Equal("Ali", addedComment.Name);
        Assert.Equal("Hello", addedComment.Body);
        Assert.Equal(game.Id, addedComment.GameId);
        Assert.False(addedComment.IsDeleted);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddAsyncShouldTrimNameAndBody()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var dto = new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto { Name = "  Ali  ", Body = "  Hello  " },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync((Ban?)null);

        Comment? addedComment = null;
        _mockCommentRepository.Setup(r => r.AddAsync(It.IsAny<Comment>()))
            .Callback<Comment>(c => addedComment = c)
            .Returns(Task.CompletedTask);

        await _service.AddAsync("test", dto);

        Assert.NotNull(addedComment);
        Assert.Equal("Ali", addedComment.Name);
        Assert.Equal("Hello", addedComment.Body);
    }

    // === GetByGameKeyAsync Tests ===
    [Fact]
    public async Task GetByGameKeyAsyncShouldThrowNotFoundExceptionWhenGameDoesNotExist()
    {
        _mockGameRepository.Setup(r => r.GetByKeyAsync("nonexistent"))
            .ReturnsAsync((Game?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByGameKeyAsync("nonexistent"));
    }

    [Fact]
    public async Task GetByGameKeyAsyncShouldReturnEmptyListWhenNoComments()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByGameIdAsync(game.Id))
            .ReturnsAsync([]);

        var result = await _service.GetByGameKeyAsync("test");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByGameKeyAsyncShouldBuildTreeWithRootAndChild()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var comments = new List<Comment>
        {
            new() { Id = rootId, Name = "Ali", Body = "Harika", GameId = game.Id, ParentCommentId = null, Action = CommentAction.None, IsDeleted = false },
            new() { Id = childId, Name = "Veli", Body = "Katiliyorum", GameId = game.Id, ParentCommentId = rootId, Action = CommentAction.Reply, IsDeleted = false },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByGameIdAsync(game.Id))
            .ReturnsAsync(comments);

        var result = (await _service.GetByGameKeyAsync("test")).ToList();

        Assert.Single(result);
        var root = result[0];
        Assert.Equal("Ali", root.Name);
        Assert.Equal("Harika", root.Body);
        Assert.Single(root.ChildComments);
        var child = root.ChildComments.First();
        Assert.Equal("Veli", child.Name);
        Assert.Equal("[Ali], Katiliyorum", child.Body);
    }

    [Fact]
    public async Task GetByGameKeyAsyncShouldShowDeletedMessageForDeletedComment()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var commentId = Guid.NewGuid();

        var comments = new List<Comment>
        {
            new() { Id = commentId, Name = "Ali", Body = "Harika", GameId = game.Id, IsDeleted = true },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByGameIdAsync(game.Id))
            .ReturnsAsync(comments);

        var result = (await _service.GetByGameKeyAsync("test")).ToList();

        Assert.Single(result);
        Assert.Equal("Ali", result[0].Name);
        Assert.Equal(CommentConstants.DeletedCommentMessage, result[0].Body);
    }

    [Fact]
    public async Task GetByGameKeyAsyncQuoteShouldUseParentRawBody()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var comments = new List<Comment>
        {
            new() { Id = parentId, Name = "Veli", Body = "Katiliyorum", GameId = game.Id, Action = CommentAction.Reply, IsDeleted = false },
            new() { Id = childId, Name = "Ayse", Body = "Bence de", GameId = game.Id, ParentCommentId = parentId, Action = CommentAction.Quote, IsDeleted = false },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByGameIdAsync(game.Id))
            .ReturnsAsync(comments);

        var result = (await _service.GetByGameKeyAsync("test")).ToList();

        Assert.Single(result);
        var child = result[0].ChildComments.First();
        Assert.Equal("[Katiliyorum], Bence de", child.Body);
    }

    [Fact]
    public async Task GetByGameKeyAsyncQuoteOfDeletedParentShouldShowDeletedMessage()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var comments = new List<Comment>
        {
            new() { Id = parentId, Name = "Veli", Body = "Katiliyorum", GameId = game.Id, IsDeleted = true },
            new() { Id = childId, Name = "Ayse", Body = "Bence de", GameId = game.Id, ParentCommentId = parentId, Action = CommentAction.Quote, IsDeleted = false },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByGameIdAsync(game.Id))
            .ReturnsAsync(comments);

        var result = (await _service.GetByGameKeyAsync("test")).ToList();

        var child = result[0].ChildComments.First();
        Assert.Equal($"[{CommentConstants.DeletedCommentMessage}], Bence de", child.Body);
    }

    [Fact]
    public async Task GetByGameKeyAsyncReplyToDeletedParentShouldPreserveParentName()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var comments = new List<Comment>
        {
            new() { Id = parentId, Name = "Ali", Body = "Harika", GameId = game.Id, IsDeleted = true },
            new() { Id = childId, Name = "Veli", Body = "Katiliyorum", GameId = game.Id, ParentCommentId = parentId, Action = CommentAction.Reply, IsDeleted = false },
        };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByGameIdAsync(game.Id))
            .ReturnsAsync(comments);

        var result = (await _service.GetByGameKeyAsync("test")).ToList();

        var child = result[0].ChildComments.First();
        Assert.Equal("[Ali], Katiliyorum", child.Body);
    }

    // === DeleteAsync Tests ===
    [Fact]
    public async Task DeleteAsyncShouldThrowNotFoundExceptionWhenGameDoesNotExist()
    {
        _mockGameRepository.Setup(r => r.GetByKeyAsync("nonexistent"))
            .ReturnsAsync((Game?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.DeleteAsync("nonexistent", Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsyncShouldThrowNotFoundExceptionWhenCommentDoesNotExist()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var commentId = Guid.NewGuid();

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync((Comment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync("test", commentId));
    }

    [Fact]
    public async Task DeleteAsyncShouldThrowNotFoundExceptionWhenCommentBelongsToDifferentGame()
    {
        var game = new Game { Id = Guid.NewGuid(), Name = "Test", Key = "test" };
        var commentId = Guid.NewGuid();

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(new Comment { Id = commentId, Name = "A", Body = "B", GameId = Guid.NewGuid() });

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync("test", commentId));
    }

    [Fact]
    public async Task DeleteAsyncShouldBeIdempotentWhenAlreadyDeleted()
    {
        var gameId = Guid.NewGuid();
        var game = new Game { Id = gameId, Name = "Test", Key = "test" };
        var commentId = Guid.NewGuid();

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(new Comment { Id = commentId, Name = "Ali", Body = "Hello", GameId = gameId, IsDeleted = true });

        await _service.DeleteAsync("test", commentId);

        _mockCommentRepository.Verify(r => r.Update(It.IsAny<Comment>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsyncShouldSoftDeleteComment()
    {
        var gameId = Guid.NewGuid();
        var game = new Game { Id = gameId, Name = "Test", Key = "test" };
        var commentId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, GameId = gameId, Name = "Ali", Body = "Hello", IsDeleted = false };

        _mockGameRepository.Setup(r => r.GetByKeyAsync("test")).ReturnsAsync(game);
        _mockCommentRepository.Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        await _service.DeleteAsync("test", commentId);

        Assert.True(comment.IsDeleted);
        _mockCommentRepository.Verify(r => r.Update(comment), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // === BanAsync Tests ===
    [Fact]
    public async Task BanAsyncShouldThrowValidationExceptionWhenDurationIsUnsupported()
    {
        var dto = new BanUserDto { User = "Ali", Duration = "2 years" };

        await Assert.ThrowsAsync<ValidationException>(() => _service.BanAsync(dto));

        _mockBanRepository.Verify(r => r.AddAsync(It.IsAny<Ban>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task BanAsyncShouldInsertNewBanWhenUserNotBanned()
    {
        var dto = new BanUserDto { User = "Ali", Duration = "1 hour" };

        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync((Ban?)null);

        Ban? addedBan = null;
        _mockBanRepository.Setup(r => r.AddAsync(It.IsAny<Ban>()))
            .Callback<Ban>(b => addedBan = b)
            .Returns(Task.CompletedTask);

        await _service.BanAsync(dto);

        Assert.NotNull(addedBan);
        Assert.Equal("Ali", addedBan.Name);
        Assert.NotNull(addedBan.BannedUntil);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task BanAsyncShouldUpdateExistingBanWhenUserAlreadyBanned()
    {
        var dto = new BanUserDto { User = "Ali", Duration = "permanent" };
        var existingBan = new Ban { Id = Guid.NewGuid(), Name = "Ali", BannedUntil = DateTime.UtcNow.AddHours(1) };

        _mockBanRepository.Setup(r => r.GetSingleAsync(It.IsAny<Expression<Func<Ban, bool>>>()))
            .ReturnsAsync(existingBan);

        await _service.BanAsync(dto);

        Assert.Null(existingBan.BannedUntil);
        _mockBanRepository.Verify(r => r.Update(existingBan), Times.Once);
        _mockBanRepository.Verify(r => r.AddAsync(It.IsAny<Ban>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // === GetBanDurations Tests ===
    [Fact]
    public void GetBanDurationsShouldReturnFiveDurationsInOrder()
    {
        var result = _service.GetBanDurations();

        Assert.Equal(5, result.Count);
        Assert.Equal("1 hour", result[0]);
        Assert.Equal("1 day", result[1]);
        Assert.Equal("1 week", result[2]);
        Assert.Equal("1 month", result[3]);
        Assert.Equal("permanent", result[4]);
    }
}