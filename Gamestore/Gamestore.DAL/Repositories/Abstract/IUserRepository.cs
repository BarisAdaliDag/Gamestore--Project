using Gamestore.Domain.Entities;

namespace Gamestore.DAL.Repositories.Abstract;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByNameWithRolesAsync(string name);
}
