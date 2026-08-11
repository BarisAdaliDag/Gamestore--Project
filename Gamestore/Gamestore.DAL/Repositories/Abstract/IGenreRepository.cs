using Gamestore.Domain.Entities;

namespace Gamestore.DAL.Repositories.Abstract;

public interface IGenreRepository : IRepository<Genre>
{
    Task<IEnumerable<Genre>> GetByParentIdAsync(Guid parentId);

    Task<IEnumerable<Genre>> GetByGameKeyAsync(string gameKey);
}