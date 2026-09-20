using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.DAL.Seed;

public static class DbSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedPublishers(modelBuilder);
        SeedPlatforms(modelBuilder);
        SeedGenres(modelBuilder);
        SeedRoles(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                Name = Gamestore.Domain.Constants.RoleNames.Administrator,
            },
            new Role
            {
                Id = Guid.Parse("90000000-0000-0000-0000-000000000002"),
                Name = Gamestore.Domain.Constants.RoleNames.Manager,
            },
            new Role
            {
                Id = Guid.Parse("90000000-0000-0000-0000-000000000003"),
                Name = Gamestore.Domain.Constants.RoleNames.Moderator,
            },
            new Role
            {
                Id = Guid.Parse("90000000-0000-0000-0000-000000000004"),
                Name = Gamestore.Domain.Constants.RoleNames.User,
            });
    }

    private static void SeedPublishers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Publisher>().HasData(
            new Publisher
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555551"),
                CompanyName = "Electronic Arts",
                HomePage = "https://www.ea.com",
                Description = "Electronic Arts Inc.",
            },
            new Publisher
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555552"),
                CompanyName = "Ubisoft",
                HomePage = "https://www.ubisoft.com",
                Description = "Ubisoft Entertainment SA",
            },
            new Publisher
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555553"),
                CompanyName = "Valve",
                HomePage = "https://www.valvesoftware.com",
                Description = "Valve Corporation",
            });
    }

    private static void SeedPlatforms(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Platform>().HasData(
            new Platform { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Type = "Mobile" },
            new Platform { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Type = "Browser" },
            new Platform { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Type = "Desktop" },
            new Platform { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Type = "Console" });
    }

    private static void SeedGenres(ModelBuilder modelBuilder)
    {
        var strategyId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var rpgId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
        var sportsId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
        var racesId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004");
        var actionId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005");
        var adventureId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000006");
        var puzzleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000007");

        // Top-level genres
        modelBuilder.Entity<Genre>().HasData(
            new Genre { Id = strategyId, Name = "Strategy", ParentGenreId = null },
            new Genre { Id = rpgId, Name = "RPG", ParentGenreId = null },
            new Genre { Id = sportsId, Name = "Sports", ParentGenreId = null },
            new Genre { Id = racesId, Name = "Races", ParentGenreId = null },
            new Genre { Id = actionId, Name = "Action", ParentGenreId = null },
            new Genre { Id = adventureId, Name = "Adventure", ParentGenreId = null },
            new Genre { Id = puzzleId, Name = "Puzzle & Skill", ParentGenreId = null });

        // Strategy children
        modelBuilder.Entity<Genre>().HasData(
            new Genre { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"), Name = "RTS", ParentGenreId = strategyId },
            new Genre { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), Name = "TBS", ParentGenreId = strategyId });

        // Races children
        modelBuilder.Entity<Genre>().HasData(
            new Genre { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000001"), Name = "Rally", ParentGenreId = racesId },
            new Genre { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000002"), Name = "Arcade", ParentGenreId = racesId },
            new Genre { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000003"), Name = "Formula", ParentGenreId = racesId },
            new Genre { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000004"), Name = "Off-road", ParentGenreId = racesId });

        // Action children
        modelBuilder.Entity<Genre>().HasData(
            new Genre { Id = Guid.Parse("dddddddd-0000-0000-0000-000000000001"), Name = "FPS", ParentGenreId = actionId },
            new Genre { Id = Guid.Parse("dddddddd-0000-0000-0000-000000000002"), Name = "TPS", ParentGenreId = actionId });
    }
}
