using Gamestore.BLL.DTOs.Games;

namespace Gamestore.BLL.Services.Abstract;

public interface IUnifiedGameCatalogService
{
    Task<PagedGamesDto> GetGamesAsync(GameQueryParams parameters);
}
