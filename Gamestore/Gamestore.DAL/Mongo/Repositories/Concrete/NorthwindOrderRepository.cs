using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using MongoDB.Driver;

namespace Gamestore.DAL.Mongo.Repositories.Concrete;

public sealed class NorthwindOrderRepository(IMongoDbContext mongoDbContext)
    : INorthwindOrderRepository
{
    public IQueryable<NorthwindOrderDocument> Query()
    {
        return mongoDbContext.Database
            .GetCollection<NorthwindOrderDocument>(NorthwindCollectionNames.Orders)
            .AsQueryable();
    }
}
