using FluentValidation;
using Gamestore.API.Models.Requests.Publishers;

namespace Gamestore.API.Validators.Publishers;

public class CreatePublisherRequestValidator : AbstractValidator<CreatePublisherRequest>
{
    public CreatePublisherRequestValidator()
    {
        RuleFor(request => request.Publisher)
            .NotNull()
            .WithMessage("Publisher information is required.");

        When(request => request.Publisher != null, () =>
        {
            RuleFor(request => request.Publisher.CompanyName)
                .NotEmpty()
                .WithMessage("Company name is required.")
                .MaximumLength(200)
                .WithMessage("Company name cannot exceed 200 characters.");

            RuleFor(request => request.Publisher.HomePage)
                .MaximumLength(500)
                .WithMessage("Home page cannot exceed 500 characters.");

            RuleFor(request => request.Publisher.Description)
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters.");
        });
    }
}
