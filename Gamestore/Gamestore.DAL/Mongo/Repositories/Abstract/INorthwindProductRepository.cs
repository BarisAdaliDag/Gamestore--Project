using Gamestore.DAL.Mongo.Documents;
using MongoDB.Bson;

namespace Gamestore.DAL.Mongo.Repositories.Abstract;

public interface INorthwindProductRepository
{
    IQueryable<NorthwindProductDocument> Query();

    Task IncrementViewCountAsync(ObjectId productId);
}
