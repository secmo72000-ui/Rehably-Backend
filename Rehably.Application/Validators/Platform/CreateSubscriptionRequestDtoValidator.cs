using FluentValidation;
using Rehably.Application.DTOs.Subscription;
using Rehably.Domain.Enums;

namespace Rehably.Application.Validators.Platform;

public class CreateSubscriptionRequestDtoValidator : AbstractValidator<CreateSubscriptionRequestDto>
{
    public CreateSubscriptionRequestDtoValidator()
    {
        RuleFor(x => x.PaymentType)
            .IsInEnum().WithMessage("Payment type must be Cash, Online, or Free");

        RuleFor(x => x.ClinicId)
            .NotEqual(Guid.Empty).WithMessage("Clinic ID is required");

        RuleFor(x => x.PackageId)
            .NotEqual(Guid.Empty).WithMessage("Package ID is required");

        RuleFor(x => x.PaymentProvider)
            .NotEmpty().When(x => x.PaymentProvider != null)
            .WithMessage("Payment provider must be valid")
            .Must(x => x == null || new[] { "PayMob", "Stripe", "PayPal" }.Contains(x))
            .WithMessage("Payment provider must be one of: PayMob, Stripe, PayPal");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters")
            .Matches("^[A-Z0-9-]+$").WithMessage("Coupon code must contain only uppercase letters, numbers, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.CouponCode));
    }
}
