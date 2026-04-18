using FluentValidation;
using Rehably.Application.DTOs.Payment;

namespace Rehably.Application.Validators.Payment;

public class CreatePaymentRequestDtoValidator : AbstractValidator<CreatePaymentRequestDto>
{
    public CreatePaymentRequestDtoValidator()
    {
        RuleFor(x => x.ClinicId)
            .NotEqual(Guid.Empty).WithMessage("ClinicId is required");

        RuleFor(x => x.SubscriptionPlanId)
            .NotEmpty().WithMessage("SubscriptionPlanId is required");

        RuleFor(x => x.ReturnUrl)
            .NotEmpty().WithMessage("ReturnUrl is required")
            .Must(BeAValidUrl).WithMessage("ReturnUrl must be a valid URL");

        RuleFor(x => x.CancelUrl)
            .NotEmpty().WithMessage("CancelUrl is required")
            .Must(BeAValidUrl).WithMessage("CancelUrl must be a valid URL");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
