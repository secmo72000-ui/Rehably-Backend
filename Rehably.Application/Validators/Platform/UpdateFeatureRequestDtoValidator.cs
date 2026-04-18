using FluentValidation;
using Rehably.Application.DTOs.Feature;

namespace Rehably.Application.Validators.Platform;

public class UpdateFeatureRequestDtoValidator : AbstractValidator<UpdateFeatureRequestDto>
{
    public UpdateFeatureRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative");
    }
}
