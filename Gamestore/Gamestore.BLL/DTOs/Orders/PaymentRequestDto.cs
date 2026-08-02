namespace Gamestore.BLL.DTOs.Orders;

public class PaymentRequestDto
{
    public required string Method { get; set; }

    public VisaPaymentModelDto? Model { get; set; }
}
