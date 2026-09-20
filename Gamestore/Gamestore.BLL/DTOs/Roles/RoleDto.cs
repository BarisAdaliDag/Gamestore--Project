using Gamestore.BLL.DTOs.Abstract;

namespace Gamestore.BLL.DTOs.Roles;

public class RoleDto : IDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
