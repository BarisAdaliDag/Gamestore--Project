using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Helpers;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Services.Concrete;

public sealed class GameMaterializationService(
    IUnitOfWork unitOfWork,
    INorthwindProductService northwindProductService) : IGameMaterializationService
{
    private static readonly SemaphoreSlim MaterializationLock = new(1, 1);

    public async Task<Game?> GetOrCreateAsync(string gameKey)
    {
        if (!gameKey.StartsWith("mongo-", StringComparison.OrdinalIgnoreCase))
        {
            return await unitOfWork.Games.GetByKeyAsync(gameKey);
        }

        await MaterializationLock.WaitAsync();
        try
        {
            return await MaterializeMongoGameAsync(gameKey);
        }
        finally
        {
            MaterializationLock.Release();
        }
    }

    private async Task<Game?> MaterializeMongoGameAsync(string gameKey)
    {
        var existingGame = await unitOfWork.Games.GetByKeyWithDetailsAsync(gameKey);
        if (existingGame is not null)
        {
            return existingGame;
        }

        var mongoItem = await northwindProductService.GetCatalogItemByKeyAsync(gameKey);
        if (mongoItem is null)
        {
            return null;
        }

        var publisher = await GetOrCreatePublisherAsync(mongoItem.SupplierName);
        var genre = await GetOrCreateGenreAsync(mongoItem.CategoryName);
        var game = new Game
        {
            Id = mongoItem.Game.Id,
            Name = mongoItem.Game.Name,
            Key = mongoItem.Game.Key,
            Description = mongoItem.Game.Description,
            Price = mongoItem.Game.Price,
            UnitInStock = mongoItem.Game.UnitInStock,
            Discount = mongoItem.Game.Discount,
            PublisherId = publisher.Id,
        };

        await unitOfWork.Games.AddAsync(game);

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
}
