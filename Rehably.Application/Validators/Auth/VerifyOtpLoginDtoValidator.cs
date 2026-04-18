using FluentValidation;
using Rehably.Application.DTOs.Auth;

namespace Rehably.Application.Validators.Auth;

public class VerifyOtpLoginDtoValidator : AbstractValidator<VerifyOtpLoginDto>
{
    public VerifyOtpLoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email format is invalid");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required")
            .Length(4, 6).WithMessage("OTP code must be between 4 and 6 digits");
    }
}
