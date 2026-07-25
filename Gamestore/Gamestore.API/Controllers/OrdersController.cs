using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Orders;
using Gamestore.API.Models.Responses.Orders;
using Gamestore.BLL.DTOs.Orders;
using Gamestore.BLL.Services.Abstract;
using Gamestore.Domain.Constants;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController(
    IOrderService orderService,
    IPaymentService paymentService,
    IMapper mapper) : ControllerBase
{
    [HttpGet("cart")]
    public async Task<ActionResult<IEnumerable<OrderGameResponse>>> GetCart()
    {
        var dtos = await orderService.GetCartAsync();
        return Ok(mapper.Map<IEnumerable<OrderGameResponse>>(dtos));
    }

    [HttpDelete("cart/{key}")]
    public async Task<IActionResult> RemoveFromCart(string key)
    {
        await orderService.RemoveFromCartAsync(key);
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetOrders()
    {
        var dtos = await orderService.GetOrdersAsync();
        return Ok(mapper.Map<IEnumerable<OrderResponse>>(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(Guid id)
    {
        var dto = await orderService.GetOrderByIdAsync(id);
        return Ok(mapper.Map<OrderResponse>(dto));
    }

    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<IEnumerable<OrderGameResponse>>> GetOrderDetails(Guid id)
    {
        var dtos = await orderService.GetOrderDetailsAsync(id);
        return Ok(mapper.Map<IEnumerable<OrderGameResponse>>(dtos));
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<PaymentMethodsResponse>> GetPaymentMethods()
    {
        var dtos = await orderService.GetPaymentMethodsAsync();
        var response = new PaymentMethodsResponse
        {
            PaymentMethods = mapper.Map<IEnumerable<PaymentMethodResponse>>(dtos),
        };

        return Ok(response);
    }

    [HttpPost("payment")]
    [ServiceFilter(typeof(ValidationFilter<PaymentRequest>))]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        if (request.Method == PaymentMethods.Bank)
        {
            var pdfBytes = await paymentService.ProcessBankPaymentAsync();
            return File(
                pdfBytes,
                "application/pdf",
                $"Invoice_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        }

        if (request.Method == PaymentMethods.IBoxTerminal)
        {
            var result = await paymentService.ProcessIBoxPaymentAsync();
            return Ok(mapper.Map<PaymentResultResponse>(result));
        }

        if (request.Method == PaymentMethods.Visa)
        {
            var visaDto = mapper.Map<VisaPaymentModelDto>(request.Model!);
            await paymentService.ProcessVisaPaymentAsync(visaDto);
            return Ok();
        }

        return BadRequest(new { message = $"Unknown payment method: {request.Method}" });
    }
}
