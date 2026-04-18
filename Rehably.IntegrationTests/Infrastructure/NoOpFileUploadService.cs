using Rehably.Application.Common;
using Rehably.Application.Services.Storage;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.IntegrationTests.Infrastructure;

public class NoOpFileUploadService : IFileUploadService
{
    public Task<Result<string>> UploadFileAsync(
        Stream stream,
        string fileName,
        string folder,
        Guid? clinicId = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<string>.Success($"https://test-storage.local/{folder}/{fileName}"));
    }

    public Task<Result<bool>> DeleteFileByUrlAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<ClinicDocument>> UploadDocumentAsync(
        Guid clinicId,
        DocumentType documentType,
        string fileName,
        Stream stream,
        DocumentStatus initialStatus = DocumentStatus.Pending,
        CancellationToken cancellationToken = default)
    {
        var document = new ClinicDocument
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            DocumentType = documentType,
            StorageUrl = $"https://test-storage.local/documents/{fileName}",
            PublicUrl = $"https://test-storage.local/documents/{fileName}",
            Status = initialStatus,
            UploadedAt = DateTime.UtcNow
        };

        return Task.FromResult(Result<ClinicDocument>.Success(document));
    }

    public Task<Result<bool>> DeleteDocumentAsync(
        Guid clinicId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<bool> CanUploadAsync(Guid clinicId, long fileSize)
    {
        return Task.FromResult(true);
    }
}
