using FluentValidation;
using Rehably.Application.DTOs.Feature;

namespace Rehably.Application.Validators.Platform;

public class UpdateFeaturePriceRequestDtoValidator : AbstractValidator<UpdateFeaturePriceRequestDto>
{
    public UpdateFeaturePriceRequestDtoValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative");

        RuleFor(x => x.PerUnitPrice)
            .GreaterThanOrEqualTo(0).When(x => x.PerUnitPrice.HasValue).WithMessage("Per-unit price must be non-negative");
    }
}
