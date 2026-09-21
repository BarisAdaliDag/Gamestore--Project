using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Constants;
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

    public async Task<RoleDto> UpdateAsync(UpdateRoleDto dto)
    {
        var roleName = dto.Role.Name.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ValidationException("Role name is required.");
        }

        var role = await unitOfWork.Roles.GetByIdAsync(dto.Role.Id)
            ?? throw new NotFoundException(nameof(Role), dto.Role.Id);
        if (IsDefaultRole(role.Name) && role.Name != roleName)
        {
            throw new ValidationException("Default roles cannot be renamed.");
        }

        var duplicateRole = await unitOfWork.Roles.GetSingleAsync(
            existingRole => existingRole.Name == roleName && existingRole.Id != role.Id);
        if (duplicateRole != null)
        {
            throw new EntityAlreadyExistsException(nameof(Role), "name", roleName);
        }

        role.Name = roleName;
        unitOfWork.Roles.Update(role);
        await unitOfWork.SaveChangesAsync();

        return mapper.Map<RoleDto>(role);
    }

    private static bool IsDefaultRole(string roleName)
    {
        return roleName is RoleNames.Administrator
            or RoleNames.Manager
            or RoleNames.Moderator
            or RoleNames.User;
    }
}
