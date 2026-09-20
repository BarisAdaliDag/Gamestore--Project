using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Constants;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Services.Concrete;

public class DefaultAdministratorService(
    IUnitOfWork unitOfWork,
    IPasswordHashService passwordHashService) : IDefaultAdministratorService
{
    public async Task<bool> EnsureCreatedAsync(string name, string password)
    {
        var administratorRole = await unitOfWork.Roles.GetSingleAsync(
            role => role.Name == RoleNames.Administrator)
            ?? throw new InvalidOperationException("The Administrator role is not configured.");

        var user = await unitOfWork.Users.GetByNameWithRolesAsync(name);

        if (user is not null)
        {
            if (user.UserRoles.Any(userRole => userRole.RoleId == administratorRole.Id))
            {
                return false;
            }

            await unitOfWork.UserRoles.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = administratorRole.Id,
            });

            await unitOfWork.SaveChangesAsync();
            return true;
        }

        user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            PasswordHash = string.Empty,
        };
        user.PasswordHash = passwordHashService.HashPassword(user, password);

        await unitOfWork.Users.AddAsync(user);
        await unitOfWork.UserRoles.AddAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = administratorRole.Id,
        });

        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
