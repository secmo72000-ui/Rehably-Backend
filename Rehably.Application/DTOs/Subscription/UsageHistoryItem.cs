using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Subscription;

public record UsageHistoryItem
{
    public DateTime Date { get; init; }
    public MetricType MetricType { get; init; }
    public long Value { get; init; }
    public string ValueFormatted { get; init; } = string.Empty;
}
