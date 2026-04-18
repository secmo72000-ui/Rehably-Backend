using FluentValidation;
using Rehably.Application.DTOs.Feature;

namespace Rehably.Application.Validators.Platform;

public class CreateFeatureRequestDtoValidator : AbstractValidator<CreateFeatureRequestDto>
{
    public CreateFeatureRequestDtoValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEqual(Guid.Empty).WithMessage("Category ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(100).WithMessage("Code cannot exceed 100 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Code must contain only lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.PricingType)
            .IsInEnum().WithMessage("Pricing type must be a valid value");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative");
    }
}
