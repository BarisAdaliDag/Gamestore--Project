using Gamestore.BLL.DTOs.Abstract;
using Gamestore.BLL.DTOs.Common;

namespace Gamestore.BLL.Services.Abstract;

public interface IService<TDto>
    where TDto : class, IDto
{
    Task<IEnumerable<TDto>> GetAllAsync();

    Task<PagedResultDto<TDto>> GetPagedAsync(PaginationParamsDto paginationParams);

    Task<TDto> GetByIdAsync(Guid id);

    Task DeleteAsync(Guid id);
}
