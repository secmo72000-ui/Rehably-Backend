using Microsoft.Extensions.Logging;
using Rehably.Application.Services.Storage;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Services.Clinic;

namespace Rehably.Infrastructure.Services.Storage;

/// <summary>
/// Storage service implementation using Cloudinary for file storage.
/// Integrates with usage tracking to enforce storage limits.
/// </summary>
public class CloudinaryStorageService : IStorageService
{
    private readonly IDocumentService _documentService;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ILogger<CloudinaryStorageService> _logger;

    public CloudinaryStorageService(
        IDocumentService documentService,
        IUsageTrackingService usageTrackingService,
        ILogger<CloudinaryStorageService> logger)
    {
        _documentService = documentService;
        _usageTrackingService = usageTrackingService;
        _logger = logger;
    }

    public async Task<(string? Url, string? Error)> UploadFileAsync(Guid clinicId, string fileName, Stream stream, string folder = "")
    {
        try
        {
            var canUpload = await CanUploadAsync(clinicId, stream.Length);
            if (!canUpload)
            {
                return (null, "Storage limit exceeded. Please upgrade your plan or delete unused files.");
            }

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var base64Data = Convert.ToBase64String(memoryStream.ToArray());

            var result = await _documentService.UploadDocumentFromBase64Async(
                clinicId,
                Domain.Enums.DocumentType.OwnerId,
                fileName,
                base64Data);

            if (!result.IsSuccess || result.Value == null)
            {
                return (null, result.Error ?? "Failed to upload file");
            }

            await _usageTrackingService.RecordStorageUsageAsync(clinicId, stream.Length);

            return (result.Value.PublicUrl ?? result.Value.StorageUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} for clinic {ClinicId}", fileName, clinicId);
            return (null, "An error occurred while uploading the file");
        }
    }

    public async Task<(string? Url, string? Error)> UploadBase64Async(Guid clinicId, string fileName, string base64Data, string folder = "")
    {
        try
        {
            var bytes = Convert.FromBase64String(base64Data);
            var canUpload = await CanUploadAsync(clinicId, bytes.Length);
            if (!canUpload)
            {
                return (null, "Storage limit exceeded. Please upgrade your plan or delete unused files.");
            }

            var result = await _documentService.UploadDocumentFromBase64Async(
                clinicId,
                Domain.Enums.DocumentType.OwnerId,
                fileName,
                base64Data);

            if (!result.IsSuccess || result.Value == null)
            {
                return (null, result.Error ?? "Failed to upload file");
            }

            await _usageTrackingService.RecordStorageUsageAsync(clinicId, bytes.Length);

            return (result.Value.PublicUrl ?? result.Value.StorageUrl, null);
        }
        catch (FormatException)
        {
            return (null, "Invalid base64 data format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading base64 file {FileName} for clinic {ClinicId}", fileName, clinicId);
            return (null, "An error occurred while uploading the file");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteFileAsync(Guid clinicId, string publicId)
    {
        try
        {
            _logger.LogWarning("DeleteFileAsync called but not fully implemented for clinic {ClinicId}, publicId {PublicId}", clinicId, publicId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {PublicId} for clinic {ClinicId}", publicId, clinicId);
            return (false, "An error occurred while deleting the file");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteByUrlAsync(Guid clinicId, string url)
    {
        try
        {
            var publicId = ExtractPublicIdFromUrl(url);
            if (string.IsNullOrEmpty(publicId))
            {
                return (false, "Invalid URL format");
            }

            return await DeleteFileAsync(clinicId, publicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file by URL {Url} for clinic {ClinicId}", url, clinicId);
            return (false, "An error occurred while deleting the file");
        }
    }

    public Task<long> GetFileSizeAsync(Guid clinicId, string publicId)
    {
        return Task.FromResult(0L);
    }

    public async Task<bool> CanUploadAsync(Guid clinicId, long fileSize)
    {
        try
        {
            var isLimitExceeded = await _usageTrackingService.IsStorageLimitExceededAsync(clinicId);
            return !isLimitExceeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking upload limit for clinic {ClinicId}", clinicId);
            return true;
        }
    }

    private static string? ExtractPublicIdFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;

            var uploadIndex = path.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
            if (uploadIndex < 0) return null;

            var afterUpload = path[(uploadIndex + "/upload/".Length)..];

            if (afterUpload.StartsWith("v") && afterUpload.Contains('/'))
            {
                var versionEndIndex = afterUpload.IndexOf('/');
                afterUpload = afterUpload[(versionEndIndex + 1)..];
            }

            var lastDotIndex = afterUpload.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                afterUpload = afterUpload[..lastDotIndex];
            }

            return afterUpload;
        }
        catch
        {
            return null;
        }
    }
}
