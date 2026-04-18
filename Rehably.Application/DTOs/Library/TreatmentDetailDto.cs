using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for treatment with its phases included.
/// </summary>
public record TreatmentDetailDto : TreatmentDto
{
    public List<TreatmentPhaseDto> Phases { get; set; } = new();
}
