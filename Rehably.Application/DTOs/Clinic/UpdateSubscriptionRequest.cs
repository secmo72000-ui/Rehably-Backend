using System.ComponentModel.DataAnnotations;

namespace Rehably.Application.DTOs.Clinic;

public record UpdateSubscriptionRequest
{
    [Required]
    public Guid NewPackageId { get; init; }
}
