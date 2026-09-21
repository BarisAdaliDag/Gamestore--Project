using FluentValidation;
using Gamestore.API.Models.Requests.Roles;

namespace Gamestore.API.Validators.Roles;

public class RoleCreateInfoRequestValidator : AbstractValidator<RoleCreateInfoRequest>
{
    public RoleCreateInfoRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(50);
    }
}
