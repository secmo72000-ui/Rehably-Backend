using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequest>
{
    public UpdateSubscriptionRequestValidator()
    {
        RuleFor(x => x.NewPackageId)
            .NotEmpty().WithMessage("New package ID is required")
            .NotEqual(Guid.Empty).WithMessage("Package ID must be valid");
    }
}
