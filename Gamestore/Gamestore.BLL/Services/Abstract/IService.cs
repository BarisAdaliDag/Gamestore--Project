using Gamestore.BLL.DTOs.Abstract;

namespace Gamestore.BLL.Services.Abstract;

public interface IService<TDto>
    where TDto : class, IDto
{
    Task<IEnumerable<TDto>> GetAllAsync();

    Task<TDto> GetByIdAsync(Guid id);

    Task DeleteAsync(Guid id);
}