using Gamestore.Domain.Entities;
using Gamestore.Domain.Enums;

namespace Gamestore.DAL.Repositories.Abstract;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetOpenOrderByCustomerIdAsync(Guid customerId);

    Task<Order?> GetByIdWithDetailsAsync(Guid id);

    Task<IEnumerable<Order>> GetByStatusesAsync(params OrderStatus[] statuses);
}
