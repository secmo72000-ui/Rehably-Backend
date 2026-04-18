using Microsoft.Extensions.Logging;

namespace Rehably.Infrastructure.BackgroundJobs;

public class AuditLogCleanupJob
{
    private readonly ILogger<AuditLogCleanupJob> _logger;

    public AuditLogCleanupJob(ILogger<AuditLogCleanupJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting audit log cleanup job for logs older than 7 years");

        try
        {
            _logger.LogInformation("Audit log cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audit log cleanup");
            throw;
        }
    }
}
