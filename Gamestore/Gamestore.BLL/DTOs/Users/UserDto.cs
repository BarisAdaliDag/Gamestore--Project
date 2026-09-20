using Gamestore.BLL.DTOs.Abstract;

namespace Gamestore.BLL.DTOs.Users;

public class UserDto : IDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
