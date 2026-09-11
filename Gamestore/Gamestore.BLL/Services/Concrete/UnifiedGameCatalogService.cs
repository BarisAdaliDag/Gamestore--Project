using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Exceptions;
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

        var sqlPublisherIds = (await unitOfWork.Publishers.GetAllAsync())
            .ToDictionary(
                publisher => NorthwindReferenceIdentity.Normalize(publisher.CompanyName),
                publisher => publisher.Id,
                StringComparer.OrdinalIgnoreCase);

        var sqlGenreIds = (await unitOfWork.Genres.GetAllAsync())
            .ToDictionary(
                genre => NorthwindReferenceIdentity.Normalize(genre.Name),
                genre => genre.Id,
                StringComparer.OrdinalIgnoreCase);

        var mongoItems = (await northwindProductService
            .GetAllWithReferencesAsync())
            .Where(item => !sqlNames.Contains(NormalizeName(item.Game.Name)))
            .Where(item => !sqlKeys.Contains(item.Game.Key))
            .GroupBy(item => NormalizeName(item.Game.Name), StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateMongoItem(group.First(), sqlPublisherIds, sqlGenreIds))
            .ToList();

        catalog.AddRange(mongoItems);

        IEnumerable<CatalogItem> filtered = catalog;

        var genreIds = parameters.Genres;
        if (genreIds is { Count: > 0 })
        {
            filtered = filtered.Where(item => item.GenreIds.Any(genreIds.Contains));
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
            filtered = filtered.Where(item => publisherIds.Contains(item.PublisherId));
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

    public async Task<GameDto> GetByKeyAsync(string key)
    {
        var sqlGame = await unitOfWork.Games.GetByKeyAsync(key);
        if (sqlGame is not null)
        {
            await unitOfWork.Games.IncrementViewCountAsync(sqlGame.Id);
            return mapper.Map<GameDto>(sqlGame);
        }

        var mongoGame = await northwindProductService.GetByKeyAsync(key);
        return mongoGame
            ?? throw new NotFoundException(nameof(Game), key);
    }

    public async Task<GameDto> GetByIdAsync(Guid id)
    {
        var sqlGame = await unitOfWork.Games.GetByIdAsync(id);
        if (sqlGame is not null)
        {
            return mapper.Map<GameDto>(sqlGame);
        }

        var mongoGame = await northwindProductService.GetByIdAsync(id);
        return mongoGame
            ?? throw new NotFoundException(nameof(Game), id);
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

    private static CatalogItem CreateMongoItem(
        NorthwindGameCatalogItem item,
        IReadOnlyDictionary<string, Guid> publisherIds,
        IReadOnlyDictionary<string, Guid> genreIds)
    {
        var publisherId = item.SupplierName is not null
            ? publisherIds.GetValueOrDefault(
                NorthwindReferenceIdentity.Normalize(item.SupplierName),
                NorthwindReferenceIdentity.CreateSupplierId(item.SupplierName))
            : Guid.Empty;

        var genreId = item.CategoryName is not null
            ? genreIds.GetValueOrDefault(
                NorthwindReferenceIdentity.Normalize(item.CategoryName),
                NorthwindReferenceIdentity.CreateCategoryId(item.CategoryName))
            : Guid.Empty;

        return new CatalogItem
        {
            Game = item.Game,
            IsMongo = true,
            PublisherId = publisherId,
            GenreIds = genreId == Guid.Empty ? [] : [genreId],
            PlatformIds = [],
            ViewCount = item.ViewCount,
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

    private static string NormalizeName(string name) => NorthwindReferenceIdentity.Normalize(name);

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
