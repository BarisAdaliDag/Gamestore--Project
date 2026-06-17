using Gamestore.Domain.Entities;

namespace Gamestore.DAL.Repositories.Abstract;

public interface IPlatformRepository : IRepository<Platform>
{
    Task<IEnumerable<Platform>> GetByGameKeyAsync(string gameKey);
}