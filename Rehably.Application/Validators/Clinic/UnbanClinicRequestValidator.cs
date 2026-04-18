using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class UnbanClinicRequestValidator : AbstractValidator<UnbanClinicRequest>
{
    public UnbanClinicRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Unban reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
