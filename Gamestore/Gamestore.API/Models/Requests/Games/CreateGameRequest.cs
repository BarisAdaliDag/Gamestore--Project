namespace Gamestore.API.Models.Requests.Games;

public class CreateGameRequest
{
    public required GameCreateInfoRequest Game { get; set; }

    public required ICollection<Guid> Genres { get; set; }

    public required ICollection<Guid> Platforms { get; set; }
}
