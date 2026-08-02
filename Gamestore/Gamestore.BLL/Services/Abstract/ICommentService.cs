using Gamestore.BLL.DTOs.Comments;

namespace Gamestore.BLL.Services.Abstract;

public interface ICommentService
{
    Task AddAsync(string gameKey, CreateCommentDto dto);

    Task<IEnumerable<CommentDto>> GetByGameKeyAsync(string gameKey);

    Task DeleteAsync(string gameKey, Guid commentId);

    Task BanAsync(BanUserDto dto);

    IReadOnlyList<string> GetBanDurations();
}
