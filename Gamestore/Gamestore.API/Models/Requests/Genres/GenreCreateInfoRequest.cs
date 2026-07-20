namespace Gamestore.API.Models.Requests.Genres;

public class GenreCreateInfoRequest
{
    public required string Name { get; set; }

    public Guid? ParentGenreId { get; set; }
}
