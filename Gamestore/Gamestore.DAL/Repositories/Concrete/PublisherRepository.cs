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
}
