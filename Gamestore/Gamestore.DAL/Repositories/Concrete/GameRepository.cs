using Gamestore.DAL.Context;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.DAL.Repositories.Concrete;

public class GameRepository(GamestoreDbContext context) : Repository<Game>(context), IGameRepository
{
    public IQueryable<Game> Query()
    {
        return DbSet.AsNoTracking();
    }

    public async Task<IEnumerable<Game>> GetAllWithDetailsAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Include(game => game.GameGenres)
            .Include(game => game.GamePlatforms)
            .Include(game => game.Comments)
            .ToListAsync();
    }

    public async Task<Game?> GetByKeyAsync(string key)
    {
        return await DbSet.FirstOrDefaultAsync(g => g.Key == key);
    }

    public Task IncrementViewCountAsync(Guid gameId)
    {
        // Atomic increment: SQL "SET ViewCount = ViewCount + 1"
        // We are NOT doing a read-modify-write → no lost updates under concurrent requests.
        return DbSet
            .Where(g => g.Id == gameId)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.ViewCount, g => g.ViewCount + 1));
    }

    public async Task<Game?> GetByIdWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(g => g.GameGenres)
            .Include(g => g.GamePlatforms)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Game?> GetByKeyWithDetailsAsync(string key)
    {
        return await DbSet
            .Include(g => g.GameGenres)
            .Include(g => g.GamePlatforms)
            .FirstOrDefaultAsync(g => g.Key == key);
    }

    public async Task<(IEnumerable<Game> Games, int TotalCount)> GetFilteredPagedAsync(
        int page,
        int size,
        string? name = null,
        Guid? genreId = null,
        Guid? platformId = null)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(g => g.Name.Contains(name));
        }

        if (genreId.HasValue)
        {
            query = query.Where(g => g.GameGenres.Any(gg => gg.GenreId == genreId.Value));
        }

        if (platformId.HasValue)
        {
            query = query.Where(g => g.GamePlatforms.Any(gp => gp.PlatformId == platformId.Value));
        }

        var totalCount = await query.CountAsync();

        var games = await query
            .OrderBy(g => g.Name)
            .Skip(size * (page - 1))
            .Take(size)
            .ToListAsync();

        return (games, totalCount);
    }

    public async Task<IEnumerable<Game>> GetByGenreIdAsync(Guid genreId)
    {
        return await DbSet
            .Where(g => g.GameGenres.Any(gg => gg.GenreId == genreId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetByPlatformIdAsync(Guid platformId)
    {
        return await DbSet
            .Where(g => g.GamePlatforms.Any(gp => gp.PlatformId == platformId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetByPublisherNameAsync(string companyName)
    {
        return await DbSet
            .Where(g => g.Publisher != null && g.Publisher.CompanyName == companyName)
            .ToListAsync();
    }
}
