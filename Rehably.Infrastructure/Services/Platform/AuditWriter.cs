using Rehably.Application.Interfaces;
using Rehably.Domain.Entities.Audit;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Platform;

public class AuditWriter : IAuditWriter
{
    private readonly AuditDbContext _auditContext;

    public AuditWriter(AuditDbContext auditContext)
    {
        _auditContext = auditContext;
    }

    public async Task WriteAsync(AuditLog auditLog)
    {
        _auditContext.AuditLogs.Add(auditLog);
        await _auditContext.SaveChangesAsync();
    }
}
