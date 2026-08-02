using FluentValidation;
using Gamestore.API.Models.Requests.Orders;
using Gamestore.Domain.Constants;

namespace Gamestore.API.Validators.Orders;

public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
{
    public PaymentRequestValidator()
    {
        RuleFor(x => x.Method)
            .NotEmpty()
            .WithMessage("Payment method is required.")
            .Must(method => PaymentMethods.All.Contains(method, StringComparer.Ordinal))
            .WithMessage("Payment method must be one of: Bank, IBox terminal, Visa.");

        When(x => x.Method == PaymentMethods.Visa, () =>
        {
            RuleFor(x => x.Model)
                .NotNull()
                .WithMessage("Visa payment details are required.");

            When(x => x.Model != null, () =>
            {
                RuleFor(x => x.Model!.Holder)
                    .NotEmpty()
                    .WithMessage("Card holder name is required.");

                RuleFor(x => x.Model!.CardNumber)
                    .NotEmpty()
                    .Length(13, 19)
                    .WithMessage("Card number must be between 13 and 19 digits.")
                    .Matches("^[0-9]+$")
                    .WithMessage("Card number must contain only digits.");

                RuleFor(x => x.Model!.MonthExpire)
                    .InclusiveBetween(1, 12)
                    .WithMessage("Expiry month must be between 1 and 12.");

                RuleFor(x => x.Model!.YearExpire)
                    .GreaterThanOrEqualTo(DateTime.UtcNow.Year)
                    .WithMessage("Card has expired.");

                RuleFor(x => x.Model!.Cvv2)
                    .InclusiveBetween(100, 9999)
                    .WithMessage("CVV2 must be 3 or 4 digits.");
            });
        });
    }
}
