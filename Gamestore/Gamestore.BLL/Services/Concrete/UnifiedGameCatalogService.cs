using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Helpers;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;

namespace Gamestore.BLL.Services.Concrete;

public sealed class UnifiedGameCatalogService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    INorthwindProductService northwindProductService) : IUnifiedGameCatalogService
{
    public async Task<PagedGamesDto> GetGamesAsync(GameQueryParams parameters)
    {
        var sqlGames = await unitOfWork.Games.GetAllWithDetailsAsync();
        var catalog = sqlGames
            .Select(CreateSqlItem)
            .ToList();

        var sqlNames = catalog
            .Select(item => NormalizeName(item.Game.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sqlKeys = catalog
            .Select(item => item.Game.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mongoItems = northwindProductService
            .GetAll()
            .Where(game => !sqlNames.Contains(NormalizeName(game.Name)))
            .Where(game => !sqlKeys.Contains(game.Key))
            .GroupBy(game => NormalizeName(game.Name), StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateMongoItem(group.First()))
            .ToList();

        catalog.AddRange(mongoItems);

        IEnumerable<CatalogItem> filtered = catalog;

        var genreIds = parameters.Genres;
        if (genreIds is { Count: > 0 })
        {
            filtered = filtered.Where(item => !item.IsMongo
                && item.GenreIds.Any(genreIds.Contains));
        }

        var platformIds = parameters.Platforms;
        if (platformIds is { Count: > 0 })
        {
            filtered = filtered.Where(item => !item.IsMongo
                && item.PlatformIds.Any(platformIds.Contains));
        }

        var publisherIds = parameters.Publishers;
        if (publisherIds is { Count: > 0 })
        {
            filtered = filtered.Where(item => !item.IsMongo
                && publisherIds.Contains(item.PublisherId));
        }

        if (parameters.MinPrice.HasValue)
        {
            filtered = filtered.Where(item => item.Game.Price >= parameters.MinPrice.Value);
        }

        if (parameters.MaxPrice.HasValue)
        {
            filtered = filtered.Where(item => item.Game.Price <= parameters.MaxPrice.Value);
        }

        var name = parameters.Name?.Trim();
        if (name?.Length >= 3)
        {
            filtered = filtered.Where(item => item.Game.Name.Contains(name));
        }

        var cutoff = PublishDateOptionsHelper.ResolveCutoff(
            parameters.DatePublishing,
            DateTime.UtcNow);
        if (cutoff.HasValue)
        {
            filtered = filtered.Where(item => item.CreatedAt >= cutoff.Value);
        }

        var sorted = ApplySorting(filtered, parameters.Sort);
        var filteredItems = sorted.ToList();
        var pageSize = PaginationOptionsHelper.ResolvePageSize(parameters.PageCount);
        var totalPages = pageSize is null
            ? 1
            : Math.Max(1, (int)Math.Ceiling(filteredItems.Count / (double)pageSize.Value));
        var currentPage = Math.Clamp(parameters.Page, 1, totalPages);

        if (pageSize is not null)
        {
            filteredItems = [.. filteredItems
                .Skip(pageSize.Value * (currentPage - 1))
                .Take(pageSize.Value)];
        }

        return new PagedGamesDto
        {
            Games = [.. filteredItems.Select(item => item.Game)],
            TotalPages = totalPages,
            CurrentPage = currentPage,
        };
    }

    private CatalogItem CreateSqlItem(Game game)
    {
        return new CatalogItem
        {
            Game = mapper.Map<GameDto>(game),
            IsMongo = false,
            PublisherId = game.PublisherId,
            GenreIds = [.. game.GameGenres.Select(relation => relation.GenreId)],
            PlatformIds = [.. game.GamePlatforms.Select(relation => relation.PlatformId)],
            ViewCount = game.ViewCount,
            CommentCount = game.Comments.Count(comment => !comment.IsDeleted),
            CreatedAt = game.CreatedAt,
        };
    }

    private static CatalogItem CreateMongoItem(GameDto game)
    {
        return new CatalogItem
        {
            Game = game,
            IsMongo = true,
            PublisherId = Guid.Empty,
            GenreIds = [],
            PlatformIds = [],
            ViewCount = 0,
            CommentCount = 0,
            CreatedAt = DateTime.MinValue,
        };
    }

    private static IEnumerable<CatalogItem> ApplySorting(
        IEnumerable<CatalogItem> items,
        string? sort)
    {
        return sort?.ToLowerInvariant() switch
        {
            "most popular" => items
                .OrderByDescending(item => item.ViewCount)
                .ThenBy(item => item.Game.Id),
            "most commented" => items
                .OrderByDescending(item => item.CommentCount)
                .ThenBy(item => item.Game.Id),
            "price asc" => items
                .OrderBy(item => item.Game.Price)
                .ThenBy(item => item.Game.Id),
            "price desc" => items
                .OrderByDescending(item => item.Game.Price)
                .ThenBy(item => item.Game.Id),
            "new" => items
                .OrderByDescending(item => item.CreatedAt)
                .ThenBy(item => item.Game.Id),
            _ => items
                .OrderBy(item => item.Game.Name)
                .ThenBy(item => item.Game.Id),
        };
    }

    private static string NormalizeName(string name)
    {
        return string.Join(
            ' ',
            name.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private sealed class CatalogItem
    {
        public required GameDto Game { get; init; }

        public bool IsMongo { get; init; }

        public Guid PublisherId { get; init; }

        public HashSet<Guid> GenreIds { get; init; } = [];

        public HashSet<Guid> PlatformIds { get; init; } = [];

        public int ViewCount { get; init; }

        public int CommentCount { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
