using Gamestore.Domain.Entities;

namespace Gamestore.DAL.Repositories.Abstract;

public interface IGameRepository : IRepository<Game>
{
    /// <summary>
    /// Filtreleme/siralama/sayfalama icin temel sorgu.
    /// Henuz veritabanina gidilmez - sorgu ustune eklemeler yapilabilir.
    /// </summary>
    IQueryable<Game> Query();

    Task<IEnumerable<Game>> GetAllWithDetailsAsync();

    new Task<int> CountAsync();

    new Task<Game?> GetByIdAsync(Guid id);

    Task<Game?> GetByKeyAsync(string key);

    Task<bool> IsDeletedAsync(string key);

    Task<bool> IsDeletedAsync(Guid id);

    Task<IReadOnlyCollection<string>> GetDeletedNorthwindKeysAsync();

    Task IncrementViewCountAsync(Guid gameId);

    Task<Game?> GetByIdWithDetailsAsync(Guid id);

    Task<Game?> GetByKeyWithDetailsAsync(string key);

    Task<(IEnumerable<Game> Games, int TotalCount)> GetFilteredPagedAsync(
        int page,
        int size,
        string? name = null,
        Guid? genreId = null,
        Guid? platformId = null);

    Task<IEnumerable<Game>> GetByGenreIdAsync(Guid genreId);

    Task<IEnumerable<Game>> GetByPlatformIdAsync(Guid platformId);

    Task<IEnumerable<Game>> GetByPublisherNameAsync(string companyName);
}
