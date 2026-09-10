using System.Text.Json;
using Gamestore.DAL.Mongo;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class ShippersController(IMongoDbContext mongoDbContext) : ControllerBase
{
    private static readonly JsonWriterSettings StrictJsonSettings = new()
    {
        OutputMode = JsonOutputMode.CanonicalExtendedJson,
    };

    [HttpGet]
    public ActionResult<IEnumerable<JsonElement>> GetAll()
    {
        var collection = mongoDbContext.Database.GetCollection<BsonDocument>(
            NorthwindCollectionNames.Shippers);

        var documents = collection
            .AsQueryable()
            .ToList();

        var response = documents
            .Select(ToJsonElement)
            .ToList();

        return Ok(response);
    }

    private static JsonElement ToJsonElement(BsonDocument document)
    {
        using var jsonDocument = JsonDocument.Parse(document.ToJson(StrictJsonSettings));
        return jsonDocument.RootElement.Clone();
    }
}
