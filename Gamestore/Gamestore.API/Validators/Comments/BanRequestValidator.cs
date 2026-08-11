using FluentValidation;
using Gamestore.API.Models.Requests.Comments;
using Gamestore.BLL.Helpers;

namespace Gamestore.API.Validators.Comments;

public class BanRequestValidator : AbstractValidator<BanRequest>
{
    public BanRequestValidator()
    {
        RuleFor(x => x.User)
            .NotEmpty().WithMessage("User is required.")
            .MaximumLength(100).WithMessage("User must not exceed 100 characters.");

        // SINGLE SOURCE OF TRUTH: the list returned by US4 and the list accepted by US5
        // are read from the same place (BanDurationHelper) -> they will never diverge.
        RuleFor(x => x.Duration)
            .NotEmpty().WithMessage("Duration is required.")
            .Must(BanDurationHelper.IsSupported)
            .WithMessage(x =>
                $"Duration must be one of: {string.Join(", ", BanDurationHelper.GetDurations())}.");
    }
}