using Gamestore.DAL.Repositories.Abstract;
using Gamestore.Domain.Entities;

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

    ICommentRepository Comments { get; }

    IRepository<Ban> Bans { get; }

    IUserRepository Users { get; }

    IRepository<Role> Roles { get; }

    IRepository<UserRole> UserRoles { get; }

    Task<int> SaveChangesAsync();
}
