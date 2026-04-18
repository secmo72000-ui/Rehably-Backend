using FluentValidation;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Validators.Library;

public class UpdateTreatmentStageRequestValidator : AbstractValidator<UpdateTreatmentStageRequest>
{
    public UpdateTreatmentStageRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.NameArabic)
            .MaximumLength(200).WithMessage("Arabic name must not exceed 200 characters")
            .When(x => x.NameArabic != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x)
            .Must(x => !x.MinWeeks.HasValue || !x.MaxWeeks.HasValue || x.MinWeeks <= x.MaxWeeks)
            .WithMessage("MinWeeks must be less than or equal to MaxWeeks")
            .When(x => x.MinWeeks.HasValue && x.MaxWeeks.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinSessions.HasValue || !x.MaxSessions.HasValue || x.MinSessions <= x.MaxSessions)
            .WithMessage("MinSessions must be less than or equal to MaxSessions")
            .When(x => x.MinSessions.HasValue && x.MaxSessions.HasValue);
    }
}
