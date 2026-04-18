using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class RegisterClinicRequestValidator : AbstractValidator<RegisterClinicRequest>
{
    public RegisterClinicRequestValidator()
    {
        RuleFor(x => x.ClinicName)
            .NotEmpty().WithMessage("Clinic name is required")
            .MaximumLength(200).WithMessage("Clinic name cannot exceed 200 characters");

        RuleFor(x => x.ClinicNameArabic)
            .MaximumLength(200).WithMessage("Clinic Arabic name cannot exceed 200 characters")
            .Must(BeValidArabic).WithMessage("Must contain valid Arabic characters only");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("Phone number format is invalid");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email format is invalid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[^A-Za-z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("Phone number format is invalid")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address cannot exceed 500 characters");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");
    }

    private static bool BeValidArabic(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return true;

        return name.All(c => c >= 0x0600 && c <= 0x06FF || c == ' ');
    }
}
