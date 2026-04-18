using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Subscription;

public record UpgradeRequestDto
{
    public Guid TargetPackageId { get; init; }
    public PaymentType PaymentType { get; init; }
}
