using Gamestore.DAL.Mongo.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Gamestore.DAL.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class NorthwindCategoryDocument : NorthwindDocument
{
    [BsonElement("CategoryID")]
    public int? CategoryId { get; set; }

    [BsonElement("CategoryName")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? CategoryName { get; set; }

    [BsonElement("Description")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? Description { get; set; }

    [BsonElement("Picture")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? Picture { get; set; }
}
