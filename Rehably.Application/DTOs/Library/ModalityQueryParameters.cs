using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Query parameters specific to modality filtering.
/// </summary>
public record ModalityQueryParameters : LibraryQueryParameters
{
    public ModalityType? ModalityType { get; init; }
    public string? TherapeuticCategory { get; init; }
}
