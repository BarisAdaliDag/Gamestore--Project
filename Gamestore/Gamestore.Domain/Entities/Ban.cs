namespace Gamestore.Domain.Entities;

public class Ban
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTime? BannedUntil { get; set; }
}