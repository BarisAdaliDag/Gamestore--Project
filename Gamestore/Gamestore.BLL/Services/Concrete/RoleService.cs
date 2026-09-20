using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;

namespace Gamestore.BLL.Services.Concrete;

public class RoleService(IUnitOfWork unitOfWork, IMapper mapper) : IRoleService
{
    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await unitOfWork.Roles.GetAllAsync();
        var orderedRoles = roles.OrderBy(role => role.Name);

        return mapper.Map<IEnumerable<RoleDto>>(orderedRoles);
    }

    public async Task<RoleDto> GetByIdAsync(Guid id)
    {
        var role = await unitOfWork.Roles.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Role), id);

        return mapper.Map<RoleDto>(role);
    }
}
