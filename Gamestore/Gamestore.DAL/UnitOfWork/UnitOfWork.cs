using Gamestore.DAL.Context;
using Gamestore.DAL.Mongo;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.Repositories.Concrete;
using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Gamestore.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly GamestoreDbContext _context;
    private readonly IEntityChangeLogWriter? _changeLogWriter;
    private bool _disposed;

    public UnitOfWork(
        GamestoreDbContext context,
        IEntityChangeLogWriter? changeLogWriter = null)
    {
        _context = context;
        _changeLogWriter = changeLogWriter;
        Games = new GameRepository(_context);
        Genres = new GenreRepository(_context);
        Platforms = new PlatformRepository(_context);
        Publishers = new PublisherRepository(_context);
        Orders = new OrderRepository(_context);
        GameGenres = new Repository<GameGenre>(_context);
        GamePlatforms = new Repository<GamePlatform>(_context);
        OrderGames = new Repository<OrderGame>(_context);
        Comments = new CommentRepository(_context);
        Bans = new Repository<Ban>(_context);
        Users = new UserRepository(_context);
        Roles = new Repository<Role>(_context);
        UserRoles = new Repository<UserRole>(_context);
    }

    public IGameRepository Games { get; }

    public IGenreRepository Genres { get; }

    public IPlatformRepository Platforms { get; }

    public IPublisherRepository Publishers { get; }

    public IOrderRepository Orders { get; }

    public IRepository<GameGenre> GameGenres { get; }

    public IRepository<GamePlatform> GamePlatforms { get; }

    public IRepository<OrderGame> OrderGames { get; }

    public ICommentRepository Comments { get; }

    public IRepository<Ban> Bans { get; }

    public IUserRepository Users { get; }

    public IRepository<Role> Roles { get; }

    public IRepository<UserRole> UserRoles { get; }

    public async Task<int> SaveChangesAsync()
    {
        var changes = CaptureChanges();
        var result = await _context.SaveChangesAsync();

        if (_changeLogWriter != null)
        {
            foreach (var change in changes)
            {
                await _changeLogWriter.WriteAsync(
                    change.Action,
                    change.EntityType,
                    change.EntityId,
                    change.OldVersion,
                    change.NewVersion);
            }
        }

        return result;
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

    private List<EntityChange> CaptureChanges()
    {
        return [.. _context.ChangeTracker
            .Entries()
            .Where(entry => entry.State is not EntityState.Unchanged and not EntityState.Detached)
            .Select(entry =>
            {
                var oldVersion = entry.State == EntityState.Added
                    ? null
                    : Snapshot(entry, useOriginalValues: true);
                var newVersion = entry.State == EntityState.Deleted
                    ? null
                    : Snapshot(entry, useOriginalValues: false);

                return new EntityChange(
                    entry.State switch
                    {
                        EntityState.Added => "Create",
                        EntityState.Deleted => "Delete",
                        EntityState.Modified => "Update",
                        EntityState.Unchanged => "Update",
                        EntityState.Detached => "Update",
                        _ => "Update",
                    },
                    entry.Metadata.ClrType.Name,
                    GetEntityId(entry),
                    oldVersion,
                    newVersion);
            })
            ];
    }

    private static string GetEntityId(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var keyValues = entry.Metadata.FindPrimaryKey()?.Properties
            .Select(property => entry.Property(property.Name).CurrentValue?.ToString() ?? string.Empty)
            .ToArray() ?? [];

        return string.Join(",", keyValues);
    }

    private static BsonDocument Snapshot(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        bool useOriginalValues)
    {
        var document = new BsonDocument();

        foreach (var property in entry.Properties)
        {
            if (entry.Metadata.ClrType == typeof(User)
                && property.Metadata.Name == nameof(User.PasswordHash))
            {
                continue;
            }

            var value = useOriginalValues
                ? property.OriginalValue
                : property.CurrentValue;

            document[property.Metadata.Name] = ToBsonValue(value);
        }

        return document;
    }

    private static BsonValue ToBsonValue(object? value)
    {
        if (value is null)
        {
            return BsonNull.Value;
        }

        if (value is Guid guid)
        {
            return guid.ToString();
        }

        if (value is DateTime dateTime)
        {
            return new BsonDateTime(dateTime);
        }

        if (value.GetType().IsEnum)
        {
            return value.ToString() ?? string.Empty;
        }

        try
        {
            return BsonValue.Create(value);
        }
        catch (ArgumentException)
        {
            return value.ToString() ?? string.Empty;
        }
    }
}
