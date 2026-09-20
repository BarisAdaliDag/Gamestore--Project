using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.DTOs.Platforms;
using Gamestore.BLL.DTOs.Publishers;

namespace Gamestore.BLL.Services.Abstract;

public interface IGameLookupService
{
    Task<PagedGamesDto> GetGamesAsync(GameQueryParams parameters);

    Task<GameDto> GetByKeyAsync(string key);

    Task<GameDto> GetByIdAsync(Guid id);

    Task<PublisherDto> GetPublisherByGameKeyAsync(string key);

    Task<IEnumerable<GenreDto>> GetGenresByGameKeyAsync(string key);

    Task<IEnumerable<PlatformDto>> GetPlatformsByGameKeyAsync(string key);
}
