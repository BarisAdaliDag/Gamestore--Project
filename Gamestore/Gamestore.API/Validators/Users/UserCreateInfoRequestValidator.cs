using FluentValidation;
using Gamestore.API.Models.Requests.Users;

namespace Gamestore.API.Validators.Users;

public class UserCreateInfoRequestValidator : AbstractValidator<UserCreateInfoRequest>
{
    public UserCreateInfoRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
