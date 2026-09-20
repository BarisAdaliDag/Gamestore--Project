using FluentValidation;
using Gamestore.API.Models.Requests.Users;

namespace Gamestore.API.Validators.Users;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Model)
            .NotNull()
            .SetValidator(new LoginModelRequestValidator());
    }
}
