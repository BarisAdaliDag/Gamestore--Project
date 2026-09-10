using MongoDB.Bson.Serialization.Attributes;

namespace Gamestore.DAL.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class NorthwindCategoryDocument : NorthwindDocument
{
    [BsonElement("CategoryID")]
    public int? CategoryId { get; set; }

    [BsonElement("CategoryName")]
    public string? CategoryName { get; set; }

    [BsonElement("Description")]
    public string? Description { get; set; }

    [BsonElement("Picture")]
    public string? Picture { get; set; }
}
