using Gamestore.DAL.Context;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.DAL.Repositories.Concrete;

public class GenreRepository(GamestoreDbContext context) : Repository<Genre>(context), IGenreRepository
{
    public async Task<IEnumerable<Genre>> GetByParentIdAsync(Guid parentId)
    {
        return await DbSet
            .Where(g => g.ParentGenreId == parentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Genre>> GetByGameKeyAsync(string gameKey)
    {
        return await DbSet
            .Where(g => g.GameGenres.Any(gg => gg.Game.Key == gameKey))
            .ToListAsync();
    }
}