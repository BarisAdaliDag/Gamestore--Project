using Gamestore.Domain.Entities;

namespace Gamestore.DAL.Repositories.Abstract;

public interface IPublisherRepository : IRepository<Publisher>
{
    Task<Publisher?> GetByCompanyNameAsync(string companyName);

    Task<Publisher?> GetByGameKeyAsync(string gameKey);
}
