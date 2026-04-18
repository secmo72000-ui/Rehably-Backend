using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Registration;

public record UploadDocumentRequest(
    DocumentType DocumentType,
    string FileName,
    System.IO.Stream FileStream
);
