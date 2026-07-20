namespace Gamestore.API.Models.Requests.Publishers;

public class PublisherUpdateInfoRequest
{
    public Guid Id { get; set; }

    public required string CompanyName { get; set; }

    public string? HomePage { get; set; }

    public string? Description { get; set; }
}
