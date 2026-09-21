using FluentValidation;
using Gamestore.API.Models.Requests.Users;

namespace Gamestore.API.Validators.Users;

public class UserUpdateInfoRequestValidator : AbstractValidator<UserUpdateInfoRequest>
{
    public UserUpdateInfoRequestValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
