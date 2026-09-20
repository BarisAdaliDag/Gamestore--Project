namespace Gamestore.API.Models.Responses.Publishers;

public class PublisherResponse
{
    public Guid Id { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string? HomePage { get; set; }

    public string? Description { get; set; }
}
