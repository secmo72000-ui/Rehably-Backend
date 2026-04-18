namespace Rehably.Application.DTOs.Subscription;

public record ScheduleDowngradeRequestDto
{
    public Guid TargetPackageId { get; init; }
}
