using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.DTOs.Platforms;
using Gamestore.BLL.DTOs.Publishers;
using Gamestore.BLL.Services.Abstract;

namespace Gamestore.BLL.Services.Concrete;

public sealed class GameLookupService(
    IUnifiedGameCatalogService gameCatalogService,
    IGameMaterializationService gameMaterializationService,
    IPublisherService publisherService,
    IGenreService genreService,
    IPlatformService platformService) : IGameLookupService
{
    public Task<PagedGamesDto> GetGamesAsync(GameQueryParams parameters)
    {
        return gameCatalogService.GetGamesAsync(parameters);
    }

    public Task<GameDto> GetByKeyAsync(string key)
    {
        return gameCatalogService.GetByKeyAsync(key);
    }

    public Task<GameDto> GetByIdAsync(Guid id)
    {
        return gameCatalogService.GetByIdAsync(id);
    }

    public async Task<PublisherDto> GetPublisherByGameKeyAsync(string key)
    {
        await gameMaterializationService.GetOrCreateAsync(key);
        return await publisherService.GetByGameKeyAsync(key);
    }

    public async Task<IEnumerable<GenreDto>> GetGenresByGameKeyAsync(string key)
    {
        await gameMaterializationService.GetOrCreateAsync(key);
        return await genreService.GetByGameKeyAsync(key);
    }

    public async Task<IEnumerable<PlatformDto>> GetPlatformsByGameKeyAsync(string key)
    {
        await gameMaterializationService.GetOrCreateAsync(key);
        return await platformService.GetByGameKeyAsync(key);
    }
}
