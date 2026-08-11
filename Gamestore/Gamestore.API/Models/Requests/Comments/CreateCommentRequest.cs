using Gamestore.Domain.Enums;

namespace Gamestore.API.Models.Requests.Comments;

public class CreateCommentRequest
{
    public CommentCreateInfoRequest Comment { get; set; } = new();

    public Guid? ParentId { get; set; }

    public CommentAction? Action { get; set; }
}