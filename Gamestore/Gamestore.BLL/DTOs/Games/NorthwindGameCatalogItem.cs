namespace Gamestore.BLL.DTOs.Games;

public sealed class NorthwindGameCatalogItem
{
    public required GameDto Game { get; init; }

    public string? SupplierName { get; init; }

    public string? CategoryName { get; init; }

    public int ViewCount { get; init; }
}
