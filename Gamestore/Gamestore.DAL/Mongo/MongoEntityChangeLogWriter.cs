using MongoDB.Bson;

namespace Gamestore.DAL.Mongo;

/// <summary>
/// Inserts audit records as raw BSON documents, as required by Epic 8 NFR3.
/// </summary>
public sealed class MongoEntityChangeLogWriter(IMongoDbContext mongoDbContext)
    : IEntityChangeLogWriter
{
    private const string CollectionName = "entity-change-logs";

    public Task WriteAsync(
        string action,
        string entityType,
        string entityId,
        BsonDocument? oldVersion,
        BsonDocument? newVersion)
    {
        var rawDocument = new BsonDocument
        {
            ["_id"] = MongoDB.Bson.ObjectId.GenerateNewId(),
            ["date"] = DateTime.UtcNow,
            ["action"] = action,
            ["entityType"] = entityType,
            ["entityId"] = entityId,
            ["oldVersion"] = oldVersion is null ? BsonNull.Value : oldVersion,
            ["newVersion"] = newVersion is null ? BsonNull.Value : newVersion,
        };

        return mongoDbContext.Database
            .GetCollection<BsonDocument>(CollectionName)
            .InsertOneAsync(rawDocument);
    }
}
