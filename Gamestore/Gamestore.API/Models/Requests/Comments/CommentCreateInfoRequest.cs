namespace Gamestore.API.Models.Requests.Comments;

public class CommentCreateInfoRequest
{
    public string Name { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}