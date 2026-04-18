using FluentValidation;
using Rehably.Application.DTOs.Subscription;

namespace Rehably.Application.Validators.Platform;

public class CancelSubscriptionRequestDtoValidator : AbstractValidator<CancelSubscriptionRequestDto>
{
    public CancelSubscriptionRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
