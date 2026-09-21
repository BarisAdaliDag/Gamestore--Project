using Gamestore.BLL.DTOs.Roles;

namespace Gamestore.BLL.Services.Abstract;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllAsync();

    Task<RoleDto> GetByIdAsync(Guid id);

    Task<RoleDto> CreateAsync(CreateRoleDto dto);
}
