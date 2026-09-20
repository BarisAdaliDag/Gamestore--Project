using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.DTOs.Users;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;

namespace Gamestore.BLL.Services.Concrete;

public class UserService(IUnitOfWork unitOfWork, IMapper mapper) : IUserService
{
    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await unitOfWork.Users.GetAllAsync();
        return mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(User), id);

        return mapper.Map<UserDto>(user);
    }

    public async Task<IEnumerable<RoleDto>> GetRolesAsync(Guid id)
    {
        var user = await unitOfWork.Users.GetByIdWithRolesAsync(id)
            ?? throw new NotFoundException(nameof(User), id);
        var roles = user.UserRoles
            .Where(userRole => userRole.Role != null)
            .Select(userRole => userRole.Role!);

        return mapper.Map<IEnumerable<RoleDto>>(roles);
    }
}
