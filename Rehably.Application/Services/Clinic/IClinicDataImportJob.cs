namespace Rehably.Application.Services.Clinic;

/// <summary>
/// Background job interface for importing clinic data from a file upload.
/// Implementations are enqueued via Hangfire and executed asynchronously.
/// </summary>
public interface IClinicDataImportJob
{
    Task ExecuteAsync(Guid clinicId, string filePath, CancellationToken cancellationToken = default);
}
