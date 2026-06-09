namespace Gamestore.BLL.DTOs.Genres;

public class GenreUpdateInfoDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public Guid? ParentGenreId { get; set; }
}
