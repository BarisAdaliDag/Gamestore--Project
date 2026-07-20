namespace Gamestore.BLL.DTOs.Genres;

public class GenreCreateInfoDto
{
    public required string Name { get; set; }

    public Guid? ParentGenreId { get; set; }
}
