namespace Gamestore.BLL.DTOs.Comments;

public class CommentDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public ICollection<CommentDto> ChildComments { get; set; } = [];
}