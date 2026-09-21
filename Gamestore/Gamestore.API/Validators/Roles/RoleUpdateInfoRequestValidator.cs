using FluentValidation;
using Gamestore.API.Models.Requests.Roles;

namespace Gamestore.API.Validators.Roles;

public class RoleUpdateInfoRequestValidator : AbstractValidator<RoleUpdateInfoRequest>
{
    public RoleUpdateInfoRequestValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(50);
    }
}
