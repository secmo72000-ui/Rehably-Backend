using FluentValidation;
using Rehably.Application.DTOs.Package;
using Rehably.Domain.Enums;

namespace Rehably.Application.Validators.Platform;

public class CreatePackageRequestDtoValidator : AbstractValidator<CreatePackageRequestDto>
{
    public CreatePackageRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Tier)
            .IsInEnum().WithMessage("Tier must be a valid PackageTier value");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Code must contain only lowercase letters, numbers, and hyphens")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));

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
            .NotEmpty().WithMessage("At least one feature must be specified")
            .Must(features => features == null || features.Select(f => f.FeatureId).Distinct().Count() == features.Count)
            .WithMessage("Duplicate feature IDs are not allowed");

        RuleForEach(x => x.Features)
            .ChildRules(feature =>
            {
                feature.RuleFor(f => f.Limit)
                    .GreaterThanOrEqualTo(0).WithMessage("Feature limit must be non-negative")
                    .When(f => f.Limit.HasValue);
            });
    }
}
