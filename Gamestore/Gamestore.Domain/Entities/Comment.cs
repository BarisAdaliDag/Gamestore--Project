using Gamestore.Domain.Enums;

namespace Gamestore.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Body { get; set; }

    public bool IsDeleted { get; set; }

    public CommentAction Action { get; set; } = CommentAction.None;

    public DateTime CreatedAt { get; set; }

    public Guid GameId { get; set; }

    public Game? Game { get; set; }

    public Guid? ParentCommentId { get; set; }

    public Comment? ParentComment { get; set; }

    public ICollection<Comment> ChildComments { get; set; } = [];
}