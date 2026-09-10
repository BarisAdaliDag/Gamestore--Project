using System.Security.Cryptography;
using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;

namespace Gamestore.BLL.Services.Concrete;

public sealed class NorthwindProductService(
    INorthwindProductRepository productRepository,
    INorthwindSupplierRepository supplierRepository,
    INorthwindCategoryRepository categoryRepository) : INorthwindProductService
{
    public IReadOnlyList<GameDto> GetAll()
    {
        return [.. GetAllWithReferences().Select(item => item.Game)];
    }

    public IReadOnlyList<NorthwindGameCatalogItem> GetAllWithReferences()
    {
        var suppliers = supplierRepository
            .Query()
            .ToList()
            .Where(supplier => supplier.SupplierId.HasValue
                && !string.IsNullOrWhiteSpace(supplier.CompanyName))
            .GroupBy(supplier => supplier.SupplierId!.Value)
            .ToDictionary(group => group.Key, group => group.First().CompanyName!);

        var categories = categoryRepository
            .Query()
            .ToList()
            .Where(category => category.CategoryId.HasValue
                && !string.IsNullOrWhiteSpace(category.CategoryName))
            .GroupBy(category => category.CategoryId!.Value)
            .ToDictionary(group => group.Key, group => group.First().CategoryName!);

        return [.. productRepository
            .Query()
            .ToList()
            .Where(IsUsable)
            .Select(product => new NorthwindGameCatalogItem
            {
                Game = MapToGame(product),
                SupplierName = product.SupplierId.HasValue
                    && suppliers.TryGetValue(product.SupplierId.Value, out var supplierName)
                    ? supplierName
                    : null,
                CategoryName = product.CategoryId.HasValue
                    && categories.TryGetValue(product.CategoryId.Value, out var categoryName)
                    ? categoryName
                    : null,
            })];
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
