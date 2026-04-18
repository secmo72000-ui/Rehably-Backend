using FluentValidation;
using Rehably.Application.DTOs.Feature;

namespace Rehably.Application.Validators.Platform;

public class CreateFeatureCategoryRequestDtoValidator : AbstractValidator<CreateFeatureCategoryRequestDto>
{
    public CreateFeatureCategoryRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(100).WithMessage("Code cannot exceed 100 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Code must contain only lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.Icon)
            .MaximumLength(100).WithMessage("Icon cannot exceed 100 characters");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative");

        RuleFor(x => x.ParentCategoryId)
            .NotEqual(Guid.Empty).When(x => x.ParentCategoryId.HasValue).WithMessage("Parent category ID must be valid");
    }
}
