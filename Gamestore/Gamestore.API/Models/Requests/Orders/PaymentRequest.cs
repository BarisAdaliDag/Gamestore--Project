namespace Gamestore.API.Models.Requests.Orders;

public class PaymentRequest
{
    public required string Method { get; set; }

    public VisaPaymentModel? Model { get; set; }
}
