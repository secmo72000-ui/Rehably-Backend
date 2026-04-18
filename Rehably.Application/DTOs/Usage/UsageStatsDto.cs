namespace Rehably.Application.DTOs.Usage;

public record UsageStatsDto
{
    public int Limit { get; init; }
    public int Used { get; init; }
    public int Remaining => Limit - Used;
    public double UsagePercentage => Limit > 0 ? (double)Used / Limit * 100 : 0;
}
