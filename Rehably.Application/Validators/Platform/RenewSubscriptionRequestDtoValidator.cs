using FluentValidation;
using Rehably.Application.DTOs.Subscription;

namespace Rehably.Application.Validators.Platform;

public class RenewSubscriptionRequestDtoValidator : AbstractValidator<RenewSubscriptionRequestDto>
{
    public RenewSubscriptionRequestDtoValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEqual(Guid.Empty).WithMessage("Package ID is required");
    }
}
