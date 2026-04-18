using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class ClinicImportRequestValidator : AbstractValidator<ClinicImportRequest>
{
    private const long MaxFileSizeBytes = 104_857_600; // 100 MB
    private static readonly string[] AllowedExtensions = [".zip", ".json"];

    public ClinicImportRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Import file is required");

        When(x => x.File != null, () =>
        {
            RuleFor(x => x.File!.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage("File size must not exceed 100 MB");

            RuleFor(x => x.File!.FileName)
                .Must(HaveAllowedExtension)
                .WithMessage("Only .zip and .json files are accepted");
        });
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}
