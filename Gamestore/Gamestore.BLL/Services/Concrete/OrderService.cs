using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Gamestore.BLL.DTOs.Orders;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Helpers;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Constants;
using Gamestore.Domain.Entities;
using Gamestore.Domain.Enums;
using MapsterMapper;
using Microsoft.Extensions.Configuration;

namespace Gamestore.BLL.Services.Concrete;

public class OrderService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IConfiguration configuration,
    INorthwindOrderRepository? northwindOrderRepository = null,
    INorthwindProductService? northwindProductService = null) : IOrderService
{
    // The payment microservice validates the IBox account number against its
    // seeded account.json. Keep the development customer aligned with that
    // account until a real customer/account integration is introduced.
    private readonly Guid _stubCustomerId =
        configuration.GetValue<Guid?>("Customer:StubId")
        ?? Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    private readonly INorthwindOrderRepository? _northwindOrderRepository = northwindOrderRepository;

    private readonly INorthwindProductService? _northwindProductService = northwindProductService;

    public async Task AddToCartAsync(string gameKey)
    {
        var game = await GetGameForCartAsync(gameKey)
            ?? throw new NotFoundException(nameof(Game), gameKey);

        if (game.UnitInStock < 1)
        {
            throw new OutOfStockException($"Game '{game.Name}' is out of stock.");
        }

        var order = await GetOrCreateOpenOrderAsync();
        var existingItem = order.OrderGames.FirstOrDefault(og => og.ProductId == game.Id);

        if (existingItem != null)
        {
            if (existingItem.Quantity >= game.UnitInStock)
            {
                throw new OutOfStockException(
                    $"Cannot add more. Only {game.UnitInStock} units available in stock.");
            }

            existingItem.Quantity++;
            unitOfWork.OrderGames.Update(existingItem);
        }
        else
        {
            await unitOfWork.OrderGames.AddAsync(new OrderGame
            {
                OrderId = order.Id,
                ProductId = game.Id,
                Price = game.Price,
                Quantity = 1,
                Discount = game.Discount,
            });
        }

        await unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveFromCartAsync(string gameKey)
    {
        var game = await unitOfWork.Games.GetByKeyAsync(gameKey)
            ?? throw new NotFoundException(nameof(Game), gameKey);

        var order = await unitOfWork.Orders.GetOpenOrderByCustomerIdAsync(_stubCustomerId)
            ?? throw new NotFoundException("Cart is empty. No open order found.");

        var item = order.OrderGames.FirstOrDefault(og => og.ProductId == game.Id)
            ?? throw new NotFoundException($"Game '{gameKey}' is not in the cart.");

        unitOfWork.OrderGames.Delete(item);

        if (order.OrderGames.Count == 1)
        {
            unitOfWork.Orders.Delete(order);
        }

        await unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<OrderGameDto>> GetCartAsync()
    {
        var order = await unitOfWork.Orders.GetOpenOrderByCustomerIdAsync(_stubCustomerId);
        return order == null
            ? []
            : mapper.Map<IEnumerable<OrderGameDto>>(order.OrderGames);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync()
    {
        var orders = await unitOfWork.Orders
            .GetByStatusesAsync(OrderStatus.Paid, OrderStatus.Cancelled);

        return mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task<IEnumerable<OrderDto>> GetOrderHistoryAsync(DateTime? start, DateTime? end)
    {
        var sqlOrders = await unitOfWork.Orders
            .GetByStatusesAsync(OrderStatus.Paid, OrderStatus.Cancelled);

        var history = sqlOrders
            .Select(mapper.Map<OrderDto>)
            .Where(order => IsWithinDateRange(order.Date, start, end))
            .ToList();

        if (_northwindOrderRepository != null)
        {
            // Read operations use the Mongo IQueryable repository. Invalid or incomplete
            // Northwind records are ignored so one damaged document does not break history.
            var northwindOrders = _northwindOrderRepository.Query().ToList();

            history.AddRange(northwindOrders
                .Select(MapNorthwindOrder)
                .Where(order => order != null)
                .Select(order => order!)
                .Where(order => IsWithinDateRange(order.Date, start, end)));
        }

        return [.. history
            .OrderByDescending(order => order.Date ?? DateTime.MinValue)];
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid id)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Order), id);

        return mapper.Map<OrderDto>(order);
    }

    public async Task<IEnumerable<OrderGameDto>> GetOrderDetailsAsync(Guid orderId)
    {
        var order = await unitOfWork.Orders.GetByIdWithDetailsAsync(orderId)
            ?? throw new NotFoundException(nameof(Order), orderId);

        return mapper.Map<IEnumerable<OrderGameDto>>(order.OrderGames);
    }

    public Task<IEnumerable<PaymentMethodDto>> GetPaymentMethodsAsync()
    {
        IEnumerable<PaymentMethodDto> methods =
        [
            new()
            {
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/5e/Visa_Inc._logo.svg/200px-Visa_Inc._logo.svg.png",
                Title = PaymentMethods.Visa,
                Description = "Pay securely with your Visa credit or debit card.",
            },
            new()
            {
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/54/Payment_terminal_2.jpg/200px-Payment_terminal_2.jpg",
                Title = PaymentMethods.IBoxTerminal,
                Description = "Pay at any IBox terminal using your invoice number.",
            },
            new()
            {
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/aa/Papierki.jpg/200px-Papierki.jpg",
                Title = PaymentMethods.Bank,
                Description = "Download a bank invoice and pay via bank transfer.",
            },
        ];

        return Task.FromResult(methods);
    }

    public async Task<OrderCheckoutDto> GetOpenOrderForPaymentAsync()
    {
        var order = await unitOfWork.Orders.GetOpenOrderByCustomerIdAsync(_stubCustomerId)
            ?? throw new ValidationException("Cart is empty. Nothing to pay for.");

        order.Status = OrderStatus.Checkout;
        unitOfWork.Orders.Update(order);
        await unitOfWork.SaveChangesAsync();

        return new OrderCheckoutDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
        };
    }

    public async Task MarkOrderAsPaidAsync(Guid orderId)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(orderId)
            ?? throw new NotFoundException(nameof(Order), orderId);

        order.Status = OrderStatus.Paid;
        order.Date = DateTime.UtcNow;
        unitOfWork.Orders.Update(order);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task MarkOrderAsCancelledAsync(Guid orderId)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(orderId)
            ?? throw new NotFoundException(nameof(Order), orderId);

        order.Status = OrderStatus.Cancelled;
        order.Date = DateTime.UtcNow;
        unitOfWork.Orders.Update(order);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task ResetOrderForRetryAsync(Guid orderId)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(orderId)
            ?? throw new NotFoundException(nameof(Order), orderId);

        order.Status = OrderStatus.Open;
        unitOfWork.Orders.Update(order);
        await unitOfWork.SaveChangesAsync();
    }

    public double CalculateOrderTotal(IEnumerable<OrderGameDto> items)
    {
        return items.Sum(item =>
            item.Price * item.Quantity * (1 - ((item.Discount ?? 0) / 100.0)));
    }

    private async Task<Order> GetOrCreateOpenOrderAsync()
    {
        var order = await unitOfWork.Orders.GetOpenOrderByCustomerIdAsync(_stubCustomerId);
        if (order != null)
        {
            return order;
        }

        order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = _stubCustomerId,
            Status = OrderStatus.Open,
        };

        await unitOfWork.Orders.AddAsync(order);
        await unitOfWork.SaveChangesAsync();
        return order;
    }

    private async Task<Game?> GetGameForCartAsync(string gameKey)
    {
        if (_northwindProductService is null
            || !gameKey.StartsWith("mongo-", StringComparison.OrdinalIgnoreCase))
        {
            return await unitOfWork.Games.GetByKeyAsync(gameKey);
        }

        var mongoItem = await _northwindProductService.GetCatalogItemByKeyAsync(gameKey);
        if (mongoItem is null)
        {
            return await unitOfWork.Games.GetByKeyAsync(gameKey);
        }

        var game = await unitOfWork.Games.GetByKeyWithDetailsAsync(gameKey);
        var publisher = await GetOrCreatePublisherAsync(mongoItem.SupplierName);
        var genre = await GetOrCreateGenreAsync(mongoItem.CategoryName);

        if (game is null)
        {
            game = new Game
            {
                Id = Guid.NewGuid(),
                Name = mongoItem.Game.Name,
                Key = mongoItem.Game.Key,
                Description = mongoItem.Game.Description,
                Price = mongoItem.Game.Price,
                UnitInStock = mongoItem.Game.UnitInStock,
                Discount = mongoItem.Game.Discount,
                PublisherId = publisher.Id,
            };

            await unitOfWork.Games.AddAsync(game);
        }
        else
        {
            // Refresh the SQL copy from Mongo before adding it to the cart.
            // Mongo itself is never written by this flow.
            game.Name = mongoItem.Game.Name;
            game.Description = mongoItem.Game.Description;
            game.Price = mongoItem.Game.Price;
            game.UnitInStock = mongoItem.Game.UnitInStock;
            game.Discount = mongoItem.Game.Discount;
            game.PublisherId = publisher.Id;
            game.GameGenres.Clear();
        }

        if (genre is not null)
        {
            game.GameGenres.Add(new GameGenre
            {
                GameId = game.Id,
                GenreId = genre.Id,
            });
        }

        await unitOfWork.SaveChangesAsync();
        return game;
    }

    private async Task<Publisher> GetOrCreatePublisherAsync(string? supplierName)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            throw new ValidationException("Northwind product has no supplier.");
        }

        var publisher = await unitOfWork.Publishers.GetByCompanyNameAsync(supplierName);
        if (publisher is not null)
        {
            return publisher;
        }

        publisher = new Publisher
        {
            Id = NorthwindReferenceIdentity.CreateSupplierId(supplierName),
            CompanyName = supplierName,
            Description = "Imported from Northwind supplier.",
        };

        await unitOfWork.Publishers.AddAsync(publisher);
        return publisher;
    }

    private async Task<Genre?> GetOrCreateGenreAsync(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return null;
        }

        var genre = await unitOfWork.Genres.GetSingleAsync(g => g.Name == categoryName);
        if (genre is not null)
        {
            return genre;
        }

        genre = new Genre
        {
            Id = NorthwindReferenceIdentity.CreateCategoryId(categoryName),
            Name = categoryName,
        };

        await unitOfWork.Genres.AddAsync(genre);
        return genre;
    }

    private static OrderDto? MapNorthwindOrder(
        Gamestore.DAL.Mongo.Documents.NorthwindOrderDocument order)
    {
        var orderKey = order.OrderId.HasValue
            ? order.OrderId.Value.ToString(CultureInfo.InvariantCulture)
            : order.Id.ToString();

        DateTime? date = null;
        if (!string.IsNullOrWhiteSpace(order.OrderDate)
            && DateTime.TryParse(
                order.OrderDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsedDate))
        {
            date = parsedDate;
        }

        return new OrderDto
        {
            Id = CreateStableGuid($"northwind-order:{orderKey}"),
            CustomerId = string.IsNullOrWhiteSpace(order.CustomerId)
                ? Guid.Empty
                : CreateStableGuid($"northwind-customer:{order.CustomerId.Trim()}"),
            Date = date,
        };
    }

    private static bool IsWithinDateRange(DateTime? date, DateTime? start, DateTime? end)
    {
        return date.HasValue
            ? (!start.HasValue || date.Value >= start.Value)
                && (!end.HasValue || date.Value <= end.Value)
            : !start.HasValue && !end.HasValue;
    }

    private static Guid CreateStableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash[..16]);
    }
}
