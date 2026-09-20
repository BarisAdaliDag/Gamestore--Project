using MongoDB.Bson.Serialization.Attributes;

namespace Gamestore.DAL.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class NorthwindShipperDocument : NorthwindDocument
{
    [BsonElement("ShipperID")]
    public int? ShipperId { get; set; }

    [BsonElement("CompanyName")]
    public string? CompanyName { get; set; }

    [BsonElement("Phone")]
    public string? Phone { get; set; }
}
