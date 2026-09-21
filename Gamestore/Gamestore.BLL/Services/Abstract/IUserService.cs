using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.DTOs.Users;

namespace Gamestore.BLL.Services.Abstract;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();

    Task<UserDto> GetByIdAsync(Guid id);

    Task<IEnumerable<RoleDto>> GetRolesAsync(Guid id);

    Task<UserDto> CreateAsync(CreateUserDto dto);

    Task<UserDto> UpdateAsync(UpdateUserDto dto);
}
