namespace Gamestore.BLL.DTOs.Orders;

/// <summary>
/// Response body returned by the payment microservice for a successful IBox transaction.
/// Used to validate that the payment was actually accepted (NFR2).
/// </summary>
public class BoxTransactionResultDto
{
    public Guid AccountNumber { get; set; }

    public Guid InvoiceNumber { get; set; }

    public int PaymentMethod { get; set; }

    public Guid AccountId { get; set; }

    public double Amount { get; set; }
}
