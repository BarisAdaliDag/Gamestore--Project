using FluentValidation;
using Gamestore.API.Models.Requests.Users;

namespace Gamestore.API.Validators.Users;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.User)
            .NotNull()
            .SetValidator(new UserUpdateInfoRequestValidator());

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
