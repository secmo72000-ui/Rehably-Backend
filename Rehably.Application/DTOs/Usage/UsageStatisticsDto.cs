namespace Rehably.Application.DTOs.Usage;

public record UsageStatisticsDto
{
    public int PatientsUsed { get; init; }
    public int? PatientsLimit { get; init; }
    public decimal PatientsPercentage { get; init; }
    public int UsersUsed { get; init; }
    public int? UsersLimit { get; init; }
    public decimal UsersPercentage { get; init; }
    public long StorageUsedBytes { get; init; }
    public long StorageLimitBytes { get; init; }
    public decimal StoragePercentage { get; init; }
    public decimal OverallPercentage { get; init; }
}
