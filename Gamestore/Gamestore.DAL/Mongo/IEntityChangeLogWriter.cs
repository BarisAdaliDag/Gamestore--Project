using MongoDB.Bson;

namespace Gamestore.DAL.Mongo;

public interface IEntityChangeLogWriter
{
    Task WriteAsync(
        string action,
        string entityType,
        string entityId,
        BsonDocument? oldVersion,
        BsonDocument? newVersion);
}
