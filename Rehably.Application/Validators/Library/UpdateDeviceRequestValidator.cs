using FluentValidation;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Validators.Library;

public class UpdateDeviceRequestValidator : AbstractValidator<UpdateDeviceRequest>
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public UpdateDeviceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Device name is required")
            .MaximumLength(200).WithMessage("Device name must not exceed 200 characters");

        RuleFor(x => x.ImageFileName)
            .Must(fileName => fileName == null || AllowedImageExtensions
                .Contains(Path.GetExtension(fileName).ToLowerInvariant()))
            .WithMessage("Image must be a .jpg, .jpeg, .png, or .webp file")
            .When(x => x.ImageStream != null);
    }
}
