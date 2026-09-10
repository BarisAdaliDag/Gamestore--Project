using MongoDB.Bson.Serialization.Attributes;

namespace Gamestore.DAL.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class NorthwindOrderDocument : NorthwindDocument
{
    [BsonElement("OrderID")]
    public int? OrderId { get; set; }

    [BsonElement("CustomerID")]
    public string? CustomerId { get; set; }

    [BsonElement("OrderDate")]
    public string? OrderDate { get; set; }
}
