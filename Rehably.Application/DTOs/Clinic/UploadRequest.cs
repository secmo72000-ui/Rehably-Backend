using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Clinic;

public record UploadRequest(
    string? Base64Data = null,
    string? FileName = null,
    Guid? ClinicId = null,
    DocumentType? DocumentType = null
);
