namespace Gamestore.API.Models.Responses.Orders;

public class PaymentResultResponse
{
    public Guid UserId { get; set; }

    public Guid OrderId { get; set; }

    public DateTime PaymentDate { get; set; }

    public double Sum { get; set; }
}
