using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Helpers;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gamestore.BLL.Services.Concrete;

public class GenreService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<GenreService> logger,
    INorthwindCategoryRepository? categoryRepository = null,
    IGameMaterializationService? gameMaterializationService = null)
    : BaseService<GenreDto, Genre>(unitOfWork, mapper), IGenreService
{
    private readonly ILogger<GenreService> _logger = logger;
    private readonly INorthwindCategoryRepository? _categoryRepository = categoryRepository;
    private readonly IGameMaterializationService? _gameMaterializationService = gameMaterializationService;

    public GenreService(IUnitOfWork unitOfWork, IMapper mapper)
        : this(unitOfWork, mapper, NullLogger<GenreService>.Instance)
    {
    }

    protected override IRepository<Genre> Repository => UnitOfWork.Genres;

    public override async Task<GenreDto> GetByIdAsync(Guid id)
    {
        var genre = await UnitOfWork.Genres.GetByIdAsync(id)
            ?? await MaterializeNorthwindCategoryByIdAsync(id)
            ?? throw new NotFoundException(nameof(Genre), id);

        return Mapper.Map<GenreDto>(genre);
    }

    public async Task<IEnumerable<GenreDto>> GetByParentIdAsync(Guid parentId)
    {
        var genres = await UnitOfWork.Genres.GetByParentIdAsync(parentId);
        return Mapper.Map<IEnumerable<GenreDto>>(genres);
    }

    public async Task<IEnumerable<GenreDto>> GetByGameKeyAsync(string gameKey)
    {
        var genres = await UnitOfWork.Genres.GetByGameKeyAsync(gameKey);
        if (!genres.Any() && _gameMaterializationService != null)
        {
            await _gameMaterializationService.GetOrCreateAsync(gameKey);
            genres = await UnitOfWork.Genres.GetByGameKeyAsync(gameKey);
        }

        return Mapper.Map<IEnumerable<GenreDto>>(genres);
    }

    public async Task<GenreDto> CreateAsync(CreateGenreDto dto)
    {
        _logger.LogInformation("Creating genre: {Name}", dto.Genre.Name);

        var existing = await UnitOfWork.Genres.GetSingleAsync(g => g.Name == dto.Genre.Name);
        if (existing != null)
        {
            throw new EntityAlreadyExistsException(nameof(Genre), "name", dto.Genre.Name);
        }

        await ValidateParentGenreAsync(dto.Genre.ParentGenreId);

        var genre = new Genre
        {
            Id = Guid.NewGuid(),
            Name = dto.Genre.Name,
            ParentGenreId = dto.Genre.ParentGenreId,
        };

        await UnitOfWork.Genres.AddAsync(genre);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Genre created: {Name} ({Id})", genre.Name, genre.Id);

        return Mapper.Map<GenreDto>(genre);
    }

    public override async Task DeleteAsync(Guid id)
    {
        var genre = await UnitOfWork.Genres.GetByIdAsync(id)
            ?? await MaterializeNorthwindCategoryByIdAsync(id)
            ?? throw new NotFoundException(nameof(Genre), id);

        var hasSubGenres = (await UnitOfWork.Genres
            .FindAsync(g => g.ParentGenreId == id)).Any();
        if (hasSubGenres)
        {
            throw new EntityInUseException(nameof(Genre), id);
        }

        UnitOfWork.Genres.Delete(genre);
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task<GenreDto> UpdateAsync(UpdateGenreDto dto)
    {
        _logger.LogInformation("Updating genre: {Id}", dto.Genre.Id);

        var genre = await UnitOfWork.Genres.GetByIdAsync(dto.Genre.Id)
            ?? await MaterializeNorthwindCategoryByIdAsync(dto.Genre.Id)
            ?? throw new NotFoundException(nameof(Genre), dto.Genre.Id);

        var existing = await UnitOfWork.Genres.GetSingleAsync(
            g => g.Name == dto.Genre.Name && g.Id != dto.Genre.Id);
        if (existing != null)
        {
            throw new EntityAlreadyExistsException(nameof(Genre), "name", dto.Genre.Name);
        }

        await ValidateParentGenreAsync(dto.Genre.ParentGenreId, dto.Genre.Id);

        genre.Name = dto.Genre.Name;
        genre.ParentGenreId = dto.Genre.ParentGenreId;

        UnitOfWork.Genres.Update(genre);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Genre updated: {Name} ({Id})", genre.Name, genre.Id);

        return Mapper.Map<GenreDto>(genre);
    }

    private async Task ValidateParentGenreAsync(Guid? parentGenreId, Guid? genreId = null)
    {
        var visitedGenreIds = new HashSet<Guid>();
        var currentParentId = parentGenreId;

        while (currentParentId.HasValue)
        {
            if (currentParentId == genreId)
            {
                throw new ValidationException("A genre hierarchy cannot contain cycles.");
            }

            if (!visitedGenreIds.Add(currentParentId.Value))
            {
                throw new ValidationException("Parent genre hierarchy already contains a cycle.");
            }

            var parentGenre = await UnitOfWork.Genres.GetByIdAsync(currentParentId.Value)
                ?? throw new ValidationException($"Parent genre with id '{currentParentId}' not found.");

            currentParentId = parentGenre.ParentGenreId;
        }
    }

    private async Task<Genre?> MaterializeNorthwindCategoryByIdAsync(Guid id)
    {
        var category = _categoryRepository?
            .Query()
            .AsEnumerable()
            .Where(item => !string.IsNullOrWhiteSpace(item.CategoryName))
            .FirstOrDefault(item => NorthwindReferenceIdentity.CreateCategoryId(item.CategoryName!) == id);

        if (category == null)
        {
            return null;
        }

        var existing = await UnitOfWork.Genres.GetSingleAsync(
            genre => genre.Name == category.CategoryName);
        if (existing != null)
        {
            return existing;
        }

        var genre = new Genre
        {
            Id = NorthwindReferenceIdentity.CreateCategoryId(category.CategoryName!),
            Name = category.CategoryName!,
        };

        await UnitOfWork.Genres.AddAsync(genre);
        await UnitOfWork.SaveChangesAsync();
        return genre;
    }
}
