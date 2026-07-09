namespace Gamestore.API.Models.Requests.Genres;

public class GenreUpdateInfoRequest
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public Guid? ParentGenreId { get; set; }
}
