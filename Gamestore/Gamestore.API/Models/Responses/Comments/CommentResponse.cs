namespace Gamestore.API.Models.Responses.Comments;

public class CommentResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public ICollection<CommentResponse> ChildComments { get; set; } = [];
}