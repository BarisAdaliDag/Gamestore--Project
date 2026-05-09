using Gamestore.DAL.Context;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.Repositories.Concrete;
using Gamestore.Domain.Entities;

namespace Gamestore.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly GamestoreDbContext _context;
    private bool _disposed;

    public UnitOfWork(GamestoreDbContext context)
    {
        _context = context;
        Games = new GameRepository(_context);
        Genres = new GenreRepository(_context);
        Platforms = new PlatformRepository(_context);
        GameGenres = new Repository<GameGenre>(_context);
        GamePlatforms = new Repository<GamePlatform>(_context);
    }

    public IGameRepository Games { get; }

    public IGenreRepository Genres { get; }

    public IPlatformRepository Platforms { get; }

    public IRepository<GameGenre> GameGenres { get; }

    public IRepository<GamePlatform> GamePlatforms { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }

        _disposed = true;
    }
}