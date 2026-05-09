using Gamestore.BLL.DTOs.Platforms;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Services.Abstract;

public interface IPlatformService : IService<PlatformDto, Platform>
{
    Task<IEnumerable<PlatformDto>> GetByGameKeyAsync(string gameKey);

    Task<PlatformDto> CreateAsync(CreatePlatformDto dto);

    Task<PlatformDto> UpdateAsync(UpdatePlatformDto dto);
}
