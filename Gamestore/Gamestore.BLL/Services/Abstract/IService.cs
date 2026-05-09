using Gamestore.BLL.DTOs.Abstract;

namespace Gamestore.BLL.Services.Abstract;

public interface IService<TDto, TEntity>
    where TDto : class, IDto
    where TEntity : class
{
    Task<IEnumerable<TDto>> GetAllAsync();

    Task<TDto> GetByIdAsync(Guid id);

    Task DeleteAsync(Guid id);
}
