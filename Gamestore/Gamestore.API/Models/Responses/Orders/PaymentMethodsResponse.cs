namespace Gamestore.API.Models.Responses.Orders;

public class PaymentMethodsResponse
{
    public IEnumerable<PaymentMethodResponse> PaymentMethods { get; set; } = [];
}
