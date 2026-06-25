using FluentValidation;
using Gamestore.API.Models.Requests.Platforms;

namespace Gamestore.API.Validators.Platforms;

public class CreatePlatformRequestValidator : AbstractValidator<CreatePlatformRequest>
{
    public CreatePlatformRequestValidator()
    {
        RuleFor(request => request.Platform)
            .NotNull()
            .WithMessage("Platform information is required.");

        When(request => request.Platform != null, () =>
        {
            RuleFor(request => request.Platform.Type)
                .NotEmpty()
                .WithMessage("Platform type is required.")
                .MaximumLength(100)
                .WithMessage("Platform type cannot exceed 100 characters.");
        });
    }
}
