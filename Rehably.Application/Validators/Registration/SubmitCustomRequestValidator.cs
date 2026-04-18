using FluentValidation;
using Rehably.Application.DTOs.Registration;

namespace Rehably.Application.Validators.Registration;

public class SubmitCustomRequestValidator : AbstractValidator<SubmitCustomRequestDto>
{
    public SubmitCustomRequestValidator()
    {
        RuleFor(x => x.FeatureIds)
            .NotEmpty().WithMessage("At least one feature ID is required")
            .Must(ids => ids != null && ids.Count > 0).WithMessage("At least one feature ID is required");

        RuleFor(x => x.FeatureIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate feature IDs are not allowed");
    }
}
