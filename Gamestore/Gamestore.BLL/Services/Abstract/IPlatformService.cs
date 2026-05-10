using Gamestore.BLL.DTOs.Platforms;

namespace Gamestore.BLL.Services.Abstract;

public interface IPlatformService : IService<PlatformDto>
{
    Task<IEnumerable<PlatformDto>> GetByGameKeyAsync(string gameKey);

    Task<PlatformDto> CreateAsync(CreatePlatformDto dto);

    Task<PlatformDto> UpdateAsync(UpdatePlatformDto dto);
}