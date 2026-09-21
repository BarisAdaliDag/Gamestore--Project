using FluentValidation;
using Gamestore.API.Models.Requests.Roles;

namespace Gamestore.API.Validators.Roles;

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(request => request.Role)
            .NotNull()
            .SetValidator(new RoleUpdateInfoRequestValidator());
    }
}
