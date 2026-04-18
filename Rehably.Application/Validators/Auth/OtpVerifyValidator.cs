using FluentValidation;
using Rehably.Application.DTOs.Auth;

namespace Rehably.Application.Validators.Auth;

public class OtpVerifyValidator : AbstractValidator<OtpVerifyDto>
{
    private static readonly string[] ValidPurposes = { "login", "password_reset" };

    public OtpVerifyValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .Length(6).WithMessage("Code must be 6 digits")
            .Matches("^[0-9]+$").WithMessage("Code must contain only digits");

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("Purpose is required")
            .Must(p => ValidPurposes.Contains(p.ToLowerInvariant()))
            .WithMessage("Purpose must be 'login' or 'password_reset'");
    }
}
