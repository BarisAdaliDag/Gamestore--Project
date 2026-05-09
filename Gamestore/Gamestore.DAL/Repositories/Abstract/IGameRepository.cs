using Gamestore.Domain.Entities;

namespace Gamestore.DAL.Repositories.Abstract;

public interface IGameRepository : IRepository<Game>
{
    Task<Game?> GetByKeyAsync(string key);

    Task<Game?> GetByIdWithDetailsAsync(Guid id);

    Task<Game?> GetByKeyWithDetailsAsync(string key);

    Task<IEnumerable<Game>> GetByGenreIdAsync(Guid genreId);

    Task<IEnumerable<Game>> GetByPlatformIdAsync(Guid platformId);
}