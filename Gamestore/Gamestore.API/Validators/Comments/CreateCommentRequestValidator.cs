using FluentValidation;
using Gamestore.API.Models.Requests.Comments;
using Gamestore.Domain.Enums;

namespace Gamestore.API.Validators.Comments;

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Comment)
            .NotNull().WithMessage("Comment is required.")
            .SetValidator(new CommentCreateInfoRequestValidator());

        // Cross-field rule: Reply/Quote are meaningless without a parent.
        // The same rule exists in CommentService (each layer validates at its own boundary).
        RuleFor(x => x.ParentId)
            .NotNull()
            .WithMessage("ParentId is required when action is 'Reply' or 'Quote'.")
            .When(x => x.Action is not null and not CommentAction.None);

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Action must be one of: None, Reply, Quote.")
            .When(x => x.Action is not null);
    }
}