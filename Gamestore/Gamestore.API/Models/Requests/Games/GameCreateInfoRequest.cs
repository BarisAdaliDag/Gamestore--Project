namespace Gamestore.API.Models.Requests.Games;

public class GameCreateInfoRequest
{
    public required string Name { get; set; }

    public string? Key { get; set; }

    public string? Description { get; set; }
}
