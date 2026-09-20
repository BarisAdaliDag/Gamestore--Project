using MongoDB.Bson;

namespace Gamestore.DAL.UnitOfWork;

internal sealed record EntityChange(
    string Action,
    string EntityType,
    string EntityId,
    BsonDocument? OldVersion,
    BsonDocument? NewVersion);
