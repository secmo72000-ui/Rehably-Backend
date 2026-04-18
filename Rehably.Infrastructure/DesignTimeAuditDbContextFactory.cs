using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure;

public class DesignTimeAuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("REHABLY_AUDIT_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set REHABLY_AUDIT_CONNECTION_STRING environment variable");

        optionsBuilder.UseNpgsql(connectionString);

        return new AuditDbContext(optionsBuilder.Options);
    }
}
