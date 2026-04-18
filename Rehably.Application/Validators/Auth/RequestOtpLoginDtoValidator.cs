using FluentValidation;
using Rehably.Application.DTOs.Auth;

namespace Rehably.Application.Validators.Auth;

public class RequestOtpLoginDtoValidator : AbstractValidator<RequestOtpLoginDto>
{
    public RequestOtpLoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email format is invalid");
    }
}
