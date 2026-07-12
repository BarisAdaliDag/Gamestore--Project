namespace Gamestore.BLL.DTOs.Publishers;

public class PublisherCreateInfoDto
{
    public required string CompanyName { get; set; }

    public string? HomePage { get; set; }

    public string? Description { get; set; }
}
