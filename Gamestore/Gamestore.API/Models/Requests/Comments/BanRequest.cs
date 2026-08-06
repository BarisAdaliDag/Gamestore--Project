namespace Gamestore.API.Models.Requests.Comments;

public class BanRequest
{
    public string User { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;
}