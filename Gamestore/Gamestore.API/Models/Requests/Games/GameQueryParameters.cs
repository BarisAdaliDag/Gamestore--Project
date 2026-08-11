namespace Gamestore.API.Models.Requests.Games;

public class GameQueryParameters : QueryParameters
{
    public string? Name { get; set; }

    public Guid? GenreId { get; set; }

    public Guid? PlatformId { get; set; }
}
