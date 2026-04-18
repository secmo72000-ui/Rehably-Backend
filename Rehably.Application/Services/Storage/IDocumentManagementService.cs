using Rehably.Application.Common;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Storage;

public interface IDocumentManagementService
{
    Task<Result<List<ClinicDocument>>> GetClinicDocumentsAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default);

    Task<Result<ClinicDocument>> VerifyDocumentAsync(
        Guid clinicId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<Result<ClinicDocument>> RejectDocumentAsync(
        Guid clinicId,
        Guid documentId,
        string reason,
        CancellationToken cancellationToken = default);
}
