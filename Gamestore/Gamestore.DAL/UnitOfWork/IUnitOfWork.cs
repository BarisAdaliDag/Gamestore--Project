using Gamestore.DAL.Repositories.Abstract;

namespace Gamestore.DAL.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IGameRepository Games { get; }

    IGenreRepository Genres { get; }

    IPlatformRepository Platforms { get; }

    IPublisherRepository Publishers { get; }

    IOrderRepository Orders { get; }

    IRepository<Domain.Entities.GameGenre> GameGenres { get; }

    IRepository<Domain.Entities.GamePlatform> GamePlatforms { get; }

    IRepository<Domain.Entities.OrderGame> OrderGames { get; }

    Task<int> SaveChangesAsync();
}
