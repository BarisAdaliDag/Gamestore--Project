namespace Gamestore.BLL.DTOs.Games;

public class UpdateGameDto
{
    public required GameUpdateInfoDto Game { get; set; }

    public required ICollection<Guid> Genres { get; set; }

    public required ICollection<Guid> Platforms { get; set; }

    public Guid Publisher { get; set; }
}
