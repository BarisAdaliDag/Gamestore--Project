using MongoDB.Bson.Serialization.Attributes;

namespace Gamestore.DAL.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class NorthwindOrderDetailDocument : NorthwindDocument
{
    [BsonElement("OrderID")]
    public int? OrderId { get; set; }

    [BsonElement("ProductID")]
    public int? ProductId { get; set; }

    [BsonElement("UnitPrice")]
    public double? UnitPrice { get; set; }

    [BsonElement("Quantity")]
    public int? Quantity { get; set; }

    [BsonElement("Discount")]
    public double? Discount { get; set; }
}
