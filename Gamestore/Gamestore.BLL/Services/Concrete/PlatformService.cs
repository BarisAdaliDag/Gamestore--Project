using Gamestore.BLL.DTOs.Platforms;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Gamestore.BLL.Services.Concrete;

public class PlatformService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<PlatformService> logger)
    : BaseService<PlatformDto, Platform>(unitOfWork, mapper), IPlatformService
{
    private readonly ILogger<PlatformService> _logger = logger;

    protected override IRepository<Platform> Repository => UnitOfWork.Platforms;

    public async Task<IEnumerable<PlatformDto>> GetByGameKeyAsync(string gameKey)
    {
        var platforms = await UnitOfWork.Platforms.GetByGameKeyAsync(gameKey);
        return Mapper.Map<IEnumerable<PlatformDto>>(platforms);
    }

    public async Task<PlatformDto> CreateAsync(CreatePlatformDto dto)
    {
        _logger.LogInformation("Creating platform: {Type}", dto.Platform.Type);

        var existing = await UnitOfWork.Platforms.GetSingleAsync(p => p.Type == dto.Platform.Type);
        if (existing != null)
        {
            throw new EntityAlreadyExistsException(nameof(Platform), "type", dto.Platform.Type);
        }

        var platform = new Platform
        {
            Id = Guid.NewGuid(),
            Type = dto.Platform.Type,
        };

        await UnitOfWork.Platforms.AddAsync(platform);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Platform created: {Type} ({Id})", platform.Type, platform.Id);

        return Mapper.Map<PlatformDto>(platform);
    }

    public async Task<PlatformDto> UpdateAsync(UpdatePlatformDto dto)
    {
        _logger.LogInformation("Updating platform: {Id}", dto.Platform.Id);

        var platform = await UnitOfWork.Platforms.GetByIdAsync(dto.Platform.Id)
            ?? throw new NotFoundException(nameof(Platform), dto.Platform.Id);

        var existing = await UnitOfWork.Platforms.GetSingleAsync(
            p => p.Type == dto.Platform.Type && p.Id != dto.Platform.Id);
        if (existing != null)
        {
            throw new EntityAlreadyExistsException(nameof(Platform), "type", dto.Platform.Type);
        }

        platform.Type = dto.Platform.Type;

        UnitOfWork.Platforms.Update(platform);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Platform updated: {Type} ({Id})", platform.Type, platform.Id);

        return Mapper.Map<PlatformDto>(platform);
    }
}
