using System.Security.Cryptography;
using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;

namespace Gamestore.BLL.Services.Concrete;

public sealed class NorthwindProductService(
    INorthwindProductRepository productRepository) : INorthwindProductService
{
    public IReadOnlyList<GameDto> GetAll()
    {
        return [.. productRepository
        .Query()
        .ToList()
        .Where(IsUsable)
        .Select(MapToGame)];
    }

    private static bool IsUsable(NorthwindProductDocument product)
    {
        return product.Id != MongoDB.Bson.ObjectId.Empty
            && !string.IsNullOrWhiteSpace(product.ProductName);
    }

    private static GameDto MapToGame(NorthwindProductDocument product)
    {
        var productKey = string.IsNullOrWhiteSpace(product.GameKey)
            ? $"mongo-{product.Id}"
            : product.GameKey!;

        return new GameDto
        {
            Id = CreateStableId(product.Id),
            Name = product.ProductName!,
            Key = productKey,
            Description = product.QuantityPerUnit,
            Price = Math.Max(product.UnitPrice ?? 0, 0),
            UnitInStock = Math.Max(product.UnitsInStock ?? 0, 0),
            Discount = 0,
        };
    }

    private static Guid CreateStableId(MongoDB.Bson.ObjectId objectId)
    {
        // The Mongo ObjectId is not a Guid. A deterministic identity is needed
        // while the product is represented as a GameDto.
        var hash = SHA256.HashData(objectId.ToByteArray());
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}
