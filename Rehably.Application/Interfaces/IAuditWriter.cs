using Rehably.Domain.Entities.Audit;

namespace Rehably.Application.Interfaces;

public interface IAuditWriter
{
    Task WriteAsync(AuditLog auditLog);
}
