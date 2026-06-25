using Gamestore.DAL.Configurations;
using Gamestore.DAL.Seed;
using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.DAL.Context;

public class GamestoreDbContext(DbContextOptions<GamestoreDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games { get; set; }

    public DbSet<Genre> Genres { get; set; }

    public DbSet<Platform> Platforms { get; set; }

    public DbSet<GameGenre> GameGenres { get; set; }

    public DbSet<GamePlatform> GamePlatforms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new GameConfiguration());
        modelBuilder.ApplyConfiguration(new GenreConfiguration());
        modelBuilder.ApplyConfiguration(new PlatformConfiguration());
        modelBuilder.ApplyConfiguration(new GameGenreConfiguration());
        modelBuilder.ApplyConfiguration(new GamePlatformConfiguration());

        DbSeeder.Seed(modelBuilder);
    }
}