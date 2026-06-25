using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Gamestore.BLL.Services.Concrete;

public class GenreService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GenreService> logger)
    : BaseService<GenreDto, Genre>(unitOfWork, mapper), IGenreService
{
    private readonly ILogger<GenreService> _logger = logger;

    protected override IRepository<Genre> Repository => UnitOfWork.Genres;

    public async Task<IEnumerable<GenreDto>> GetByParentIdAsync(Guid parentId)
    {
        var genres = await UnitOfWork.Genres.GetByParentIdAsync(parentId);
        return Mapper.Map<IEnumerable<GenreDto>>(genres);
    }

    public async Task<IEnumerable<GenreDto>> GetByGameKeyAsync(string gameKey)
    {
        var genres = await UnitOfWork.Genres.GetByGameKeyAsync(gameKey);
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

    public async Task<GenreDto> UpdateAsync(UpdateGenreDto dto)
    {
        _logger.LogInformation("Updating genre: {Id}", dto.Genre.Id);

        var genre = await UnitOfWork.Genres.GetByIdAsync(dto.Genre.Id)
            ?? throw new NotFoundException(nameof(Genre), dto.Genre.Id);

        var existing = await UnitOfWork.Genres.GetSingleAsync(
            g => g.Name == dto.Genre.Name && g.Id != dto.Genre.Id);
        if (existing != null)
        {
            throw new EntityAlreadyExistsException(nameof(Genre), "name", dto.Genre.Name);
        }

        if (dto.Genre.ParentGenreId == dto.Genre.Id)
        {
            throw new ValidationException("A genre cannot be its own parent.");
        }

        await ValidateParentGenreAsync(dto.Genre.ParentGenreId);

        genre.Name = dto.Genre.Name;
        genre.ParentGenreId = dto.Genre.ParentGenreId;

        UnitOfWork.Genres.Update(genre);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Genre updated: {Name} ({Id})", genre.Name, genre.Id);

        return Mapper.Map<GenreDto>(genre);
    }

    private async Task ValidateParentGenreAsync(Guid? parentGenreId)
    {
        if (parentGenreId == null)
        {
            return;
        }

        _ = await UnitOfWork.Genres.GetByIdAsync(parentGenreId.Value)
            ?? throw new ValidationException($"Parent genre with id '{parentGenreId}' not found.");
    }
}
