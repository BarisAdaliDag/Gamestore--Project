using Gamestore.DAL.Context;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.Domain.Entities;
using Gamestore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.DAL.Repositories.Concrete;

public class OrderRepository(GamestoreDbContext context)
    : Repository<Order>(context), IOrderRepository
{
    public async Task<Order?> GetOpenOrderByCustomerIdAsync(Guid customerId)
    {
        return await DbSet
            .Include(o => o.OrderGames)
            .FirstOrDefaultAsync(o => o.CustomerId == customerId
                                      && o.Status == OrderStatus.Open);
    }

    public async Task<Order?> GetByIdWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(o => o.OrderGames)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetByStatusesAsync(params OrderStatus[] statuses)
    {
        return await DbSet
            .Where(o => statuses.Contains(o.Status))
            .ToListAsync();
    }
}
