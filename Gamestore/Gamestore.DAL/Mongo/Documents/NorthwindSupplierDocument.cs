using Gamestore.DAL.Mongo.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Gamestore.DAL.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class NorthwindSupplierDocument : NorthwindDocument
{
    [BsonElement("SupplierID")]
    public int? SupplierId { get; set; }

    [BsonElement("CompanyName")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? CompanyName { get; set; }

    [BsonElement("ContactName")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? ContactName { get; set; }

    [BsonElement("ContactTitle")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? ContactTitle { get; set; }

    [BsonElement("Address")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? Address { get; set; }

    [BsonElement("City")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? City { get; set; }

    [BsonElement("Region")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? Region { get; set; }

    [BsonElement("PostalCode")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? PostalCode { get; set; }

    [BsonElement("Country")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? Country { get; set; }

    [BsonElement("Phone")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? Phone { get; set; }

    [BsonElement("Fax")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? Fax { get; set; }

    [BsonElement("HomePage")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? HomePage { get; set; }
}
