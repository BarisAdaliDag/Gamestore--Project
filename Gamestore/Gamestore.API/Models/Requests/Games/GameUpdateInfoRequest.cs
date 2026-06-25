namespace Gamestore.API.Models.Requests.Games;

public class GameUpdateInfoRequest
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Key { get; set; }

    public string? Description { get; set; }
}
