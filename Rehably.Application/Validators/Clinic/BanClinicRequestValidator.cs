using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class BanClinicRequestValidator : AbstractValidator<BanClinicRequest>
{
    public BanClinicRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Ban reason is required")
            .MaximumLength(500).WithMessage("Ban reason cannot exceed 500 characters");
    }
}
