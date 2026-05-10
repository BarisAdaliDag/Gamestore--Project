using Gamestore.BLL.DTOs.Genres;

namespace Gamestore.BLL.Services.Abstract;

public interface IGenreService : IService<GenreDto>
{
    Task<IEnumerable<GenreDto>> GetByParentIdAsync(Guid parentId);

    Task<IEnumerable<GenreDto>> GetByGameKeyAsync(string gameKey);

    Task<GenreDto> CreateAsync(CreateGenreDto dto);

    Task<GenreDto> UpdateAsync(UpdateGenreDto dto);
}