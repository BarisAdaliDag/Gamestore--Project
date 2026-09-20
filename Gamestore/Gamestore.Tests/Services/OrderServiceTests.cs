using Gamestore.BLL.DTOs.Orders;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using Gamestore.Domain.Enums;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Gamestore.Tests.Services;

public class OrderServiceTests
{
    private const string StubCustomerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IGameRepository> _mockGameRepository = new();
    private readonly Mock<IOrderRepository> _mockOrderRepository = new();
    private readonly Mock<IRepository<OrderGame>> _mockOrderGameRepository = new();
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(_mockGameRepository.Object);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Orders)
            .Returns(_mockOrderRepository.Object);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.OrderGames)
            .Returns(_mockOrderGameRepository.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Customer:StubId"] = StubCustomerId,
            })
            .Build();

        _service = new OrderService(_mockUnitOfWork.Object, _mockMapper.Object, configuration);
    }

    [Fact]
    public async Task AddToCartAsyncShouldThrowOutOfStockExceptionWhenGameIsOutOfStock()
    {
        var game = CreateGame(stock: 0);
        _mockGameRepository.Setup(repository => repository.GetByKeyAsync(game.Key))
            .ReturnsAsync(game);

        await Assert.ThrowsAsync<OutOfStockException>(() => _service.AddToCartAsync(game.Key));
        _mockOrderGameRepository.Verify(
            repository => repository.AddAsync(It.IsAny<OrderGame>()),
            Times.Never);
    }

    [Fact]
    public async Task AddToCartAsyncShouldThrowOutOfStockExceptionWhenQuantityReachesStock()
    {
        var game = CreateGame(stock: 1);
        var order = CreateOpenOrder();
        order.OrderGames.Add(new OrderGame
        {
            OrderId = order.Id,
            ProductId = game.Id,
            Price = game.Price,
            Quantity = 1,
        });

        _mockGameRepository.Setup(repository => repository.GetByKeyAsync(game.Key))
            .ReturnsAsync(game);
        _mockOrderRepository.Setup(repository => repository.GetOpenOrderByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<OutOfStockException>(() => _service.AddToCartAsync(game.Key));
        Assert.Equal(1, order.OrderGames.Single().Quantity);
    }

    [Fact]
    public async Task AddToCartAsyncShouldIncrementQuantityWhenGameIsAlreadyInCart()
    {
        var game = CreateGame(stock: 5);
        var order = CreateOpenOrder();
        var existingItem = new OrderGame
        {
            OrderId = order.Id,
            ProductId = game.Id,
            Price = game.Price,
            Quantity = 1,
        };
        order.OrderGames.Add(existingItem);

        _mockGameRepository.Setup(repository => repository.GetByKeyAsync(game.Key))
            .ReturnsAsync(game);
        _mockOrderRepository.Setup(repository => repository.GetOpenOrderByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(order);

        await _service.AddToCartAsync(game.Key);

        Assert.Equal(2, existingItem.Quantity);
        _mockOrderGameRepository.Verify(repository => repository.Update(existingItem), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddToCartAsyncShouldCreateOpenOrderWhenCartDoesNotExist()
    {
        var game = CreateGame(stock: 5);
        Order? createdOrder = null;
        OrderGame? addedItem = null;

        _mockGameRepository.Setup(repository => repository.GetByKeyAsync(game.Key))
            .ReturnsAsync(game);
        _mockOrderRepository.Setup(repository => repository.GetOpenOrderByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Order?)null);
        _mockOrderRepository.Setup(repository => repository.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(order => createdOrder = order)
            .Returns(Task.CompletedTask);
        _mockOrderGameRepository.Setup(repository => repository.AddAsync(It.IsAny<OrderGame>()))
            .Callback<OrderGame>(item => addedItem = item)
            .Returns(Task.CompletedTask);

        await _service.AddToCartAsync(game.Key);

        Assert.NotNull(createdOrder);
        Assert.Equal(Guid.Parse(StubCustomerId), createdOrder.CustomerId);
        Assert.Equal(OrderStatus.Open, createdOrder.Status);
        Assert.NotNull(addedItem);
        Assert.Equal(createdOrder.Id, addedItem.OrderId);
        Assert.Equal(game.Id, addedItem.ProductId);
        Assert.Equal(1, addedItem.Quantity);
    }

    [Fact]
    public async Task RemoveFromCartAsyncShouldDeleteOpenOrderWhenLastGameIsRemoved()
    {
        var game = CreateGame(stock: 5);
        var order = CreateOpenOrder();
        var item = new OrderGame
        {
            OrderId = order.Id,
            ProductId = game.Id,
            Price = game.Price,
            Quantity = 1,
        };
        order.OrderGames.Add(item);

        _mockGameRepository.Setup(repository => repository.GetByKeyAsync(game.Key))
            .ReturnsAsync(game);
        _mockOrderRepository.Setup(repository => repository.GetOpenOrderByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(order);

        await _service.RemoveFromCartAsync(game.Key);

        _mockOrderGameRepository.Verify(repository => repository.Delete(item), Times.Once);
        _mockOrderRepository.Verify(repository => repository.Delete(order), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveFromCartAsyncShouldKeepOpenOrderWhenOtherGamesRemain()
    {
        var game = CreateGame(stock: 5);
        var order = CreateOpenOrder();
        var item = new OrderGame
        {
            OrderId = order.Id,
            ProductId = game.Id,
            Price = game.Price,
            Quantity = 1,
        };
        order.OrderGames.Add(item);
        order.OrderGames.Add(new OrderGame
        {
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Price = 10,
            Quantity = 2,
        });

        _mockGameRepository.Setup(repository => repository.GetByKeyAsync(game.Key))
            .ReturnsAsync(game);
        _mockOrderRepository.Setup(repository => repository.GetOpenOrderByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(order);

        await _service.RemoveFromCartAsync(game.Key);

        _mockOrderGameRepository.Verify(repository => repository.Delete(item), Times.Once);
        _mockOrderRepository.Verify(repository => repository.Delete(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task GetCartAsyncShouldReturnEmptyWhenNoOpenOrderExists()
    {
        _mockOrderRepository.Setup(repository => repository.GetOpenOrderByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Order?)null);

        var result = await _service.GetCartAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOrderHistoryAsyncShouldIncludeNorthwindOrdersWithoutDateLimits()
    {
        _mockOrderRepository
            .Setup(repository => repository.GetByStatusesAsync(It.IsAny<OrderStatus[]>()))
            .ReturnsAsync([]);
        var northwindOrders = new Mock<INorthwindOrderRepository>();
        northwindOrders.Setup(repository => repository.Query())
            .Returns(new[]
            {
                new NorthwindOrderDocument
                {
                    OrderId = 1,
                    CustomerId = "ALFKI",
                    OrderDate = "1997-01-01",
                },
                new NorthwindOrderDocument
                {
                    OrderId = 2,
                    CustomerId = "BONAP",
                    OrderDate = "1998-01-01",
                },
            }.AsQueryable());
        var service = new OrderService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            new ConfigurationBuilder().Build(),
            northwindOrders.Object);

        var result = await service.GetOrderHistoryAsync(null, null);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task MarkOrderAsPaidAsyncShouldSetPaidStatusAndDate()
    {
        var order = CreateOpenOrder();
        order.Status = OrderStatus.Checkout;

        _mockOrderRepository.Setup(repository => repository.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        await _service.MarkOrderAsPaidAsync(order.Id);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.Date);
        _mockOrderRepository.Verify(repository => repository.Update(order), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkOrderAsPaidAsyncShouldSynchronizeMongoProductStock()
    {
        var game = CreateGame(stock: 5);
        game.Key = "mongo-507f1f77bcf86cd799439011";

        var order = CreateOpenOrder();
        order.Status = OrderStatus.Checkout;
        order.OrderGames.Add(new OrderGame
        {
            OrderId = order.Id,
            ProductId = game.Id,
            Quantity = 2,
            Game = game,
        });

        var mongoProductRepository = new Mock<INorthwindProductRepository>();
        mongoProductRepository
            .Setup(repository => repository.DecrementStockAsync(game.Key, 2))
            .ReturnsAsync(true);
        _mockOrderRepository.Setup(repository => repository.GetByIdAsync(order.Id))
            .ReturnsAsync(order);
        _mockOrderRepository.Setup(repository => repository.GetByIdWithDetailsAsync(order.Id))
            .ReturnsAsync(order);

        var service = new OrderService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            new ConfigurationBuilder().Build(),
            northwindProductRepository: mongoProductRepository.Object);

        await service.MarkOrderAsPaidAsync(order.Id);

        Assert.Equal(3, game.UnitInStock);
        mongoProductRepository.Verify(
            repository => repository.DecrementStockAsync(game.Key, 2),
            Times.Once);
        _mockGameRepository.Verify(repository => repository.Update(game), Times.Once);
    }

    [Fact]
    public async Task MarkOrderAsCancelledAsyncShouldSetCancelledStatusAndDate()
    {
        var order = CreateOpenOrder();
        order.Status = OrderStatus.Checkout;

        _mockOrderRepository.Setup(repository => repository.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        await _service.MarkOrderAsCancelledAsync(order.Id);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.Date);
        _mockOrderRepository.Verify(repository => repository.Update(order), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ResetOrderForRetryAsyncShouldSetOpenStatusSoPaymentCanBeRetried()
    {
        var order = CreateOpenOrder();
        order.Status = OrderStatus.Checkout;

        _mockOrderRepository.Setup(repository => repository.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        await _service.ResetOrderForRetryAsync(order.Id);

        Assert.Equal(OrderStatus.Open, order.Status);
        _mockOrderRepository.Verify(repository => repository.Update(order), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public void CalculateOrderTotalShouldApplyDiscounts()
    {
        var items = new List<OrderGameDto>
        {
            new() { ProductId = Guid.NewGuid(), Price = 100, Quantity = 2, Discount = 10 },
            new() { ProductId = Guid.NewGuid(), Price = 50, Quantity = 1, Discount = null },
        };

        var total = _service.CalculateOrderTotal(items);

        Assert.Equal(230, total, precision: 3);
    }

    private static Game CreateGame(int stock)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            Key = "test-game",
            Price = 59.99,
            UnitInStock = stock,
        };

    private static Order CreateOpenOrder()
        => new()
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.Parse(StubCustomerId),
            Status = OrderStatus.Open,
        };
}
