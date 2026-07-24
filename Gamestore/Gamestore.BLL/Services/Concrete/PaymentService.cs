using System.Net.Http.Json;
using Gamestore.BLL.DTOs.Orders;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Gamestore.BLL.Services.Concrete;

public class PaymentService(
    IOrderService orderService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PaymentService> logger) : IPaymentService
{
    private const int MaxRetryAttempts = 3;

    public async Task<byte[]> ProcessBankPaymentAsync()
    {
        var order = await orderService.GetOpenOrderForPaymentAsync();

        try
        {
            var items = await orderService.GetOrderDetailsAsync(order.Id);
            var total = orderService.CalculateOrderTotal(items);
            var validityDays = configuration.GetValue<int?>("BankPayment:ValidityDays") ?? 3;
            var creationDate = DateTime.UtcNow;
            var validUntil = creationDate.AddDays(validityDays);
            var pdfBytes = GenerateBankInvoicePdf(
                order.CustomerId,
                order.Id,
                creationDate,
                validUntil,
                total);

            await orderService.MarkOrderAsPaidAsync(order.Id);
            return pdfBytes;
        }
        catch (Exception exception)
        {
            await TryCancelOrderAsync(order.Id, exception);
            throw;
        }
    }

    public async Task<PaymentResultDto> ProcessIBoxPaymentAsync()
    {
        var order = await orderService.GetOpenOrderForPaymentAsync();

        try
        {
            var items = await orderService.GetOrderDetailsAsync(order.Id);
            var total = orderService.CalculateOrderTotal(items);
            var requestBody = new
            {
                transactionAmount = total,
                accountNumber = order.CustomerId,
                invoiceNumber = order.Id,
            };

            var succeeded = await SendPaymentRequestWithRetryAsync(
                "/api/payments/ibox",
                requestBody);

            if (!succeeded)
            {
                throw new ValidationException("IBox payment failed after all retry attempts.");
            }

            await orderService.MarkOrderAsPaidAsync(order.Id);
            return new PaymentResultDto
            {
                UserId = order.CustomerId,
                OrderId = order.Id,
                PaymentDate = DateTime.UtcNow,
                Sum = total,
            };
        }
        catch (Exception exception)
        {
            await TryCancelOrderAsync(order.Id, exception);
            throw;
        }
    }

    public async Task ProcessVisaPaymentAsync(VisaPaymentModelDto visaModel)
    {
        var order = await orderService.GetOpenOrderForPaymentAsync();

        try
        {
            var items = await orderService.GetOrderDetailsAsync(order.Id);
            var total = orderService.CalculateOrderTotal(items);
            var requestBody = new
            {
                transactionAmount = total,
                cardHolderName = visaModel.Holder,
                cardNumber = visaModel.CardNumber,
                expirationMonth = visaModel.MonthExpire,
                expirationYear = visaModel.YearExpire,
                cvv = visaModel.Cvv2,
            };

            var succeeded = await SendPaymentRequestWithRetryAsync(
                "/api/payments/visa",
                requestBody);

            if (!succeeded)
            {
                throw new ValidationException("Visa payment failed after all retry attempts.");
            }

            await orderService.MarkOrderAsPaidAsync(order.Id);
        }
        catch (Exception exception)
        {
            await TryCancelOrderAsync(order.Id, exception);
            throw;
        }
    }

    private async Task<bool> SendPaymentRequestWithRetryAsync(string endpoint, object requestBody)
    {
        var client = httpClientFactory.CreateClient("PaymentService");

        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                logger.LogInformation(
                    "Payment attempt {Attempt}/{MaxAttempts} to {Endpoint}",
                    attempt,
                    MaxRetryAttempts,
                    endpoint);

                using var response = await client.PostAsJsonAsync(endpoint, requestBody);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                logger.LogWarning(
                    "Payment attempt {Attempt} failed with status {StatusCode}",
                    attempt,
                    response.StatusCode);
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Payment attempt {Attempt} failed", attempt);
            }
            catch (TaskCanceledException exception)
            {
                logger.LogWarning(exception, "Payment attempt {Attempt} timed out", attempt);
            }

            if (attempt < MaxRetryAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
            }
        }

        return false;
    }

    private async Task TryCancelOrderAsync(Guid orderId, Exception paymentException)
    {
        try
        {
            await orderService.MarkOrderAsCancelledAsync(orderId);
        }
        catch (Exception cancellationException)
        {
            logger.LogError(
                cancellationException,
                "Could not mark order {OrderId} as cancelled after payment failure: {PaymentError}",
                orderId,
                paymentException.Message);
        }
    }

    private static byte[] GenerateBankInvoicePdf(
        Guid customerId,
        Guid orderId,
        DateTime creationDate,
        DateTime validUntil,
        double totalSum)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(style => style.FontSize(14));
                page.Header().Text("Bank Payment Invoice")
                    .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium);
                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text($"User ID: {customerId}");
                    column.Item().Text($"Order ID: {orderId}");
                    column.Item().Text($"Creation Date: {creationDate:yyyy-MM-dd HH:mm:ss} UTC");
                    column.Item().Text($"Valid Until: {validUntil:yyyy-MM-dd HH:mm:ss} UTC");
                    column.Item().PaddingTop(20)
                        .Text($"Total Sum: ${totalSum:F2}")
                        .SemiBold().FontSize(18);
                    column.Item().PaddingTop(30)
                        .Text("Please complete the payment before the validity date.")
                        .Italic().FontSize(12).FontColor(Colors.Grey.Medium);
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated by Game Store - ");
                    text.Span($"{DateTime.UtcNow:yyyy-MM-dd}");
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }
}
