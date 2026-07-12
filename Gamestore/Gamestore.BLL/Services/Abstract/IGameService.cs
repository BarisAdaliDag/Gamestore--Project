using Gamestore.BLL.DTOs.Common;
using Gamestore.BLL.DTOs.Games;

namespace Gamestore.BLL.Services.Abstract;

public interface IGameService : IService<GameDto>
{
    Task<int> GetTotalCountAsync();

    Task<GameDto> GetByKeyAsync(string key);

    Task<PagedResultDto<GameDto>> GetFilteredAsync(GameFilterDto filter);

    Task<IEnumerable<GameDto>> GetByGenreIdAsync(Guid genreId);

    Task<PagedResultDto<GameDto>> GetByGenreIdAsync(Guid genreId, PaginationParamsDto paginationParams);

    Task<IEnumerable<GameDto>> GetByPlatformIdAsync(Guid platformId);

    Task<PagedResultDto<GameDto>> GetByPlatformIdAsync(Guid platformId, PaginationParamsDto paginationParams);

    Task<IEnumerable<GameDto>> GetByPublisherNameAsync(string companyName);

    Task<GameDto> CreateAsync(CreateGameDto dto);

    Task<GameDto> UpdateAsync(UpdateGameDto dto);

    Task DeleteByKeyAsync(string key);

    Task<(byte[] FileBytes, string FileName)> GenerateGameFileAsync(string key);
}
