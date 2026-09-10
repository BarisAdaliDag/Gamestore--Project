using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Gamestore.DAL.Mongo.Repositories.Concrete;

public sealed class NorthwindProductRepository(IMongoDbContext mongoDbContext)
    : INorthwindProductRepository
{
    public IQueryable<NorthwindProductDocument> Query()
    {
        return mongoDbContext.Database
            .GetCollection<NorthwindProductDocument>(NorthwindCollectionNames.Products)
            .AsQueryable();
    }

    public Task IncrementViewCountAsync(ObjectId productId)
    {
        var collection = mongoDbContext.Database
            .GetCollection<NorthwindProductDocument>(NorthwindCollectionNames.Products);
        var filter = Builders<NorthwindProductDocument>.Filter.Eq(product => product.Id, productId);
        var update = Builders<NorthwindProductDocument>.Update.Inc(product => product.ViewCount, 1);

        return collection.UpdateOneAsync(filter, update);
    }
}
