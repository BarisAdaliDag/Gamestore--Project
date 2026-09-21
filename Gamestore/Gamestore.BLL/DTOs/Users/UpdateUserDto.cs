namespace Gamestore.BLL.DTOs.Users;

public class UpdateUserDto
{
    public required UserUpdateInfoDto User { get; set; }

    public required ICollection<Guid> Roles { get; set; }

    public required string Password { get; set; }
}
