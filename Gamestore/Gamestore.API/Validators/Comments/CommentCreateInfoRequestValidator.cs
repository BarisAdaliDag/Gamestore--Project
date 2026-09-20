using FluentValidation;
using Gamestore.API.Models.Requests.Comments;

namespace Gamestore.API.Validators.Comments;

public class CommentCreateInfoRequestValidator : AbstractValidator<CommentCreateInfoRequest>
{
    public CommentCreateInfoRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Comment author name is required.")
            .MaximumLength(100).WithMessage("Comment author name must not exceed 100 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.")
            .MaximumLength(1000).WithMessage("Comment body must not exceed 1000 characters.");
    }
}