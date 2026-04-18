using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class ApproveClinicRequestValidator : AbstractValidator<ApproveClinicRequest>
{
    public ApproveClinicRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
    }
}
