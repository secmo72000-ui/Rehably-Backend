using FluentValidation;
using Rehably.Application.DTOs.Subscription;

namespace Rehably.Application.Validators.Platform;

public class UpgradeSubscriptionRequestDtoValidator : AbstractValidator<UpgradeSubscriptionRequestDto>
{
    public UpgradeSubscriptionRequestDtoValidator()
    {
        RuleFor(x => x.NewPackageId)
            .NotEqual(Guid.Empty).WithMessage("New package ID is required");
    }
}
