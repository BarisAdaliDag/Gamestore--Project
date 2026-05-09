namespace Gamestore.BLL.DTOs.Games;

public class GameUpdateInfoDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Key { get; set; }

    public string? Description { get; set; }
}
