using Gamestore.BLL.DTOs.Genres;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Services.Abstract;

public interface IGenreService : IService<GenreDto, Genre>
{
    Task<IEnumerable<GenreDto>> GetByParentIdAsync(Guid parentId);

    Task<IEnumerable<GenreDto>> GetByGameKeyAsync(string gameKey);

    Task<GenreDto> CreateAsync(CreateGenreDto dto);

    Task<GenreDto> UpdateAsync(UpdateGenreDto dto);
}
