using System.Net;
using System.Text;
using Gamestore.BLL.DTOs.Orders;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.BLL.Services.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gamestore.Tests.Services;

public class PaymentServiceTests
{
    private static readonly Guid CustomerId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid OrderId = Guid.NewGuid();

    private readonly Mock<IOrderService> _mockOrderService = new();
    private readonly TestablePaymentService _service;

    public PaymentServiceTests()
    {
        _mockOrderService.Setup(service => service.GetOpenOrderForPaymentAsync())
            .ReturnsAsync(new OrderCheckoutDto { Id = OrderId, CustomerId = CustomerId });
        List<OrderGameDto> orderItems =
        [
            new() { ProductId = Guid.NewGuid(), Price = 100, Quantity = 1, Discount = 0 },
        ];
        _mockOrderService.Setup(service => service.GetOrderDetailsAsync(OrderId))
            .ReturnsAsync(orderItems);
        _mockOrderService.Setup(service => service.CalculateOrderTotal(It.IsAny<IEnumerable<OrderGameDto>>()))
            .Returns(100.0);

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BankPayment:ValidityDays"] = "3",
            })
            .Build();

        _service = new TestablePaymentService(
            _mockOrderService.Object,
            mockHttpClientFactory.Object,
            configuration,
            Mock.Of<ILogger<PaymentService>>());
    }

    [Fact]
    public async Task ProcessBankPaymentAsyncShouldReturnPdfAndMarkOrderAsPaid()
    {
        var pdf = await _service.ProcessBankPaymentAsync();

        Assert.NotEmpty(pdf);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf));
        Assert.Equal(0, _service.SendCallCount);
        _mockOrderService.Verify(service => service.MarkOrderAsPaidAsync(OrderId), Times.Once);
        _mockOrderService.Verify(
            service => service.MarkOrderAsCancelledAsync(It.IsAny<Guid>()),
            Times.Never);
        _mockOrderService.Verify(
            service => service.ResetOrderForRetryAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessIBoxPaymentAsyncShouldMarkOrderAsPaidWhenResponseIsValid()
    {
        _service.EnqueueResponse(() => JsonResponse(ValidIBoxBody()));

        var result = await _service.ProcessIBoxPaymentAsync();

        Assert.Equal(CustomerId, result.UserId);
        Assert.Equal(OrderId, result.OrderId);
        Assert.Equal(100.0, result.Sum);
        Assert.Equal(1, _service.SendCallCount);
        _mockOrderService.Verify(service => service.MarkOrderAsPaidAsync(OrderId), Times.Once);
        _mockOrderService.Verify(
            service => service.MarkOrderAsCancelledAsync(It.IsAny<Guid>()),
            Times.Never);
        _mockOrderService.Verify(
            service => service.ResetOrderForRetryAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessIBoxPaymentAsyncShouldRetryWhenResponseBodyDoesNotMatchOrder()
    {
        var wrongAccount = Guid.NewGuid();
        _service.EnqueueResponse(() => JsonResponse($@"{{""accountNumber"":""{wrongAccount}"",""invoiceNumber"":""{OrderId}"",""paymentMethod"":1,""accountId"":""{wrongAccount}"",""amount"":100.0}}"));
        _service.EnqueueResponse(() => JsonResponse(ValidIBoxBody()));

        var result = await _service.ProcessIBoxPaymentAsync();

        Assert.Equal(2, _service.SendCallCount);
        Assert.Equal(OrderId, result.OrderId);
        _mockOrderService.Verify(service => service.MarkOrderAsPaidAsync(OrderId), Times.Once);
    }

    [Fact]
    public async Task ProcessIBoxPaymentAsyncShouldRetryWhenResponseBodyIsEmpty()
    {
        _service.EnqueueResponse(() => EmptyResponse(HttpStatusCode.OK));
        _service.EnqueueResponse(() => JsonResponse(ValidIBoxBody()));

        await _service.ProcessIBoxPaymentAsync();

        Assert.Equal(2, _service.SendCallCount);
        _mockOrderService.Verify(service => service.MarkOrderAsPaidAsync(OrderId), Times.Once);
    }

    [Fact]
    public async Task ProcessIBoxPaymentAsyncShouldRetryWhenRequestFails()
    {
        _service.EnqueueResponse(() => throw new HttpRequestException("Connection refused"));
        _service.EnqueueResponse(() => EmptyResponse(HttpStatusCode.InternalServerError));
        _service.EnqueueResponse(() => JsonResponse(ValidIBoxBody()));

        await _service.ProcessIBoxPaymentAsync();

        Assert.Equal(3, _service.SendCallCount);
        _mockOrderService.Verify(service => service.MarkOrderAsPaidAsync(OrderId), Times.Once);
    }

    [Fact]
    public async Task ProcessIBoxPaymentAsyncShouldResetOrderWhenAllRetriesFail()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            _service.EnqueueResponse(() => EmptyResponse(HttpStatusCode.InternalServerError));
        }

        await Assert.ThrowsAsync<ValidationException>(_service.ProcessIBoxPaymentAsync);

        Assert.Equal(3, _service.SendCallCount);
        _mockOrderService.Verify(service => service.MarkOrderAsPaidAsync(It.IsAny<Guid>()), Times.Never);
        _mockOrderService.Verify(service => service.MarkOrderAsCancelledAsync(It.IsAny<Guid>()), Times.Never);
        _mockOrderService.Verify(service => service.ResetOrderForRetryAsync(OrderId), Times.Once);
    }

    [Fact]
    public async Task ProcessVisaPaymentAsyncShouldMarkOrderAsPaidOnSuccess()
    {
        _service.EnqueueResponse(() => EmptyResponse(HttpStatusCode.OK));

        await _service.ProcessVisaPaymentAsync(CreateVisaModel());

        Assert.Equal(1, _service.SendCallCount);
        _mockOrderService.Verify(service => service.MarkOrderAsPaidAsync(OrderId), Times.Once);
        _mockOrderService.Verify(
            service => service.MarkOrderAsCancelledAsync(It.IsAny<Guid>()),
            Times.Never);
        _mockOrderService.Verify(
            service => service.ResetOrderForRetryAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessVisaPaymentAsyncShouldNotRetryWhenProviderRejectsRequest()
    {
        _service.EnqueueResponse(() => EmptyResponse(HttpStatusCode.BadRequest));

        await Assert.ThrowsAsync<ValidationException>(() => _service.ProcessVisaPaymentAsync(CreateVisaModel()));

        Assert.Equal(1, _service.SendCallCount);
        _mockOrderService.Verify(service => service.MarkOrderAsPaidAsync(It.IsAny<Guid>()), Times.Never);
        _mockOrderService.Verify(service => service.MarkOrderAsCancelledAsync(It.IsAny<Guid>()), Times.Never);
        _mockOrderService.Verify(service => service.ResetOrderForRetryAsync(OrderId), Times.Once);
    }

    [Fact]
    public async Task ProcessVisaPaymentAsyncShouldResetOrderWhenTransientFailuresExhaustRetries()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            _service.EnqueueResponse(() => EmptyResponse(HttpStatusCode.InternalServerError));
        }

        await Assert.ThrowsAsync<ValidationException>(() => _service.ProcessVisaPaymentAsync(CreateVisaModel()));

        Assert.Equal(3, _service.SendCallCount);
        _mockOrderService.Verify(service => service.MarkOrderAsPaidAsync(It.IsAny<Guid>()), Times.Never);
        _mockOrderService.Verify(service => service.MarkOrderAsCancelledAsync(It.IsAny<Guid>()), Times.Never);
        _mockOrderService.Verify(service => service.ResetOrderForRetryAsync(OrderId), Times.Once);
    }

    private static string ValidIBoxBody()
        => $@"{{""accountNumber"":""{CustomerId}"",""invoiceNumber"":""{OrderId}"",""paymentMethod"":1,""accountId"":""{CustomerId}"",""amount"":100.0}}";

    private static VisaPaymentModelDto CreateVisaModel()
        => new()
        {
            Holder = "Test User",
            CardNumber = "4007702835532454",
            MonthExpire = 10,
            YearExpire = 2030,
            Cvv2 = 111,
        };

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage EmptyResponse(HttpStatusCode statusCode)
        => new(statusCode)
        {
            Content = new StringContent(string.Empty),
        };

    private sealed class TestablePaymentService(
        IOrderService orderService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
        : PaymentService(orderService, httpClientFactory, configuration, logger)
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses = new();

        public int SendCallCount { get; private set; }

        public void EnqueueResponse(Func<HttpResponseMessage> responseFactory)
            => _responses.Enqueue(responseFactory);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpClient client,
            string endpoint,
            object requestBody)
        {
            SendCallCount++;
            return Task.FromResult(_responses.Dequeue()());
        }
    }
}
