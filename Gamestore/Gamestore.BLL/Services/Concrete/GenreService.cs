using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;

namespace Gamestore.BLL.Services.Concrete;

public class GenreService(IUnitOfWork unitOfWork, IMapper mapper)
    : BaseService<GenreDto, Genre>(unitOfWork, mapper), IGenreService
{
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
        var existing = await UnitOfWork.Genres.GetSingleAsync(g => g.Name == dto.Genre.Name);
        if (existing != null)
        {
            throw new ValidationException($"Genre with name '{dto.Genre.Name}' already exists.");
        }

        var genre = new Genre
        {
            Id = Guid.NewGuid(),
            Name = dto.Genre.Name,
            ParentGenreId = dto.Genre.ParentGenreId,
        };

        await UnitOfWork.Genres.AddAsync(genre);
        await UnitOfWork.SaveChangesAsync();

        return Mapper.Map<GenreDto>(genre);
    }

    public async Task<GenreDto> UpdateAsync(UpdateGenreDto dto)
    {
        var genre = await UnitOfWork.Genres.GetByIdAsync(dto.Genre.Id)
            ?? throw new NotFoundException(nameof(Genre), dto.Genre.Id);

        genre.Name = dto.Genre.Name;
        genre.ParentGenreId = dto.Genre.ParentGenreId;

        UnitOfWork.Genres.Update(genre);
        await UnitOfWork.SaveChangesAsync();

        return Mapper.Map<GenreDto>(genre);
    }
}
