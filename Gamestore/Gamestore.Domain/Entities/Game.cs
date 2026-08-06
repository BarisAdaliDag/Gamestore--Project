namespace Gamestore.Domain.Entities;

public class Game
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Key { get; set; }

    public string? Description { get; set; }

    public double Price { get; set; }

    public int UnitInStock { get; set; }

    public int Discount { get; set; }

    public Guid PublisherId { get; set; }

    public Publisher? Publisher { get; set; }

    public ICollection<GameGenre> GameGenres { get; set; } = [];

    public ICollection<GamePlatform> GamePlatforms { get; set; } = [];

    public ICollection<OrderGame> OrderGames { get; set; } = [];

    public ICollection<Comment> Comments { get; set; } = [];
}
