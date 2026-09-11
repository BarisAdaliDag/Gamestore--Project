using FluentValidation;
using Gamestore.API.Models.Requests.Games;

namespace Gamestore.API.Validators.Games;

public class CreateGameRequestValidator : AbstractValidator<CreateGameRequest>
{
    public CreateGameRequestValidator()
    {
        RuleFor(request => request.Game)
            .NotNull()
            .WithMessage("Game information is required.");

        When(request => request.Game != null, () =>
        {
            RuleFor(request => request.Game.Name)
                .NotEmpty()
                .WithMessage("Game name is required.")
                .MaximumLength(200)
                .WithMessage("Game name cannot exceed 200 characters.");

            RuleFor(request => request.Game.Key)
                .MaximumLength(100)
                .WithMessage("Game key cannot exceed 100 characters.")
                .Must(key => string.IsNullOrWhiteSpace(key)
                    || !key.StartsWith("mongo-", StringComparison.OrdinalIgnoreCase))
                .WithMessage("The 'mongo-' key prefix is reserved for Northwind products.");

            RuleFor(request => request.Game.Description)
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters.");

            RuleFor(request => request.Game.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Price cannot be negative.");

            RuleFor(request => request.Game.UnitInStock)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Unit in stock cannot be negative.");

            RuleFor(request => request.Game.Discount)
                .InclusiveBetween(0, 100)
                .WithMessage("Discount must be between 0 and 100.");
        });

        RuleFor(request => request.Genres)
            .NotEmpty()
            .WithMessage("At least one genre is required.");

        RuleFor(request => request.Platforms)
            .NotEmpty()
            .WithMessage("At least one platform is required.");

        RuleFor(request => request.Publisher)
            .NotEmpty()
            .WithMessage("Publisher id is required.");
    }
}
