using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Gamestore.DAL.Mongo.Documents;

public abstract class NorthwindDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
}
