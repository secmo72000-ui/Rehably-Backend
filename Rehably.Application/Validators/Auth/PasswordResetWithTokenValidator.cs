using FluentValidation;
using Rehably.Application.DTOs.Auth;

namespace Rehably.Application.Validators.Auth;

public class PasswordResetWithTokenValidator : AbstractValidator<PasswordResetWithTokenDto>
{
    public PasswordResetWithTokenValidator()
    {
        RuleFor(x => x.ResetToken)
            .NotEmpty().WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain a digit");
    }
}
