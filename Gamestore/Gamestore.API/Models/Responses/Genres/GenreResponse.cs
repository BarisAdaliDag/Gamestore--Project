namespace Gamestore.API.Models.Responses.Genres;

public class GenreResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? ParentGenreId { get; set; }
}
