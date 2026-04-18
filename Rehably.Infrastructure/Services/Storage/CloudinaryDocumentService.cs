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

public class CloudinaryDocumentService : IDocumentService, IDisposable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicRepository _clinicRepository;
    private readonly IRepository<ClinicDocument> _documentRepository;
    private readonly CloudinarySettings _settings;
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryDocumentService> _logger;

    private static readonly string[] s_allowedMimeTypes = new[]
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/jpg",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public CloudinaryDocumentService(
        IUnitOfWork unitOfWork,
        IClinicRepository clinicRepository,
        IRepository<ClinicDocument> documentRepository,
        IOptions<CloudinarySettings> settings,
        ILogger<CloudinaryDocumentService> logger)
    {
        _unitOfWork = unitOfWork;
        _clinicRepository = clinicRepository;
        _documentRepository = documentRepository;
        _settings = settings.Value;
        _logger = logger;

        var account = new Account(
            _settings.CloudName,
            _settings.ApiKey,
            _settings.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<Result<ClinicDocument>> UploadDocumentAsync(
        Guid clinicId,
        DocumentType documentType,
        string fileName,
        Stream stream,
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
                Status = DocumentStatus.Pending,
                UploadedAt = DateTime.UtcNow
            };

            await _documentRepository.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} uploaded for clinic {ClinicId}", document.Id, clinicId);

            return Result<ClinicDocument>.Success(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for clinic {ClinicId}", clinicId);
            return Result<ClinicDocument>.Failure("An error occurred while uploading the document.");
        }
    }

    public async Task<Result<ClinicDocument>> UploadDocumentFromBase64Async(
        Guid clinicId,
        DocumentType documentType,
        string fileName,
        string base64Data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64Data);
            using var stream = new MemoryStream(bytes);
            return await UploadDocumentAsync(clinicId, documentType, fileName, stream, cancellationToken);
        }
        catch (FormatException)
        {
            return Result<ClinicDocument>.Failure("Invalid base64 data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document from base64 for clinic {ClinicId}", clinicId);
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

    public async Task<Result<List<ClinicDocument>>> GetClinicDocumentsAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _documentRepository.FindAsync(d => d.ClinicId == clinicId);

            return Result<List<ClinicDocument>>.Success(documents.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for clinic {ClinicId}", clinicId);
            return Result<List<ClinicDocument>>.Failure("An error occurred while retrieving documents.");
        }
    }

    public async Task<Result<ClinicDocument>> VerifyDocumentAsync(
        Guid clinicId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(documentId);

            if (document == null || document.ClinicId != clinicId)
            {
                return Result<ClinicDocument>.Failure($"Document with ID {documentId} not found for clinic {clinicId}");
            }

            document.Status = DocumentStatus.Verified;
            document.VerifiedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} verified for clinic {ClinicId}", documentId, clinicId);

            return Result<ClinicDocument>.Success(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying document {DocumentId} for clinic {ClinicId}", documentId, clinicId);
            return Result<ClinicDocument>.Failure("An error occurred while verifying the document.");
        }
    }

    public async Task<Result<ClinicDocument>> RejectDocumentAsync(
        Guid clinicId,
        Guid documentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Result<ClinicDocument>.Failure("Rejection reason is required");
            }

            var document = await _documentRepository.GetByIdAsync(documentId);

            if (document == null || document.ClinicId != clinicId)
            {
                return Result<ClinicDocument>.Failure($"Document with ID {documentId} not found for clinic {clinicId}");
            }

            document.Status = DocumentStatus.Rejected;
            document.RejectionReason = reason;
            document.VerifiedAt = null;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} rejected for clinic {ClinicId}: {Reason}", documentId, clinicId, reason);

            return Result<ClinicDocument>.Success(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting document {DocumentId} for clinic {ClinicId}", documentId, clinicId);
            return Result<ClinicDocument>.Failure("An error occurred while rejecting the document.");
        }
    }

    public void Dispose()
    {
    }
}
