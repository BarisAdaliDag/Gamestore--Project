namespace Gamestore.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string PasswordHash { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
