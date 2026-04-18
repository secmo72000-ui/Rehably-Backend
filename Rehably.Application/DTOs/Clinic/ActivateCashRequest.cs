namespace Rehably.Application.DTOs.Clinic;

public record ActivateCashRequest
{
    public Guid PackageId { get; init; }
}
