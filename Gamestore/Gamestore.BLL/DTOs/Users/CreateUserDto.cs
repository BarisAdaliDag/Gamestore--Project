namespace Gamestore.BLL.DTOs.Users;

public class CreateUserDto
{
    public required UserCreateInfoDto User { get; set; }

    public required ICollection<Guid> Roles { get; set; }

    public required string Password { get; set; }
}
