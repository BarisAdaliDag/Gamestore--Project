namespace Gamestore.Domain.Entities;

public class GamePlatform
{
    public Guid GameId { get; set; }

    public Game? Game { get; set; }

    public Guid PlatformId { get; set; }

    public Platform? Platform { get; set; }
}