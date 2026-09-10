using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Gamestore.DAL.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class EntityChangeLogDocument
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    [BsonElement("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [BsonElement("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [BsonElement("oldVersion")]
    public BsonDocument? OldVersion { get; set; }

    [BsonElement("newVersion")]
    public BsonDocument? NewVersion { get; set; }
}
