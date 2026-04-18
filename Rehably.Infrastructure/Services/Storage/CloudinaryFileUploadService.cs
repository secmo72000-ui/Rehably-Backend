using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rehably.Application.Common;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Storage;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Services.Storage;

public class CloudinaryFileUploadService : IFileUploadService, IDisposable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicRepository _clinicRepository;
    private readonly IRepository<ClinicDocument> _documentRepository;
    private readonly CloudinarySettings _settings;
    private readonly Cloudinary _cloudinary;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ILogger<CloudinaryFileUploadService> _logger;

    public CloudinaryFileUploadService(
        IUnitOfWork unitOfWork,
        IClinicRepository clinicRepository,
        IRepository<ClinicDocument> documentRepository,
        IOptions<CloudinarySettings> settings,
        IUsageTrackingService usageTrackingService,
        ILogger<CloudinaryFileUploadService> logger)
    {
        _unitOfWork = unitOfWork;
        _clinicRepository = clinicRepository;
        _documentRepository = documentRepository;
        _settings = settings.Value;
        _usageTrackingService = usageTrackingService;
        _logger = logger;

        var account = new Account(
            _settings.CloudName,
            _settings.ApiKey,
            _settings.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<Result<string>> UploadFileAsync(
        Stream stream,
        string fileName,
        string folder,
        Guid? clinicId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (stream.Length > _settings.MaxFileSizeBytes)
            {
                return Result<string>.Failure(
                    $"File size exceeds maximum allowed size of {_settings.MaxFileSizeBytes / (1024 * 1024)}MB");
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (!_settings.AllowedExtensions.Contains(extension))
            {
                return Result<string>.Failure(
                    $"Invalid file type '{extension}'. Allowed types: {string.Join(", ", _settings.AllowedExtensions)}");
            }

            if (clinicId.HasValue)
            {
                var canUpload = await CanUploadAsync(clinicId.Value, stream.Length);
                if (!canUpload)
                {
                    return Result<string>.Failure(
                        "Storage limit exceeded. Please upgrade your plan or delete unused files.");
                }
            }

            var publicId = $"{folder}/{Guid.NewGuid()}";

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                PublicId = publicId,
                Folder = folder,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return Result<string>.Failure("Failed to upload file. Please try again later.");
            }

            var url = uploadResult.SecureUrl?.ToString() ?? string.Empty;

            if (clinicId.HasValue)
            {
                await _usageTrackingService.RecordStorageUsageAsync(clinicId.Value, stream.Length);
            }

            _logger.LogInformation("File uploaded to {Folder} for clinic {ClinicId}", folder, clinicId?.ToString() ?? "N/A");

            return Result<string>.Success(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to folder {Folder}", fileName, folder);
            return Result<string>.Failure("An error occurred while uploading the file.");
        }
    }

    public async Task<Result<bool>> DeleteFileByUrlAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var publicId = ExtractPublicIdFromUrl(url);
            if (string.IsNullOrEmpty(publicId))
            {
                return Result<bool>.Failure("Invalid URL format — could not extract public ID");
            }

            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Auto
            };

            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

            if (deletionResult.Result != "ok")
            {
                _logger.LogWarning("Cloudinary deletion returned non-ok result for URL {Url}: {Result}", url, deletionResult.Result);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file by URL {Url}", url);
            return Result<bool>.Failure("An error occurred while deleting the file.");
        }
    }

    public async Task<Result<ClinicDocument>> UploadDocumentAsync(
        Guid clinicId,
        DocumentType documentType,
        string fileName,
        Stream stream,
        DocumentStatus initialStatus = DocumentStatus.Pending,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (stream.Length > _settings.MaxFileSizeBytes)
            {
                return Result<ClinicDocument>.Failure(
                    $"File size exceeds maximum allowed size of {_settings.MaxFileSizeBytes / (1024 * 1024)}MB");
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_settings.AllowedExtensions.Contains(extension))
            {
                return Result<ClinicDocument>.Failure(
                    $"Invalid file type '{extension}'. Allowed types: {string.Join(", ", _settings.AllowedExtensions)}");
            }

            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return Result<ClinicDocument>.Failure($"Clinic with ID {clinicId} not found");
            }

            var folder = $"{_settings.Folder}/{clinicId}/{documentType.ToString().ToLower()}";
            var publicId = $"{folder}/{Guid.NewGuid()}";

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                PublicId = publicId,
                Folder = folder,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return Result<ClinicDocument>.Failure("Failed to upload document. Please try again later.");
            }

            var document = new ClinicDocument
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                DocumentType = documentType,
                StorageUrl = uploadResult.SecureUrl?.ToString() ?? string.Empty,
                PublicUrl = uploadResult.SecureUrl?.ToString(),
                Status = initialStatus,
                UploadedAt = DateTime.UtcNow
            };

            if (initialStatus == DocumentStatus.Verified)
            {
                document.VerifiedAt = DateTime.UtcNow;
            }

            await _documentRepository.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Document {DocumentId} uploaded for clinic {ClinicId} with status {Status}",
                document.Id, clinicId, initialStatus);

            return Result<ClinicDocument>.Success(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for clinic {ClinicId}", clinicId);
            return Result<ClinicDocument>.Failure("An error occurred while uploading the document.");
        }
    }

    public async Task<Result<bool>> DeleteDocumentAsync(
        Guid clinicId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(documentId);

            if (document == null || document.ClinicId != clinicId)
            {
                return Result<bool>.Failure($"Document with ID {documentId} not found for clinic {clinicId}");
            }

            var uri = new Uri(document.StorageUrl);
            var publicId = uri.AbsolutePath.TrimStart('/').Replace($"{_settings.Folder}/", "");

            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Auto
            };

            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

            if (deletionResult.Result != "ok")
            {
                _logger.LogWarning("Cloudinary deletion failed for document {DocumentId}: {Result}", documentId, deletionResult.Result);
            }

            await _documentRepository.DeleteAsync(document);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} deleted for clinic {ClinicId}", documentId, clinicId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId} for clinic {ClinicId}", documentId, clinicId);
            return Result<bool>.Failure("An error occurred while deleting the document.");
        }
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
            return false;
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

    public void Dispose()
    {
    }
}
