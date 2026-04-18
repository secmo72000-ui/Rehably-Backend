using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class ClinicDocumentInputValidator : AbstractValidator<ClinicDocumentInput>
{
    private static readonly string[] AllowedContentTypes = {
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public ClinicDocumentInputValidator()
    {
        RuleFor(x => x.Base64Content)
            .NotEmpty().WithMessage("Document content is required")
            .Must(BeValidBase64).WithMessage("Document content must be valid base64 string")
            .Must(NotExceedMaxSize).WithMessage("Document size cannot exceed 10MB");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .MaximumLength(255).WithMessage("File name cannot exceed 255 characters");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required")
            .Must(BeAllowedContentType).WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}");
    }

    private static bool BeValidBase64(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return false;

        try
        {
            Convert.FromBase64String(base64String);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool NotExceedMaxSize(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return true;

        try
        {
            var bytes = Convert.FromBase64String(base64String);
            return bytes.Length <= MaxFileSizeBytes;
        }
        catch
        {
            return true; // Let BeValidBase64 handle invalid base64
        }
    }

    private static bool BeAllowedContentType(string contentType)
    {
        return AllowedContentTypes.Contains(contentType.ToLower());
    }
}
