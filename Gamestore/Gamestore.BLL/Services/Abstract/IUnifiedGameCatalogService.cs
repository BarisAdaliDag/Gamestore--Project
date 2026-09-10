using Gamestore.BLL.DTOs.Games;

namespace Gamestore.BLL.Services.Abstract;

public interface IUnifiedGameCatalogService
{
    Task<PagedGamesDto> GetGamesAsync(GameQueryParams parameters);

    Task<GameDto> GetByKeyAsync(string key);

    Task<GameDto> GetByIdAsync(Guid id);
}
