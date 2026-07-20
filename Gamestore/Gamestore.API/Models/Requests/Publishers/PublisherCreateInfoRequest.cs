namespace Gamestore.API.Models.Requests.Publishers;

public class PublisherCreateInfoRequest
{
    public required string CompanyName { get; set; }

    public string? HomePage { get; set; }

    public string? Description { get; set; }
}
