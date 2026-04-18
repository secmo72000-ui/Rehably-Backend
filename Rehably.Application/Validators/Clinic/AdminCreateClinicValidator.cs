using FluentValidation;
using Rehably.Application.DTOs.Clinic;
using Rehably.Domain.Enums;

namespace Rehably.Application.Validators.Clinic;

public class AdminCreateClinicValidator : AbstractValidator<AdminCreateClinicRequestDto>
{
    public AdminCreateClinicValidator()
    {
        RuleFor(x => x.ClinicName)
            .NotEmpty().WithMessage("Clinic name is required")
            .MaximumLength(200).WithMessage("Clinic name cannot exceed 200 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MinimumLength(3).WithMessage("Slug must be at least 3 characters")
            .MaximumLength(50).WithMessage("Slug must not exceed 50 characters")
            .Matches(@"^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens, and must not start or end with a hyphen");

        RuleFor(x => x.OwnerEmail)
            .NotEmpty().WithMessage("Owner email is required")
            .EmailAddress().WithMessage("Owner email must be a valid email address");

        RuleFor(x => x.OwnerFirstName)
            .NotEmpty().WithMessage("Owner first name is required");

        RuleFor(x => x.OwnerLastName)
            .NotEmpty().WithMessage("Owner last name is required");

        RuleFor(x => x.BillingCycle)
            .IsInEnum().WithMessage("Billing cycle must be a valid value");

        RuleFor(x => x.PaymentType)
            .IsInEnum().WithMessage("Payment type must be a valid value");

        When(x => x.PackageId == null, () =>
        {
            RuleFor(x => x.CustomFeatures)
                .NotNull().WithMessage("Custom features are required when no package is specified")
                .Must(f => f != null && f.Count >= 1)
                .WithMessage("At least one custom feature is required when no package is specified");

            RuleFor(x => x.CustomMonthlyPrice)
                .NotNull().WithMessage("Custom monthly price is required when no package is specified")
                .GreaterThanOrEqualTo(0).WithMessage("Custom monthly price must be non-negative");

            RuleFor(x => x.CustomYearlyPrice)
                .NotNull().WithMessage("Custom yearly price is required when no package is specified")
                .GreaterThanOrEqualTo(0).WithMessage("Custom yearly price must be non-negative");
        });
    }
}
