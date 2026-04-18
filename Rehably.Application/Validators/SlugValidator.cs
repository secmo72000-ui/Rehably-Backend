using FluentValidation;

namespace Rehably.Application.Validators;

public class SlugValidator : AbstractValidator<string>
{
    private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "www", "app", "mail", "ftp", "test", "staging", "dev"
    };

    public SlugValidator()
    {
        RuleFor(slug => slug)
            .NotEmpty().WithMessage("Slug is required")
            .MinimumLength(3).WithMessage("Slug must be at least 3 characters")
            .MaximumLength(50).WithMessage("Slug must not exceed 50 characters")
            .Matches(@"^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens, and must not start or end with a hyphen")
            .Must(slug => !string.IsNullOrEmpty(slug) && !slug.Contains("--")).WithMessage("Slug must not contain consecutive hyphens")
            .Must(slug => !string.IsNullOrEmpty(slug) && !ReservedWords.Contains(slug)).WithMessage("Slug uses a reserved word");
    }
}
