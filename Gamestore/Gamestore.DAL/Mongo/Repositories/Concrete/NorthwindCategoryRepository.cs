using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using MongoDB.Driver;

namespace Gamestore.DAL.Mongo.Repositories.Concrete;

public sealed class NorthwindCategoryRepository(IMongoDbContext mongoDbContext)
    : INorthwindCategoryRepository
{
    public IQueryable<NorthwindCategoryDocument> Query()
    {
        return mongoDbContext.Database
            .GetCollection<NorthwindCategoryDocument>(NorthwindCollectionNames.Categories)
            .AsQueryable();
    }
}
