using FluentValidation;
using Gamestore.API.Models.Requests.Games;

namespace Gamestore.API.Validators.Games;

public class UpdateGameRequestValidator : AbstractValidator<UpdateGameRequest>
{
    public UpdateGameRequestValidator()
    {
        RuleFor(request => request.Game)
            .NotNull()
            .WithMessage("Game information is required.");

        When(request => request.Game != null, () =>
        {
            RuleFor(request => request.Game.Id)
                .NotEmpty()
                .WithMessage("Game id is required.");

            RuleFor(request => request.Game.Name)
                .NotEmpty()
                .WithMessage("Game name is required.")
                .MaximumLength(200)
                .WithMessage("Game name cannot exceed 200 characters.");

            RuleFor(request => request.Game.Key)
                .NotEmpty()
                .WithMessage("Game key is required.")
                .MaximumLength(100)
                .WithMessage("Game key cannot exceed 100 characters.");

            RuleFor(request => request.Game.Description)
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters.");
        });

        RuleFor(request => request.Genres)
            .NotNull()
            .WithMessage("Genres list is required.");

        RuleFor(request => request.Platforms)
            .NotNull()
            .WithMessage("Platforms list is required.");
    }
}
