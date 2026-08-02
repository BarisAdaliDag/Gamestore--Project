namespace Gamestore.API.Models.Requests.Orders;

public class VisaPaymentModel
{
    public required string Holder { get; set; }

    public required string CardNumber { get; set; }

    public int MonthExpire { get; set; }

    public int YearExpire { get; set; }

    public int Cvv2 { get; set; }
}
