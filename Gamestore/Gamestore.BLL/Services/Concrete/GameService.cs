using System.Text;
using System.Text.Json;
using Gamestore.BLL.DTOs.Common;
using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Filtering;
using Gamestore.BLL.Helpers;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gamestore.BLL.Services.Concrete;

public class GameService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    GameQueryPipeline pipeline,
    ILogger<GameService> logger)
    : BaseService<GameDto, Game>(unitOfWork, mapper), IGameService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly ILogger<GameService> _logger = logger;

    public GameService(IUnitOfWork unitOfWork, IMapper mapper, GameQueryPipeline pipeline)
        : this(unitOfWork, mapper, pipeline, NullLogger<GameService>.Instance)
    {
    }

    protected override IRepository<Game> Repository => UnitOfWork.Games;

    public async Task<int> GetTotalCountAsync()
    {
        return await UnitOfWork.Games.CountAsync();
    }

    public async Task<GameDto> GetByKeyAsync(string key)
    {
        var game = await UnitOfWork.Games.GetByKeyAsync(key)
            ?? throw new NotFoundException(nameof(Game), key);

        // Atomically increment the view count on the GET /games/{key} endpoint.
        // On 404 (game not found) the counter is not incremented — correct behavior.
        await UnitOfWork.Games.IncrementViewCountAsync(game.Id);

        return Mapper.Map<GameDto>(game);
    }

    public async Task<PagedGamesDto> GetGamesAsync(GameQueryParams parameters)
    {
        _logger.LogInformation(
            "Getting games (page {Page}, size {PageCount})", parameters.Page, parameters.PageCount);

        var result = await pipeline.ExecuteAsync(UnitOfWork.Games.Query(), parameters);

        _logger.LogInformation(
            "Found {Count} games (page {Page}/{TotalPages})",
            result.Games.Count,
            result.CurrentPage,
            result.TotalPages);

        return new PagedGamesDto
        {
            Games = Mapper.Map<List<GameDto>>(result.Games),
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage,
        };
    }

    public IReadOnlyList<string> GetPaginationOptions()
    {
        return PaginationOptionsHelper.GetOptions();
    }

    public IReadOnlyList<string> GetSortingOptions()
    {
        return SortingOptionsHelper.GetOptions();
    }

    public IReadOnlyList<string> GetPublishDateOptions()
    {
        return PublishDateOptionsHelper.GetOptions();
    }

    public async Task<IEnumerable<GameDto>> GetByGenreIdAsync(Guid genreId)
    {
        var games = await UnitOfWork.Games.GetByGenreIdAsync(genreId);
        return Mapper.Map<IEnumerable<GameDto>>(games);
    }

    public async Task<PagedResultDto<GameDto>> GetByGenreIdAsync(
        Guid genreId,
        PaginationParamsDto paginationParams)
    {
        var (items, totalCount) = await UnitOfWork.Games.GetPagedAsync(
            paginationParams.Page,
            paginationParams.Size,
            game => game.GameGenres.Any(gameGenre => gameGenre.GenreId == genreId));

        return new PagedResultDto<GameDto>
        {
            Items = Mapper.Map<IEnumerable<GameDto>>(items),
            CurrentPage = paginationParams.Page,
            PageSize = paginationParams.Size,
            TotalCount = totalCount,
        };
    }

    public async Task<IEnumerable<GameDto>> GetByPlatformIdAsync(Guid platformId)
    {
        var games = await UnitOfWork.Games.GetByPlatformIdAsync(platformId);
        return Mapper.Map<IEnumerable<GameDto>>(games);
    }

    public async Task<PagedResultDto<GameDto>> GetByPlatformIdAsync(
        Guid platformId,
        PaginationParamsDto paginationParams)
    {
        var (items, totalCount) = await UnitOfWork.Games.GetPagedAsync(
            paginationParams.Page,
            paginationParams.Size,
            game => game.GamePlatforms.Any(gamePlatform => gamePlatform.PlatformId == platformId));

        return new PagedResultDto<GameDto>
        {
            Items = Mapper.Map<IEnumerable<GameDto>>(items),
            CurrentPage = paginationParams.Page,
            PageSize = paginationParams.Size,
            TotalCount = totalCount,
        };
    }

    public async Task<IEnumerable<GameDto>> GetByPublisherNameAsync(string companyName)
    {
        var games = await UnitOfWork.Games.GetByPublisherNameAsync(companyName);
        return Mapper.Map<IEnumerable<GameDto>>(games);
    }

    public async Task<GameDto> CreateAsync(CreateGameDto dto)
    {
        _logger.LogInformation("Creating game: {Name}", dto.Game.Name);

        var key = string.IsNullOrWhiteSpace(dto.Game.Key)
            ? GenerateKey(dto.Game.Name)
            : dto.Game.Key;

        var existing = await UnitOfWork.Games.GetByKeyAsync(key);
        if (existing != null)
        {
            throw new EntityAlreadyExistsException(nameof(Game), "key", key);
        }

        var (genreIds, platformIds) = await ValidateRelationIdsAsync(dto.Genres, dto.Platforms);
        await ValidatePublisherAsync(dto.Publisher);

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = dto.Game.Name,
            Key = key,
            Description = dto.Game.Description,
            Price = dto.Game.Price,
            UnitInStock = dto.Game.UnitInStock,
            Discount = dto.Game.Discount,
            PublisherId = dto.Publisher,
        };

        foreach (var genreId in genreIds)
        {
            game.GameGenres.Add(new GameGenre { GameId = game.Id, GenreId = genreId });
        }

        foreach (var platformId in platformIds)
        {
            game.GamePlatforms.Add(new GamePlatform { GameId = game.Id, PlatformId = platformId });
        }

        await UnitOfWork.Games.AddAsync(game);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Game created: {Name} (Key: {Key})", game.Name, game.Key);

        return Mapper.Map<GameDto>(game);
    }

    public async Task<GameDto> UpdateAsync(UpdateGameDto dto)
    {
        _logger.LogInformation("Updating game: {Id}", dto.Game.Id);

        var game = await UnitOfWork.Games.GetByIdWithDetailsAsync(dto.Game.Id)
            ?? throw new NotFoundException(nameof(Game), dto.Game.Id);

        var keyOwner = await UnitOfWork.Games.GetByKeyAsync(dto.Game.Key);
        if (keyOwner != null && keyOwner.Id != dto.Game.Id)
        {
            throw new EntityAlreadyExistsException(nameof(Game), "key", dto.Game.Key);
        }

        var (genreIds, platformIds) = await ValidateRelationIdsAsync(dto.Genres, dto.Platforms);
        await ValidatePublisherAsync(dto.Publisher);

        game.Name = dto.Game.Name;
        game.Key = dto.Game.Key;
        game.Description = dto.Game.Description;
        game.Price = dto.Game.Price;
        game.UnitInStock = dto.Game.UnitInStock;
        game.Discount = dto.Game.Discount;
        game.PublisherId = dto.Publisher;

        game.GameGenres.Clear();
        foreach (var genreId in genreIds)
        {
            game.GameGenres.Add(new GameGenre { GameId = game.Id, GenreId = genreId });
        }

        game.GamePlatforms.Clear();
        foreach (var platformId in platformIds)
        {
            game.GamePlatforms.Add(new GamePlatform { GameId = game.Id, PlatformId = platformId });
        }

        UnitOfWork.Games.Update(game);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Game updated: {Name} (Key: {Key})", game.Name, game.Key);

        return Mapper.Map<GameDto>(game);
    }

    public async Task DeleteByKeyAsync(string key)
    {
        _logger.LogInformation("Deleting game: {Key}", key);

        var game = await UnitOfWork.Games.GetByKeyAsync(key)
            ?? throw new NotFoundException(nameof(Game), key);

        var isInAnyOrder = (await UnitOfWork.OrderGames
            .FindAsync(orderGame => orderGame.ProductId == game.Id)).Any();
        if (isInAnyOrder)
        {
            throw new EntityInUseException(nameof(Game), key);
        }

        UnitOfWork.Games.Delete(game);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Game deleted: {Key}", key);
    }

    public async Task<(byte[] FileBytes, string FileName)> GenerateGameFileAsync(string key)
    {
        var game = await UnitOfWork.Games.GetByKeyAsync(key)
            ?? throw new NotFoundException(nameof(Game), key);

        var dto = Mapper.Map<GameDto>(game);
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"{game.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

        return (bytes, fileName);
    }

    private static string GenerateKey(string name)
    {
        return name.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);
    }

    private async Task ValidatePublisherAsync(Guid publisherId)
    {
        _ = await UnitOfWork.Publishers.GetByIdAsync(publisherId)
            ?? throw new ValidationException($"Publisher with id '{publisherId}' not found.");
    }

    private async Task<(List<Guid> GenreIds, List<Guid> PlatformIds)> ValidateRelationIdsAsync(
        ICollection<Guid> genreIds,
        ICollection<Guid> platformIds)
    {
        var uniqueGenreIds = genreIds.Distinct().ToList();
        if (uniqueGenreIds.Count != genreIds.Count)
        {
            throw new ValidationException("Duplicate genre ids are not allowed.");
        }

        var uniquePlatformIds = platformIds.Distinct().ToList();
        if (uniquePlatformIds.Count != platformIds.Count)
        {
            throw new ValidationException("Duplicate platform ids are not allowed.");
        }

        foreach (var genreId in uniqueGenreIds)
        {
            _ = await UnitOfWork.Genres.GetByIdAsync(genreId)
                ?? throw new ValidationException($"Genre with id '{genreId}' not found.");
        }

        foreach (var platformId in uniquePlatformIds)
        {
            _ = await UnitOfWork.Platforms.GetByIdAsync(platformId)
                ?? throw new ValidationException($"Platform with id '{platformId}' not found.");
        }

        return (uniqueGenreIds, uniquePlatformIds);
    }
}