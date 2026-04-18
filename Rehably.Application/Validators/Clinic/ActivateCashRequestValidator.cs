using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class ActivateCashRequestValidator : AbstractValidator<ActivateCashRequest>
{
    public ActivateCashRequestValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("Package ID is required")
            .NotEqual(Guid.Empty).WithMessage("Package ID must be valid");
    }
}
