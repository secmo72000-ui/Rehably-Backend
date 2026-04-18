using Rehably.Application.DTOs.Usage;

namespace Rehably.Application.DTOs.Platform;

public record ClinicActivityDto
{
    public Guid ClinicId { get; init; }
    public string ClinicName { get; init; } = string.Empty;
    public string? PackageName { get; init; }
    public UsageStatisticsDto Usage { get; init; } = new();
    public List<LoginHistoryDto> DailyLogins { get; init; } = new();
    public List<FailedLoginDto> FailedLogins { get; init; } = new();
}
