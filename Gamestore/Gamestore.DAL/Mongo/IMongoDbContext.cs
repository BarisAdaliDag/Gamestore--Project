using MongoDB.Driver;

namespace Gamestore.DAL.Mongo;

public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
}
