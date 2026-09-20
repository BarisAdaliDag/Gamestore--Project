using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using MongoDB.Driver;

namespace Gamestore.DAL.Mongo.Repositories.Concrete;

public sealed class NorthwindSupplierRepository(IMongoDbContext mongoDbContext)
    : INorthwindSupplierRepository
{
    public IQueryable<NorthwindSupplierDocument> Query()
    {
        return mongoDbContext.Database
            .GetCollection<NorthwindSupplierDocument>(NorthwindCollectionNames.Suppliers)
            .AsQueryable();
    }
}
