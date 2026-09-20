using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.DTOs.Users;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;
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
}
