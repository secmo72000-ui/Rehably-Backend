using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Domain.Entities.Audit;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "SecurityStamp", "ResetTokenHash", "ResetTokenSelector",
        "Token", "ConcurrencyStamp", "TwoFactorEnabled", "NormalizedEmail",
        "NormalizedUserName"
    };

    private readonly string? _userId;
    private readonly string? _tenantId;
    private readonly string? _ipAddress;
    private readonly string? _userAgent;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AuditInterceptor> _logger;

    public AuditInterceptor(string? userId, string? tenantId, string? ipAddress, string? userAgent, IServiceScopeFactory serviceScopeFactory, ILogger<AuditInterceptor> logger)
    {
        _userId = userId;
        _tenantId = tenantId;
        _ipAddress = ipAddress;
        _userAgent = userAgent;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await AuditEntitiesAsync(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private AuditLog? CreateAuditLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var entityName = entry.Entity.GetType().Name;
        var entityId = GetEntityId(entry);

        AuditLog? auditLog = entry.State switch
        {
            EntityState.Added => new AuditLog
            {
                TenantId = _tenantId,
                UserId = _userId ?? "",
                ActionType = "Create",
                EntityName = entityName,
                EntityId = entityId,
                IpAddress = _ipAddress,
                UserAgent = _userAgent
            },
            EntityState.Modified => CreateUpdateAuditLog(entry, entityName, entityId),
            EntityState.Deleted => CreateDeleteAuditLog(entry, entityName, entityId),
            _ => null
        };

        return auditLog;
    }

    private AuditLog CreateUpdateAuditLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string entityName, string entityId)
    {
        var auditLog = new AuditLog
        {
            TenantId = _tenantId,
            UserId = _userId ?? "",
            ActionType = "Update",
            EntityName = entityName,
            EntityId = entityId,
            IpAddress = _ipAddress,
            UserAgent = _userAgent
        };

        var properties = entry.Properties.Where(p => p.IsModified && p.Metadata.IsShadowProperty() == false);
        foreach (var property in properties)
        {
            if (SensitiveFields.Contains(property.Metadata.Name))
                continue;

            var oldValue = entry.OriginalValues[property.Metadata.Name]?.ToString();
            var newValue = entry.CurrentValues[property.Metadata.Name]?.ToString();

            if (oldValue != newValue)
            {
                auditLog.Entries.Add(new AuditLogEntry
                {
                    PropertyName = property.Metadata.Name,
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
        }

        return auditLog;
    }

    private AuditLog CreateDeleteAuditLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string entityName, string entityId)
    {
        var auditLog = new AuditLog
        {
            TenantId = _tenantId,
            UserId = _userId ?? "",
            ActionType = "Delete",
            EntityName = entityName,
            EntityId = entityId,
            IpAddress = _ipAddress,
            UserAgent = _userAgent
        };

        foreach (var property in entry.Properties)
        {
            if (SensitiveFields.Contains(property.Metadata.Name))
                continue;

            auditLog.Entries.Add(new AuditLogEntry
            {
                PropertyName = property.Metadata.Name,
                OldValue = entry.OriginalValues[property.Metadata.Name]?.ToString(),
                NewValue = null
            });
        }

        return auditLog;
    }

    private async Task AuditEntitiesAsync(DbContext? context, CancellationToken cancellationToken = default)
    {
        if (context == null) return;

        var changedEntries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .ToList();

        if (!changedEntries.Any())
        {
            return;
        }

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var auditDbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

            foreach (var entry in changedEntries)
            {
                var entityName = entry.Entity.GetType().Name;
                var entityId = GetEntityId(entry);

                AuditLog auditLog;

                if (entry.State == EntityState.Added)
                {
                    auditLog = new AuditLog
                    {
                        TenantId = _tenantId,
                        UserId = _userId ?? "",
                        ActionType = "Create",
                        EntityName = entityName,
                        EntityId = entityId,
                        IpAddress = _ipAddress,
                        UserAgent = _userAgent
                    };
                    auditDbContext.Add(auditLog);
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditLog = new AuditLog
                    {
                        TenantId = _tenantId,
                        UserId = _userId ?? "",
                        ActionType = "Update",
                        EntityName = entityName,
                        EntityId = entityId,
                        IpAddress = _ipAddress,
                        UserAgent = _userAgent
                    };

                    var properties = entry.Properties.Where(p => p.IsModified && p.Metadata.IsShadowProperty() == false);
                    foreach (var property in properties)
                    {
                        if (SensitiveFields.Contains(property.Metadata.Name))
                            continue;

                        var oldValue = entry.OriginalValues[property.Metadata.Name]?.ToString();
                        var newValue = entry.CurrentValues[property.Metadata.Name]?.ToString();

                        if (oldValue != newValue)
                        {
                            auditLog.Entries.Add(new AuditLogEntry
                            {
                                PropertyName = property.Metadata.Name,
                                OldValue = oldValue,
                                NewValue = newValue
                            });
                        }
                    }
                    auditDbContext.Add(auditLog);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    auditLog = new AuditLog
                    {
                        TenantId = _tenantId,
                        UserId = _userId ?? "",
                        ActionType = "Delete",
                        EntityName = entityName,
                        EntityId = entityId,
                        IpAddress = _ipAddress,
                        UserAgent = _userAgent
                    };

                    foreach (var property in entry.Properties)
                    {
                        if (SensitiveFields.Contains(property.Metadata.Name))
                            continue;

                        auditLog.Entries.Add(new AuditLogEntry
                        {
                            PropertyName = property.Metadata.Name,
                            OldValue = entry.OriginalValues[property.Metadata.Name]?.ToString(),
                            NewValue = null
                        });
                    }
                    auditDbContext.Add(auditLog);
                }
            }

            await auditDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist audit log entries.");
            Activity.Current?.AddEvent(new ActivityEvent("AuditError", tags: new ActivityTagsCollection(new Dictionary<string, object?>
            {
                ["exception"] = ex.Message
            })));
        }
    }

    private static string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return idProperty?.CurrentValue?.ToString() ?? string.Empty;
    }
}
