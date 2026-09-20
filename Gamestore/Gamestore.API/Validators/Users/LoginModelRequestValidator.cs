using FluentValidation;
using Gamestore.API.Models.Requests.Users;

namespace Gamestore.API.Validators.Users;

public class LoginModelRequestValidator : AbstractValidator<LoginModelRequest>
{
    public LoginModelRequestValidator()
    {
        RuleFor(request => request.Login)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
