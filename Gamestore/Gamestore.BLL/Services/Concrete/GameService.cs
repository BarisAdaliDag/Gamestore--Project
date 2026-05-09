using System.Text;
using System.Text.Json;
using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;

namespace Gamestore.BLL.Services.Concrete;

public class GameService(IUnitOfWork unitOfWork, IMapper mapper)
    : BaseService<GameDto, Game>(unitOfWork, mapper), IGameService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    protected override IRepository<Game> Repository => UnitOfWork.Games;

    public async Task<GameDto> GetByKeyAsync(string key)
    {
        var game = await UnitOfWork.Games.GetByKeyAsync(key)
            ?? throw new NotFoundException(nameof(Game), key);
        return Mapper.Map<GameDto>(game);
    }

    public async Task<IEnumerable<GameDto>> GetByGenreIdAsync(Guid genreId)
    {
        var games = await UnitOfWork.Games.GetByGenreIdAsync(genreId);
        return Mapper.Map<IEnumerable<GameDto>>(games);
    }

    public async Task<IEnumerable<GameDto>> GetByPlatformIdAsync(Guid platformId)
    {
        var games = await UnitOfWork.Games.GetByPlatformIdAsync(platformId);
        return Mapper.Map<IEnumerable<GameDto>>(games);
    }

    public async Task<GameDto> CreateAsync(CreateGameDto dto)
    {
        var key = string.IsNullOrWhiteSpace(dto.Game.Key)
            ? GenerateKey(dto.Game.Name)
            : dto.Game.Key;

        var existing = await UnitOfWork.Games.GetByKeyAsync(key);
        if (existing != null)
        {
            throw new ValidationException($"Game with key '{key}' already exists.");
        }

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = dto.Game.Name,
            Key = key,
            Description = dto.Game.Description,
        };

        foreach (var genreId in dto.Genres)
        {
            game.GameGenres.Add(new GameGenre { GameId = game.Id, GenreId = genreId });
        }

        foreach (var platformId in dto.Platforms)
        {
            game.GamePlatforms.Add(new GamePlatform { GameId = game.Id, PlatformId = platformId });
        }

        await UnitOfWork.Games.AddAsync(game);
        await UnitOfWork.SaveChangesAsync();

        return Mapper.Map<GameDto>(game);
    }

    public async Task<GameDto> UpdateAsync(UpdateGameDto dto)
    {
        var game = await UnitOfWork.Games.GetByIdWithDetailsAsync(dto.Game.Id)
            ?? throw new NotFoundException(nameof(Game), dto.Game.Id);

        game.Name = dto.Game.Name;
        game.Key = dto.Game.Key;
        game.Description = dto.Game.Description;

        game.GameGenres.Clear();
        foreach (var genreId in dto.Genres)
        {
            game.GameGenres.Add(new GameGenre { GameId = game.Id, GenreId = genreId });
        }

        game.GamePlatforms.Clear();
        foreach (var platformId in dto.Platforms)
        {
            game.GamePlatforms.Add(new GamePlatform { GameId = game.Id, PlatformId = platformId });
        }

        UnitOfWork.Games.Update(game);
        await UnitOfWork.SaveChangesAsync();

        return Mapper.Map<GameDto>(game);
    }

    public async Task DeleteByKeyAsync(string key)
    {
        var game = await UnitOfWork.Games.GetByKeyAsync(key)
            ?? throw new NotFoundException(nameof(Game), key);

        UnitOfWork.Games.Delete(game);
        await UnitOfWork.SaveChangesAsync();
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
}
