using Rehably.Domain.Enums;

namespace Rehably.API.DTOs.Clinic;

public record UploadDocumentForm(
    DocumentType DocumentType,
    IFormFile File
);
