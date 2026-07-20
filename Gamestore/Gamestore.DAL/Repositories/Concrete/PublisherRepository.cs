using Gamestore.DAL.Context;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.DAL.Repositories.Concrete;

public class PublisherRepository(GamestoreDbContext context) : Repository<Publisher>(context), IPublisherRepository
{
    public async Task<Publisher?> GetByCompanyNameAsync(string companyName)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.CompanyName == companyName);
    }

    public async Task<Publisher?> GetByGameKeyAsync(string gameKey)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.Games.Any(g => g.Key == gameKey));
    }

    public async Task<bool> HasGamesAsync(Guid publisherId)
    {
        return await DbSet
            .AsNoTracking()
            .AnyAsync(p => p.Id == publisherId && p.Games.Any());
    }
}
