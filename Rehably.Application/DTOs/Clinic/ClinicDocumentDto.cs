namespace Rehably.Application.DTOs.Clinic;

/// <summary>Summary of a single document uploaded by a clinic during onboarding.</summary>
public record ClinicDocumentDto
{
    /// <summary>Unique identifier of the document.</summary>
    public Guid Id { get; init; }

    /// <summary>Document type (e.g. OwnerId, MedicalLicense).</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Public URL where the document can be accessed.</summary>
    public string FileUrl { get; init; } = string.Empty;

    /// <summary>When the document was uploaded.</summary>
    public DateTime UploadedAt { get; init; }

    /// <summary>Current verification status (Pending, Verified, Rejected).</summary>
    public string VerificationStatus { get; init; } = string.Empty;

    /// <summary>Reason for rejection, if the document was rejected.</summary>
    public string? RejectionReason { get; init; }
}
