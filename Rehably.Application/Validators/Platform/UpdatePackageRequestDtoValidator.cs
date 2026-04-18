using FluentValidation;
using Rehably.Application.DTOs.Package;
using Rehably.Domain.Enums;

namespace Rehably.Application.Validators.Platform;

public class UpdatePackageRequestDtoValidator : AbstractValidator<UpdatePackageRequestDto>
{
    public UpdatePackageRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Tier)
            .IsInEnum().WithMessage("Tier must be a valid PackageTier value");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.MonthlyPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Monthly price must be non-negative");

        RuleFor(x => x.YearlyPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Yearly price must be non-negative");

        RuleFor(x => x.TrialDays)
            .GreaterThanOrEqualTo(0).WithMessage("Trial days must be non-negative");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative");

        RuleFor(x => x.Features)
            .Must(features => features == null || features.Select(f => f.FeatureId).Distinct().Count() == features.Count)
            .WithMessage("Duplicate feature IDs are not allowed")
            .When(x => x.Features != null);

        RuleForEach(x => x.Features)
            .ChildRules(feature =>
            {
                feature.RuleFor(f => f.Limit)
                    .GreaterThanOrEqualTo(0).WithMessage("Feature limit must be non-negative")
                    .When(f => f.Limit.HasValue);
            })
            .When(x => x.Features != null);
    }
}
