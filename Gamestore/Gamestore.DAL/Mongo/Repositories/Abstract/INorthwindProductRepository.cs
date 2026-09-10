using Gamestore.DAL.Mongo.Documents;
using MongoDB.Bson;

namespace Gamestore.DAL.Mongo.Repositories.Abstract;

public interface INorthwindProductRepository
{
    IQueryable<NorthwindProductDocument> Query();

    Task<NorthwindProductDocument?> EnsureGameKeyAsync(
        ObjectId productId,
        string gameKey);

    Task IncrementViewCountAsync(ObjectId productId);

    Task<bool> DecrementStockAsync(string gameKey, int quantity);
}
