using FluentValidation;
using Gamestore.API.Models.Requests.Roles;

namespace Gamestore.API.Validators.Roles;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(request => request.Role)
            .NotNull()
            .SetValidator(new RoleCreateInfoRequestValidator());
    }
}
