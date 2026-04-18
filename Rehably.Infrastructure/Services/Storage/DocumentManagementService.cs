using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Storage;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Storage;

public class DocumentManagementService : IDocumentManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<ClinicDocument> _documentRepository;
    private readonly ILogger<DocumentManagementService> _logger;

    public DocumentManagementService(
        IUnitOfWork unitOfWork,
        IRepository<ClinicDocument> documentRepository,
        ILogger<DocumentManagementService> logger)
    {
        _unitOfWork = unitOfWork;
        _documentRepository = documentRepository;
        _logger = logger;
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
}
