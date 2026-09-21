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

    public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
    {
        var roleName = dto.Role.Name.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ValidationException("Role name is required.");
        }

        var existingRole = await unitOfWork.Roles.GetSingleAsync(role => role.Name == roleName);
        if (existingRole != null)
        {
            throw new EntityAlreadyExistsException(nameof(Role), "name", roleName);
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
        };

        await unitOfWork.Roles.AddAsync(role);
        await unitOfWork.SaveChangesAsync();

        return mapper.Map<RoleDto>(role);
    }
}
