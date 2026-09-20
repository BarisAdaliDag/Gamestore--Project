using Gamestore.BLL.DTOs.Orders;

namespace Gamestore.BLL.Services.Abstract;

public interface IPaymentService
{
    Task<byte[]> ProcessBankPaymentAsync();

    Task<PaymentResultDto> ProcessIBoxPaymentAsync();

    Task ProcessVisaPaymentAsync(VisaPaymentModelDto visaModel);
}
