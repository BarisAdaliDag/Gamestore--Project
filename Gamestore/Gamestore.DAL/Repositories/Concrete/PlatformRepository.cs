using Gamestore.DAL.Context;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.DAL.Repositories.Concrete;

public class PlatformRepository(GamestoreDbContext context) : Repository<Platform>(context), IPlatformRepository
{
    public async Task<IEnumerable<Platform>> GetByGameKeyAsync(string gameKey)
    {
        return await DbSet
            .Where(p => p.GamePlatforms.Any(gp => gp.Game.Key == gameKey))
            .ToListAsync();
    }
}