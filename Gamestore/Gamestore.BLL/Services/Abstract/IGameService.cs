using Gamestore.BLL.DTOs.Games;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Services.Abstract;

public interface IGameService : IService<GameDto, Game>
{
    Task<GameDto> GetByKeyAsync(string key);

    Task<IEnumerable<GameDto>> GetByGenreIdAsync(Guid genreId);

    Task<IEnumerable<GameDto>> GetByPlatformIdAsync(Guid platformId);

    Task<GameDto> CreateAsync(CreateGameDto dto);

    Task<GameDto> UpdateAsync(UpdateGameDto dto);

    Task DeleteByKeyAsync(string key);

    Task<(byte[] FileBytes, string FileName)> GenerateGameFileAsync(string key);
}
