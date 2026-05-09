using Gamestore.BLL.DTOs.Platforms;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;

namespace Gamestore.BLL.Services.Concrete;

public class PlatformService(IUnitOfWork unitOfWork, IMapper mapper)
    : BaseService<PlatformDto, Platform>(unitOfWork, mapper), IPlatformService
{
    protected override IRepository<Platform> Repository => UnitOfWork.Platforms;

    public async Task<IEnumerable<PlatformDto>> GetByGameKeyAsync(string gameKey)
    {
        var platforms = await UnitOfWork.Platforms.GetByGameKeyAsync(gameKey);
        return Mapper.Map<IEnumerable<PlatformDto>>(platforms);
    }

    public async Task<PlatformDto> CreateAsync(CreatePlatformDto dto)
    {
        var existing = await UnitOfWork.Platforms.GetSingleAsync(p => p.Type == dto.Platform.Type);
        if (existing != null)
        {
            throw new ValidationException($"Platform with type '{dto.Platform.Type}' already exists.");
        }

        var platform = new Platform
        {
            Id = Guid.NewGuid(),
            Type = dto.Platform.Type,
        };

        await UnitOfWork.Platforms.AddAsync(platform);
        await UnitOfWork.SaveChangesAsync();

        return Mapper.Map<PlatformDto>(platform);
    }

    public async Task<PlatformDto> UpdateAsync(UpdatePlatformDto dto)
    {
        var platform = await UnitOfWork.Platforms.GetByIdAsync(dto.Platform.Id)
            ?? throw new NotFoundException(nameof(Platform), dto.Platform.Id);

        platform.Type = dto.Platform.Type;

        UnitOfWork.Platforms.Update(platform);
        await UnitOfWork.SaveChangesAsync();

        return Mapper.Map<PlatformDto>(platform);
    }
}
