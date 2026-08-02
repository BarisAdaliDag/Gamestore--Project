using Gamestore.BLL.DTOs.Orders;

namespace Gamestore.BLL.Services.Abstract;

public interface IOrderService
{
    Task AddToCartAsync(string gameKey);

    Task RemoveFromCartAsync(string gameKey);

    Task<IEnumerable<OrderGameDto>> GetCartAsync();

    Task<IEnumerable<OrderDto>> GetOrdersAsync();

    Task<OrderDto> GetOrderByIdAsync(Guid id);

    Task<IEnumerable<OrderGameDto>> GetOrderDetailsAsync(Guid orderId);

    Task<IEnumerable<PaymentMethodDto>> GetPaymentMethodsAsync();

    Task<OrderCheckoutDto> GetOpenOrderForPaymentAsync();

    Task MarkOrderAsPaidAsync(Guid orderId);

    Task MarkOrderAsCancelledAsync(Guid orderId);

    double CalculateOrderTotal(IEnumerable<OrderGameDto> items);
}
