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
            .AsEnumerable()
            .Where(supplier => supplier.SupplierId.HasValue
                && !string.IsNullOrWhiteSpace(supplier.CompanyName))
            .GroupBy(supplier => supplier.SupplierId!.Value)
            .ToDictionary(group => group.Key, group => group.First().CompanyName!);

        var categories = categoryRepository
            .Query()
            .AsEnumerable()
            .Where(category => category.CategoryId.HasValue
                && !string.IsNullOrWhiteSpace(category.CategoryName))
            .GroupBy(category => category.CategoryId!.Value)
            .ToDictionary(group => group.Key, group => group.First().CategoryName!);

        return BuildCatalog(productRepository.Query(), suppliers, categories);
    }

    public async Task<IReadOnlyList<GameDto>> GetAllAsync()
    {
        return [.. (await GetAllWithReferencesAsync()).Select(item => item.Game)];
    }

    public async Task<IReadOnlyList<NorthwindGameCatalogItem>> GetAllWithReferencesAsync()
    {
        var suppliers = supplierRepository
            .Query()
            .AsEnumerable()
            .Where(supplier => supplier.SupplierId.HasValue
                && !string.IsNullOrWhiteSpace(supplier.CompanyName))
            .GroupBy(supplier => supplier.SupplierId!.Value)
            .ToDictionary(group => group.Key, group => group.First().CompanyName!);

        var categories = categoryRepository
            .Query()
            .AsEnumerable()
            .Where(category => category.CategoryId.HasValue
                && !string.IsNullOrWhiteSpace(category.CategoryName))
            .GroupBy(category => category.CategoryId!.Value)
            .ToDictionary(group => group.Key, group => group.First().CategoryName!);

        var products = productRepository
            .Query()
            .AsEnumerable()
            .Where(IsUsable)
            .ToList();

        var hydratedProducts = new List<NorthwindProductDocument>(products.Count);
        foreach (var product in products)
        {
            var gameKey = GetGameKey(product);
            var hydratedProduct = await productRepository.EnsureGameKeyAsync(product.Id, gameKey);
            hydratedProducts.Add(hydratedProduct ?? product);
        }

        return BuildCatalog(hydratedProducts, suppliers, categories);
    }

    public async Task<GameDto?> GetByKeyAsync(string key)
    {
        var product = productRepository
            .Query()
            .AsEnumerable()
            .FirstOrDefault(product => string.Equals(
                GetGameKey(product),
                key,
                StringComparison.OrdinalIgnoreCase));

        if (product is null || !IsUsable(product))
        {
            return null;
        }

        var gameKey = GetGameKey(product);
        product = await productRepository.EnsureGameKeyAsync(product.Id, gameKey) ?? product;

        await productRepository.IncrementViewCountAsync(product.Id);
        return MapToGame(product);
    }

    public async Task<GameDto?> GetByIdAsync(Guid id)
    {
        var product = productRepository
            .Query()
            .AsEnumerable()
            .FirstOrDefault(product => IsUsable(product)
                && CreateStableId(product.Id) == id);

        if (product is null)
        {
            return null;
        }

        var gameKey = GetGameKey(product);
        product = await productRepository.EnsureGameKeyAsync(product.Id, gameKey) ?? product;

        await productRepository.IncrementViewCountAsync(product.Id);
        return MapToGame(product);
    }

    public async Task<NorthwindGameCatalogItem?> GetCatalogItemByKeyAsync(string key)
    {
        var item = (await GetAllWithReferencesAsync())
            .FirstOrDefault(item => string.Equals(
                item.Game.Key,
                key,
                StringComparison.OrdinalIgnoreCase));

        return item;
    }

    private static IReadOnlyList<NorthwindGameCatalogItem> BuildCatalog(
        IEnumerable<NorthwindProductDocument> products,
        Dictionary<int, string> suppliers,
        Dictionary<int, string> categories)
    {
        return [.. products
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
                ViewCount = Math.Max(product.ViewCount ?? 0, 0),
            })];
    }

    private static bool IsUsable(NorthwindProductDocument product)
    {
        return product.Id != MongoDB.Bson.ObjectId.Empty
            && !string.IsNullOrWhiteSpace(product.ProductName);
    }

    private static GameDto MapToGame(NorthwindProductDocument product)
    {
        var productKey = GetGameKey(product);

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

    private static string GetGameKey(NorthwindProductDocument product)
    {
        return string.IsNullOrWhiteSpace(product.GameKey)
            ? $"mongo-{product.Id}"
            : product.GameKey;
    }
}
