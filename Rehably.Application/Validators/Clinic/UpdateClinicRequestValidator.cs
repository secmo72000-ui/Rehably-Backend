using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class UpdateClinicRequestValidator : AbstractValidator<UpdateClinicRequest>
{
    public UpdateClinicRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required")
            .When(x => x.Name != null)
            .MaximumLength(200).WithMessage("Clinic name cannot exceed 200 characters");

        RuleFor(x => x.NameArabic)
            .MaximumLength(200).WithMessage("Clinic Arabic name cannot exceed 200 characters")
            .Must(BeValidArabic).WithMessage("Must contain valid Arabic characters only");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .When(x => x.Phone != null)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("Phone number format is invalid");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email format is invalid")
            .When(x => x.Email != null);
    }

    private static bool BeValidArabic(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return true;

        return name.All(c => c >= 0x0600 && c <= 0x06FF || c == ' ');
    }
}
