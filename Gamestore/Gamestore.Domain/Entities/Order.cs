using Gamestore.Domain.Enums;

namespace Gamestore.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    public DateTime? Date { get; set; }

    public Guid CustomerId { get; set; }

    public OrderStatus Status { get; set; }

    public ICollection<OrderGame> OrderGames { get; set; } = [];
}
