using Gamestore.Domain.Entities;

namespace Gamestore.DAL.Repositories.Abstract;

public interface ICommentRepository : IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetByGameIdAsync(Guid gameId);
}