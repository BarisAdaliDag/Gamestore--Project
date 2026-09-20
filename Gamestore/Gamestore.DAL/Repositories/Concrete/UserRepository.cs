using Gamestore.DAL.Context;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.DAL.Repositories.Concrete;

public class UserRepository(GamestoreDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByNameWithRolesAsync(string name)
    {
        return await DbSet
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(user => user.Name == name);
    }
}
