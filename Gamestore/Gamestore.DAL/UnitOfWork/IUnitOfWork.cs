using Gamestore.DAL.Repositories.Abstract;

namespace Gamestore.DAL.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IGameRepository Games { get; }

    IGenreRepository Genres { get; }

    IPlatformRepository Platforms { get; }

    IPublisherRepository Publishers { get; }

    IRepository<Domain.Entities.GameGenre> GameGenres { get; }

    IRepository<Domain.Entities.GamePlatform> GamePlatforms { get; }

    Task<int> SaveChangesAsync();
}
