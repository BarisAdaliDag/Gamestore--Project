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
        var mongoItem = await northwindProductService.GetCatalogItemByKeyAsync(gameKey);
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
        }
        else
        {
            // The SQL copy is refreshed from Mongo. Mongo itself remains read-only here.
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
}
