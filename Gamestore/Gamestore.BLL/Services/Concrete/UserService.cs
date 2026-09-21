using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.DTOs.Users;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Constants;
using Gamestore.Domain.Entities;
using MapsterMapper;

namespace Gamestore.BLL.Services.Concrete;

public class UserService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IPasswordHashService passwordHashService) : IUserService
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

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var userName = dto.User.Name.Trim();
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ValidationException("User name and password are required.");
        }

        var roleIds = dto.Roles.Distinct().ToList();
        if (roleIds.Count == 0 || roleIds.Count != dto.Roles.Count)
        {
            throw new ValidationException("At least one unique role is required.");
        }

        var existingUser = await unitOfWork.Users.GetSingleAsync(user => user.Name == userName);
        if (existingUser != null)
        {
            throw new EntityAlreadyExistsException(nameof(User), "name", userName);
        }

        var roles = (await unitOfWork.Roles.FindAsync(role => roleIds.Contains(role.Id))).ToList();
        if (roles.Count != roleIds.Count)
        {
            throw new ValidationException("One or more selected roles do not exist.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = userName,
            PasswordHash = string.Empty,
        };
        user.PasswordHash = passwordHashService.HashPassword(user, dto.Password);

        await unitOfWork.Users.AddAsync(user);
        foreach (var roleId in roleIds)
        {
            await unitOfWork.UserRoles.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
            });
        }

        await unitOfWork.SaveChangesAsync();
        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateAsync(UpdateUserDto dto)
    {
        var userName = dto.User.Name.Trim();
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ValidationException("User name and password are required.");
        }

        var roleIds = dto.Roles.Distinct().ToList();
        if (roleIds.Count == 0 || roleIds.Count != dto.Roles.Count)
        {
            throw new ValidationException("At least one unique role is required.");
        }

        var user = await unitOfWork.Users.GetByIdWithRolesForUpdateAsync(dto.User.Id)
            ?? throw new NotFoundException(nameof(User), dto.User.Id);
        var duplicateUser = await unitOfWork.Users.GetSingleAsync(
            existingUser => existingUser.Name == userName && existingUser.Id != user.Id);
        if (duplicateUser != null)
        {
            throw new EntityAlreadyExistsException(nameof(User), "name", userName);
        }

        var roles = (await unitOfWork.Roles.FindAsync(role => roleIds.Contains(role.Id))).ToList();
        if (roles.Count != roleIds.Count)
        {
            throw new ValidationException("One or more selected roles do not exist.");
        }

        var existingRoleIds = user.UserRoles.Select(userRole => userRole.RoleId).ToHashSet();
        var removedUserRoles = user.UserRoles
            .Where(userRole => !roleIds.Contains(userRole.RoleId))
            .ToList();
        foreach (var userRole in removedUserRoles)
        {
            unitOfWork.UserRoles.Delete(userRole);
        }

        foreach (var roleId in roleIds.Where(roleId => !existingRoleIds.Contains(roleId)))
        {
            await unitOfWork.UserRoles.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
            });
        }

        user.Name = userName;
        user.PasswordHash = passwordHashService.HashPassword(user, dto.Password);

        await unitOfWork.SaveChangesAsync();
        return mapper.Map<UserDto>(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await unitOfWork.Users.GetByIdWithRolesForUpdateAsync(id)
            ?? throw new NotFoundException(nameof(User), id);
        var isAdministrator = user.UserRoles.Any(
            userRole => userRole.Role?.Name == RoleNames.Administrator);

        if (isAdministrator
            && await unitOfWork.Users.CountByRoleNameAsync(RoleNames.Administrator) <= 1)
        {
            throw new ValidationException("The last Administrator user cannot be deleted.");
        }

        foreach (var userRole in user.UserRoles)
        {
            unitOfWork.UserRoles.Delete(userRole);
        }

        unitOfWork.Users.Delete(user);
        await unitOfWork.SaveChangesAsync();
    }
}
