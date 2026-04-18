using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Subscription;

public record UsageStatisticsResponse
{
    public Guid ClinicId { get; init; }
    public string ClinicName { get; init; } = string.Empty;

    public long StorageUsedBytes { get; init; }
    public long StorageLimitBytes { get; init; }
    public decimal StorageUsedPercentage { get; init; }
    public string StorageUsedFormatted { get; init; } = string.Empty;
    public string StorageLimitFormatted { get; init; } = string.Empty;
    public int PatientsCount { get; init; }
    public int? PatientsLimit { get; init; }
    public decimal PatientsUsedPercentage { get; init; }
    public int UsersCount { get; init; }
    public int? UsersLimit { get; init; }
    public decimal UsersUsedPercentage { get; init; }
    public List<UsageHistoryItem> History { get; init; } = new();
}
