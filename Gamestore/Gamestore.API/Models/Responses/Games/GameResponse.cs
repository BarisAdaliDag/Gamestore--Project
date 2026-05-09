namespace Gamestore.API.Models.Responses.Games;

public class GameResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }
}
