using FluentValidation;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Validators.Library;

public class CreateExerciseRequestValidator : AbstractValidator<CreateExerciseRequest>
{
    private static readonly string[] AllowedVideoExtensions = [".mp4", ".webm", ".mov"];
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public CreateExerciseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Exercise name is required")
            .MaximumLength(200).WithMessage("Exercise name must not exceed 200 characters");

        RuleFor(x => x.BodyRegionCategoryId)
            .NotEqual(Guid.Empty).WithMessage("Body region category is required");

        RuleFor(x => x.VideoFileName)
            .Must(fileName => fileName == null || AllowedVideoExtensions
                .Contains(Path.GetExtension(fileName).ToLowerInvariant()))
            .WithMessage("Video must be a .mp4, .webm, or .mov file")
            .When(x => x.VideoStream != null);

        RuleFor(x => x.ThumbnailFileName)
            .Must(fileName => fileName == null || AllowedImageExtensions
                .Contains(Path.GetExtension(fileName).ToLowerInvariant()))
            .WithMessage("Thumbnail must be a .jpg, .jpeg, .png, or .webp file")
            .When(x => x.ThumbnailStream != null);
    }
}
