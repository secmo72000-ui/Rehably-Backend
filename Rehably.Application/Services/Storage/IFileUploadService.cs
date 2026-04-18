using Rehably.Application.Common;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Storage;

public interface IFileUploadService
{
    Task<Result<string>> UploadFileAsync(
        Stream stream,
        string fileName,
        string folder,
        Guid? clinicId = null,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteFileByUrlAsync(
        string url,
        CancellationToken cancellationToken = default);

    Task<Result<ClinicDocument>> UploadDocumentAsync(
        Guid clinicId,
        DocumentType documentType,
        string fileName,
        Stream stream,
        DocumentStatus initialStatus = DocumentStatus.Pending,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteDocumentAsync(
        Guid clinicId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<bool> CanUploadAsync(Guid clinicId, long fileSize);
}
