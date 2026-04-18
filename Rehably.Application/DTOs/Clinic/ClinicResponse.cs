using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Clinic;

public record ClinicSubscriptionFeatureDto
{
    public Guid FeatureId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsIncluded { get; init; }
    public int? Limit { get; init; }
}

public record ClinicResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? NameArabic { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? Description { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }

    public ClinicStatus Status { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAt { get; init; }
    public bool IsBanned { get; init; }
    public string? BanReason { get; init; }
    public DateTime? BannedAt { get; init; }
    public string? BannedBy { get; init; }
    public Guid? SubscriptionPlanId { get; init; }
    public string? SubscriptionPlanName { get; init; }
    public SubscriptionStatus SubscriptionStatus { get; init; }
    public DateTime SubscriptionStartDate { get; init; }
    public DateTime? SubscriptionEndDate { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public long StorageUsedBytes { get; init; }
    public long StorageLimitBytes { get; init; }
    public int PatientsCount { get; init; }
    public int? PatientsLimit { get; init; }
    public int UsersCount { get; init; }
    public int? UsersLimit { get; init; }
    public decimal StorageUsedPercentage => StorageLimitBytes > 0 ? (decimal)StorageUsedBytes / StorageLimitBytes * 100 : 0;
    public decimal PatientsUsedPercentage => PatientsLimit.GetValueOrDefault() > 0 ? (decimal)PatientsCount / PatientsLimit!.Value * 100 : 0;
    public decimal UsersUsedPercentage => UsersLimit.GetValueOrDefault() > 0 ? (decimal)UsersCount / UsersLimit!.Value * 100 : 0;
    public string? OwnerFirstName { get; init; }
    public string? OwnerLastName { get; init; }
    public string? OwnerEmail { get; init; }
    public string? PaymentMethod { get; init; }
    public List<ClinicSubscriptionFeatureDto> PackageFeatures { get; init; } = [];
    public List<ClinicDocumentDto> Documents { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
