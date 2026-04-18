using FluentValidation;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Validators.Clinic;

public class GetClinicsQueryValidator : AbstractValidator<GetClinicsQuery>
{
    private static readonly string[] AllowedSortFields = { "name", "createdAt", "status" };

    public GetClinicsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Search query cannot exceed 200 characters");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || AllowedSortFields.Any(f => f.Equals(sortBy, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}");
    }
}
