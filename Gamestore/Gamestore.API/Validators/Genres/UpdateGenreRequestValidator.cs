using FluentValidation;
using Gamestore.API.Models.Requests.Genres;

namespace Gamestore.API.Validators.Genres;

public class UpdateGenreRequestValidator : AbstractValidator<UpdateGenreRequest>
{
    public UpdateGenreRequestValidator()
    {
        RuleFor(request => request.Genre)
            .NotNull()
            .WithMessage("Genre information is required.");

        When(request => request.Genre != null, () =>
        {
            RuleFor(request => request.Genre.Id)
                .NotEmpty()
                .WithMessage("Genre id is required.");

            RuleFor(request => request.Genre.Name)
                .NotEmpty()
                .WithMessage("Genre name is required.")
                .MaximumLength(100)
                .WithMessage("Genre name cannot exceed 100 characters.");

            RuleFor(request => request.Genre.ParentGenreId)
                .NotEqual(Guid.Empty)
                .WithMessage("Parent genre id cannot be empty.")
                .When(request => request.Genre.ParentGenreId.HasValue);

            RuleFor(request => request.Genre)
                .Must(genre => genre.ParentGenreId != genre.Id)
                .WithMessage("A genre cannot be its own parent.")
                .When(request => request.Genre.ParentGenreId.HasValue);
        });
    }
}
