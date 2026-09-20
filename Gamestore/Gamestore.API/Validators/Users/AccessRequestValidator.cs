using FluentValidation;
using Gamestore.API.Models.Requests.Users;

namespace Gamestore.API.Validators.Users;

public class AccessRequestValidator : AbstractValidator<AccessRequest>
{
    public AccessRequestValidator()
    {
        RuleFor(request => request.TargetPage)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.TargetId)
            .MaximumLength(100);
    }
}
