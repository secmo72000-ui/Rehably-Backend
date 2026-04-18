namespace Rehably.Application.DTOs.Clinic;

/// <summary>Full document review response for a clinic's onboarding workflow.</summary>
public record ClinicDocumentsResponseDto
{
    /// <summary>ID of the clinic whose documents are listed.</summary>
    public Guid ClinicId { get; init; }

    /// <summary>Display name of the clinic.</summary>
    public string ClinicName { get; init; } = string.Empty;

    /// <summary>Current onboarding step name.</summary>
    public string OnboardingStatus { get; init; } = string.Empty;

    /// <summary>When the last document batch was uploaded.</summary>
    public DateTime? DocumentsUploadedAt { get; init; }

    /// <summary>All documents uploaded by this clinic.</summary>
    public List<ClinicDocumentDto> Documents { get; init; } = [];
}
