using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Gamestore.DAL.Mongo.Repositories.Concrete;

public sealed class NorthwindProductRepository(IMongoDbContext mongoDbContext)
    : INorthwindProductRepository
{
    private readonly MongoEntityChangeLogWriter _changeLogWriter = new(mongoDbContext);

    public IQueryable<NorthwindProductDocument> Query()
    {
        return mongoDbContext.Database
            .GetCollection<NorthwindProductDocument>(NorthwindCollectionNames.Products)
            .AsQueryable();
    }

    public async Task<NorthwindProductDocument?> EnsureGameKeyAsync(
        ObjectId productId,
        string gameKey)
    {
        var collection = mongoDbContext.Database
            .GetCollection<NorthwindProductDocument>(NorthwindCollectionNames.Products);
        var filterBuilder = Builders<NorthwindProductDocument>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(product => product.Id, productId),
            filterBuilder.Or(
                filterBuilder.Exists(nameof(NorthwindProductDocument.GameKey), false),
                filterBuilder.Eq(product => product.GameKey, null),
                filterBuilder.Eq(product => product.GameKey, string.Empty)));
        var update = Builders<NorthwindProductDocument>.Update.Set(
            product => product.GameKey,
            gameKey);

        var oldProduct = await collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<NorthwindProductDocument>
            {
                ReturnDocument = ReturnDocument.Before,
            });

        if (oldProduct is not null)
        {
            var oldVersion = oldProduct.ToBsonDocument();
            oldProduct.GameKey = gameKey;

            await _changeLogWriter.WriteAsync(
                "Update",
                nameof(NorthwindProductDocument),
                productId.ToString(),
                oldVersion,
                oldProduct.ToBsonDocument());

            return oldProduct;
        }

        return await collection
            .Find(filterBuilder.Eq(product => product.Id, productId))
            .FirstOrDefaultAsync();
    }

    public async Task IncrementViewCountAsync(ObjectId productId)
    {
        var collection = mongoDbContext.Database
            .GetCollection<NorthwindProductDocument>(NorthwindCollectionNames.Products);
        var filter = Builders<NorthwindProductDocument>.Filter.Eq(product => product.Id, productId);
        var update = Builders<NorthwindProductDocument>.Update.Inc(product => product.ViewCount, 1);

        var oldProduct = await collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<NorthwindProductDocument>
            {
                ReturnDocument = ReturnDocument.Before,
            });

        if (oldProduct == null)
        {
            return;
        }

        var oldVersion = oldProduct.ToBsonDocument();
        oldProduct.ViewCount = (oldProduct.ViewCount ?? 0) + 1;

        await _changeLogWriter.WriteAsync(
            "Update",
            nameof(NorthwindProductDocument),
            productId.ToString(),
            oldVersion,
            oldProduct.ToBsonDocument());
    }

    public async Task<bool> DecrementStockAsync(string gameKey, int quantity)
    {
        if (quantity <= 0 || string.IsNullOrWhiteSpace(gameKey))
        {
            return false;
        }

        var collection = mongoDbContext.Database
            .GetCollection<NorthwindProductDocument>(NorthwindCollectionNames.Products);
        var productFilter = Builders<NorthwindProductDocument>.Filter.Eq(
            product => product.GameKey,
            gameKey);

        if (gameKey.StartsWith("mongo-", StringComparison.OrdinalIgnoreCase)
            && ObjectId.TryParse(gameKey["mongo-".Length..], out var productId))
        {
            productFilter = Builders<NorthwindProductDocument>.Filter.Or(
                productFilter,
                Builders<NorthwindProductDocument>.Filter.Eq(product => product.Id, productId));
        }

        var filter = Builders<NorthwindProductDocument>.Filter.And(
            productFilter,
            Builders<NorthwindProductDocument>.Filter.Gte(
                product => product.UnitsInStock,
                quantity));
        var update = Builders<NorthwindProductDocument>.Update.Inc(
            product => product.UnitsInStock,
            -quantity);

        var oldProduct = await collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<NorthwindProductDocument>
            {
                ReturnDocument = ReturnDocument.Before,
            });

        if (oldProduct == null)
        {
            return false;
        }

        var oldVersion = oldProduct.ToBsonDocument();
        oldProduct.UnitsInStock = (oldProduct.UnitsInStock ?? 0) - quantity;

        await _changeLogWriter.WriteAsync(
            "Update",
            nameof(NorthwindProductDocument),
            gameKey,
            oldVersion,
            oldProduct.ToBsonDocument());

        return true;
    }
}
