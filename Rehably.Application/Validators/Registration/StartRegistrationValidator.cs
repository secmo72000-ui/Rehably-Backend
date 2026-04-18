using FluentValidation;
using Rehably.Application.DTOs.Registration;

namespace Rehably.Application.Validators.Registration;

public class StartRegistrationValidator : AbstractValidator<StartRegistrationRequestDto>
{
    public StartRegistrationValidator()
    {
        RuleFor(x => x.ClinicName)
            .NotEmpty().WithMessage("Clinic name is required")
            .MaximumLength(200).WithMessage("Clinic name cannot exceed 200 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("A valid email address is required");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required");

        RuleFor(x => x.OwnerFirstName)
            .NotEmpty().WithMessage("Owner first name is required");

        RuleFor(x => x.OwnerLastName)
            .NotEmpty().WithMessage("Owner last name is required");

        When(x => !string.IsNullOrEmpty(x.PreferredSlug), () =>
        {
            RuleFor(x => x.PreferredSlug!)
                .MinimumLength(3).WithMessage("Slug must be at least 3 characters")
                .MaximumLength(50).WithMessage("Slug must not exceed 50 characters")
                .Matches(@"^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$")
                    .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens, and must not start or end with a hyphen")
                .Must(slug => !slug.Contains("--"))
                    .WithMessage("Slug must not contain consecutive hyphens");
        });
    }
}
