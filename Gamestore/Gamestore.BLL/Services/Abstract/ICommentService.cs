using Gamestore.BLL.DTOs.Comments;

namespace Gamestore.BLL.Services.Abstract;

public interface ICommentService
{
    Task AddAsync(string gameKey, CreateCommentDto dto);
}