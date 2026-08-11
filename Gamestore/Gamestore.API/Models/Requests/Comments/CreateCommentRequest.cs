namespace Gamestore.API.Models.Requests.Comments;

public class CreateCommentRequest
{
    public CommentCreateInfoRequest Comment { get; set; } = new();

    public string? ParentId { get; set; }

    public string? Action { get; set; }
}
