using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Gamestore.DAL.Mongo;

public sealed class MongoDbContext : IMongoDbContext
{
    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"];
        var databaseName = configuration["MongoDb:DatabaseName"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "MongoDb:ConnectionString configuration is required.");
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException(
                "MongoDb:DatabaseName configuration is required.");
        }

        var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
        clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

        var client = new MongoClient(clientSettings);
        Database = client.GetDatabase(databaseName);
    }

    public IMongoDatabase Database { get; }
}
