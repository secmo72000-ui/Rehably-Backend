using FluentValidation;
using Rehably.Application.DTOs.Registration;
using Rehably.Domain.Enums;

namespace Rehably.Application.Validators.Registration;

public class SubmitGlobalPackageValidator : AbstractValidator<SubmitGlobalPackageRequestDto>
{
    public SubmitGlobalPackageValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("Package ID is required");

        RuleFor(x => x.BillingCycle)
            .IsInEnum().WithMessage("Billing cycle must be a valid value (Monthly or Yearly)");
    }
}
