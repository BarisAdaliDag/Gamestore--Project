using FluentValidation;
using Gamestore.API.Models.Requests.Genres;

namespace Gamestore.API.Validators.Genres;

public class CreateGenreRequestValidator : AbstractValidator<CreateGenreRequest>
{
    public CreateGenreRequestValidator()
    {
        RuleFor(request => request.Genre)
            .NotNull()
            .WithMessage("Genre information is required.");

        When(request => request.Genre != null, () =>
        {
            RuleFor(request => request.Genre.Name)
                .NotEmpty()
                .WithMessage("Genre name is required.")
                .MaximumLength(100)
                .WithMessage("Genre name cannot exceed 100 characters.");

            RuleFor(request => request.Genre.ParentGenreId)
                .NotEqual(Guid.Empty)
                .WithMessage("Parent genre id cannot be empty.")
                .When(request => request.Genre.ParentGenreId.HasValue);
        });
    }
}
