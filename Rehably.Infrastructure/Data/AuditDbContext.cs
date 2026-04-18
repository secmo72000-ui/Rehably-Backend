using Microsoft.EntityFrameworkCore;
using Rehably.Domain.Entities.Audit;

namespace Rehably.Infrastructure.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AuditLogEntry> AuditLogEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.IsSuccess);
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.OtpReference).HasMaxLength(20);

            entity.HasMany(a => a.Entries)
                .WithOne(e => e.AuditLog)
                .HasForeignKey(e => e.AuditLogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLogEntry>(entity =>
        {
            entity.Property(e => e.PropertyName).HasMaxLength(100);
            entity.Property(e => e.OldValue).HasMaxLength(1000);
            entity.Property(e => e.NewValue).HasMaxLength(1000);
        });
    }
}
