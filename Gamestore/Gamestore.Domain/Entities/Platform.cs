namespace Gamestore.Domain.Entities;

public class Platform
{
    public Guid Id { get; set; }

    public required string Type { get; set; }

    public ICollection<GamePlatform> GamePlatforms { get; set; } = [];
}