using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Tenant;

public class ClinicDocument
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public string? PublicUrl { get; set; }
    public DocumentStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }

    public Clinic? Clinic { get; set; }
}
