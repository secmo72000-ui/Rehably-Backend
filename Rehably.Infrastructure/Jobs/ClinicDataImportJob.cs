using Microsoft.Extensions.Logging;
using Rehably.Application.Services.Clinic;

namespace Rehably.Infrastructure.Jobs;

/// <summary>
/// Background job that processes a clinic data import file (.zip or .json).
/// Stub implementation — full import logic to be implemented in a future iteration.
/// </summary>
public class ClinicDataImportJob : IClinicDataImportJob
{
    private readonly ILogger<ClinicDataImportJob> _logger;

    public ClinicDataImportJob(ILogger<ClinicDataImportJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid clinicId, string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting data import for clinic {ClinicId} from file {FilePath}",
            clinicId, filePath);

        await Task.CompletedTask;

        _logger.LogInformation(
            "Data import job completed for clinic {ClinicId}",
            clinicId);
    }
}
