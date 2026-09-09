using Gamestore.BLL.DTOs.Abstract;

namespace Gamestore.BLL.DTOs.Orders;

public class OrderDto : IDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public DateTime? Date { get; set; }
}
