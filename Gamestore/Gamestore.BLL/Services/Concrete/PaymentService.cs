using System.Net.Http.Json;
using System.Text.Json;
using Gamestore.BLL.DTOs.Orders;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Gamestore.BLL.Services.Concrete;

public class PaymentService(
    IOrderService orderService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PaymentService> logger,
    TimeProvider? timeProvider = null) : IPaymentService
{
    private const int MaxRetryAttempts = 3;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    private enum PaymentAttemptOutcome
    {
        Success,
        TransientFailure,
        PermanentFailure,
    }

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
            await TryResetOrderAsync(order.Id, exception);
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

            var succeeded = await SendPaymentRequestWithRetryAsync<IBoxTransactionResultDto>(
                "/api/payments/ibox",
                requestBody,
                result => IsValidIBoxResponse(result, order.CustomerId, order.Id, total));

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
            await TryResetOrderAsync(order.Id, exception);
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

            var succeeded = await SendPaymentRequestWithRetryAsync<object>(
                "/api/payments/visa",
                requestBody,
                static result => result is null);

            if (!succeeded)
            {
                throw new ValidationException("Visa payment failed after all retry attempts.");
            }

            await orderService.MarkOrderAsPaidAsync(order.Id);
        }
        catch (Exception exception)
        {
            await TryResetOrderAsync(order.Id, exception);
            throw;
        }
    }

    /// <summary>
    /// Sends the payment request. Extracted as a virtual method so tests can simulate
    /// microservice responses (success, rejection, timeout) without a real HTTP server.
    /// </summary>
    protected virtual Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string endpoint,
        object requestBody)
        => client.PostAsJsonAsync(endpoint, requestBody);

    private async Task<bool> SendPaymentRequestWithRetryAsync<TResponse>(
        string endpoint,
        object requestBody,
        Func<TResponse?, bool> isResponseValid)
    {
        var client = httpClientFactory.CreateClient("PaymentService");
        var attempt = 0;

        var pipelineBuilder = new ResiliencePipelineBuilder<PaymentAttemptOutcome>
        {
            TimeProvider = _timeProvider,
        };

        var retryOptions = new RetryStrategyOptions<PaymentAttemptOutcome>
        {
            MaxRetryAttempts = MaxRetryAttempts - 1,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Linear,
            ShouldHandle = new PredicateBuilder<PaymentAttemptOutcome>()
                .HandleResult(outcome => outcome == PaymentAttemptOutcome.TransientFailure),
        };

        var pipeline = pipelineBuilder.AddRetry(retryOptions).Build();

        var outcome = await pipeline.ExecuteAsync(
            async cancellationToken =>
            {
                attempt++;
                logger.LogInformation(
                    "Payment attempt {Attempt}/{MaxAttempts} to {Endpoint}",
                    attempt,
                    MaxRetryAttempts,
                    endpoint);

                try
                {
                    using var response = await SendAsync(client, endpoint, requestBody);
                    var statusCode = (int)response.StatusCode;

                    if (statusCode is >= 400 and < 500)
                    {
                        logger.LogWarning(
                            "Payment attempt {Attempt} rejected with status {StatusCode}. The request is invalid, so no retry will be attempted.",
                            attempt,
                            response.StatusCode);
                        return PaymentAttemptOutcome.PermanentFailure;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning(
                            "Payment attempt {Attempt} failed with status {StatusCode}",
                            attempt,
                            response.StatusCode);
                        return PaymentAttemptOutcome.TransientFailure;
                    }

                    var body = await ReadResponseBodyAsync<TResponse>(response);
                    if (isResponseValid(body))
                    {
                        return PaymentAttemptOutcome.Success;
                    }

                    logger.LogWarning(
                        "Payment attempt {Attempt} returned an invalid response body",
                        attempt);
                    return PaymentAttemptOutcome.TransientFailure;
                }
                catch (HttpRequestException exception)
                {
                    logger.LogWarning(exception, "Payment attempt {Attempt} failed", attempt);
                    return PaymentAttemptOutcome.TransientFailure;
                }
                catch (TaskCanceledException exception)
                {
                    logger.LogWarning(exception, "Payment attempt {Attempt} timed out", attempt);
                    return PaymentAttemptOutcome.TransientFailure;
                }
            },
            CancellationToken.None);

        return outcome == PaymentAttemptOutcome.Success;
    }

    private static async Task<TResponse?> ReadResponseBodyAsync<TResponse>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    // NFR2: a 200 status alone is not enough - the microservice response must echo
    // the same account, invoice and amount that the payment was requested with.
    private static bool IsValidIBoxResponse(
        IBoxTransactionResultDto? result,
        Guid expectedAccountNumber,
        Guid expectedInvoiceNumber,
        double expectedAmount)
        => result is not null
            && result.AccountNumber == expectedAccountNumber
            && result.InvoiceNumber == expectedInvoiceNumber
            && Math.Abs(result.Amount - expectedAmount) < 0.001;

    private async Task TryResetOrderAsync(Guid orderId, Exception paymentException)
    {
        try
        {
            await orderService.ResetOrderForRetryAsync(orderId);
        }
        catch (Exception resetException)
        {
            logger.LogError(
                resetException,
                "Could not reset order {OrderId} for retry after payment failure: {PaymentError}",
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
