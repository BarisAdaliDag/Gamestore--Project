using Gamestore.Domain.Enums;

namespace Gamestore.BLL.DTOs.Comments;

public class CreateCommentDto
{
    public required CommentCreateInfoDto Comment { get; set; }

    public Guid? ParentId { get; set; }

    public CommentAction Action { get; set; } = CommentAction.None;
}