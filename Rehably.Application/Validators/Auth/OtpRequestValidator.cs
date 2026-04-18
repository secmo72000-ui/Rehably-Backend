using FluentValidation;
using Rehably.Application.DTOs.Auth;

namespace Rehably.Application.Validators.Auth;

public class OtpRequestValidator : AbstractValidator<OtpRequestDto>
{
    private static readonly string[] ValidPurposes = { "login", "password_reset" };

    public OtpRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("Purpose is required")
            .Must(p => ValidPurposes.Contains(p.ToLowerInvariant()))
            .WithMessage("Purpose must be 'login' or 'password_reset'");
    }
}
