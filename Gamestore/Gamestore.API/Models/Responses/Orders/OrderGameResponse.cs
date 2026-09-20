namespace Gamestore.API.Models.Responses.Orders;

public class OrderGameResponse
{
    public Guid ProductId { get; set; }

    public double Price { get; set; }

    public int Quantity { get; set; }

    public int? Discount { get; set; }
}
