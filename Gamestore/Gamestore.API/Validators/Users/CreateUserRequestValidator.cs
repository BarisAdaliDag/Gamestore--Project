using FluentValidation;
using Gamestore.API.Models.Requests.Users;

namespace Gamestore.API.Validators.Users;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.User)
            .NotNull()
            .SetValidator(new UserCreateInfoRequestValidator());

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(200);

        RuleFor(request => request.Roles)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(roles => roles.Distinct().Count() == roles.Count)
            .WithMessage("Roles cannot contain duplicate values.");
    }
}
