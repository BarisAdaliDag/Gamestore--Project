using Gamestore.BLL.DTOs.Abstract;

namespace Gamestore.BLL.DTOs.Genres;

public class GenreDto : IDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? ParentGenreId { get; set; }
}
