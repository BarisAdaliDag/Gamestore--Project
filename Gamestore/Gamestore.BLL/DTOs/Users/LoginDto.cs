namespace Gamestore.BLL.DTOs.Users;

public class LoginDto
{
    public required string Login { get; set; }

    public required string Password { get; set; }

    public bool InternalAuth { get; set; }
}
