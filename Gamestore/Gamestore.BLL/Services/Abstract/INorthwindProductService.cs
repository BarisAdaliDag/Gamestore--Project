using Gamestore.BLL.DTOs.Games;

namespace Gamestore.BLL.Services.Abstract;

public interface INorthwindProductService
{
    IReadOnlyList<GameDto> GetAll();

    IReadOnlyList<NorthwindGameCatalogItem> GetAllWithReferences();

    Task<IReadOnlyList<GameDto>> GetAllAsync();

    Task<IReadOnlyList<NorthwindGameCatalogItem>> GetAllWithReferencesAsync();

    Task<GameDto?> GetByKeyAsync(string key);

    Task<GameDto?> GetByIdAsync(Guid id);

    Task<NorthwindGameCatalogItem?> GetCatalogItemByKeyAsync(string key);
}
