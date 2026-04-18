using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Clinic;

public record GetClinicsQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }  // Search in Name, NameArabic, City, Email
    public ClinicStatus? Status { get; init; }
    public SubscriptionStatus? SubscriptionStatus { get; init; }
    public Guid? PackageId { get; init; }
    public bool IncludeDeleted { get; init; } = false;
    public string? SortBy { get; init; } = "createdAt";
    public bool SortDesc { get; init; } = true;
}
