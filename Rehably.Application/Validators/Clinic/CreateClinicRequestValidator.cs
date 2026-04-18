using FluentValidation;
using Rehably.Application.DTOs.Clinic;
using Rehably.Domain.Enums;

namespace Rehably.Application.Validators.Clinic;

public class CreateClinicRequestValidator : AbstractValidator<CreateClinicRequest>
{
    public CreateClinicRequestValidator()
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

        RuleFor(x => x.Slug)
            .SetValidator(new SlugValidator()!)
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.Governorate)
            .MaximumLength(100).WithMessage("Governorate cannot exceed 100 characters");

        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("Package ID is required")
            .NotEqual(Guid.Empty).WithMessage("Package ID must be valid");

        RuleFor(x => x.PaymentType)
            .IsInEnum().WithMessage("Payment type must be Cash (0), Online (1), or Free (2)");

        RuleFor(x => x.PaymentReference)
            .NotEmpty().WithMessage("Payment reference is required for online payments")
            .MaximumLength(200).WithMessage("Payment reference cannot exceed 200 characters")
            .When(x => x.PaymentType == PaymentType.Online);

        RuleFor(x => x.CustomTrialDays)
            .InclusiveBetween(0, 90).WithMessage("Custom trial days must be between 0 and 90")
            .When(x => x.CustomTrialDays.HasValue);

        // Only validate dates when BOTH are explicitly provided
        RuleFor(x => x.SubscriptionStartDate)
            .LessThan(x => x.SubscriptionEndDate).WithMessage("Subscription start date must be before end date")
            .When(x => x.SubscriptionStartDate.HasValue && x.SubscriptionEndDate.HasValue);

        RuleFor(x => x.SubscriptionEndDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Subscription end date must be in the future")
            .When(x => x.SubscriptionEndDate.HasValue);

        RuleFor(x => x.OwnerIdDocument)
            .Must(BeValidFile).WithMessage("Owner ID document must be a valid image or PDF (max 5MB)")
            .When(x => x.OwnerIdDocument != null);

        RuleFor(x => x.MedicalLicenseDocument)
            .Must(BeValidFile).WithMessage("Medical license document must be a valid image or PDF (max 5MB)")
            .When(x => x.MedicalLicenseDocument != null);
    }

    private static bool BeValidArabic(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return true;

        return name.All(c => c >= 0x0600 && c <= 0x06FF || c == ' ');
    }

    private static readonly string[] AllowedContentTypes =
    {
        "application/pdf", "image/jpeg", "image/jpg", "image/png",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private static bool BeValidFile(Microsoft.AspNetCore.Http.IFormFile? file)
    {
        if (file == null) return true;
        if (file.Length > 5 * 1024 * 1024) return false; // 5MB max
        return AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant());
    }
}
