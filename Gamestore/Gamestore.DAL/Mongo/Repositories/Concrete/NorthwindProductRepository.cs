using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;
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
}
