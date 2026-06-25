namespace Gamestore.API.Models.Requests.Games;

public class UpdateGameRequest
{
    public required GameUpdateInfoRequest Game { get; set; }

    public required ICollection<Guid> Genres { get; set; }

    public required ICollection<Guid> Platforms { get; set; }
}
