using FluentValidation;
using Gamestore.API.Models.Requests.Comments;

namespace Gamestore.API.Validators.Comments;

public class CommentCreateInfoRequestValidator : AbstractValidator<CommentCreateInfoRequest>
{
    public CommentCreateInfoRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.")
            .MaximumLength(1000).WithMessage("Comment body must not exceed 1000 characters.");
    }
}
