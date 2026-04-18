using Microsoft.EntityFrameworkCore;
using Npgsql;
using Rehably.Application.Contexts;
using Rehably.Application.Services.Clinic;
using Rehably.Infrastructure.Data;
using Rehably.Domain.Enums;
using System.Security.Claims;

namespace Rehably.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    private static readonly string[] AllowAnonymousPaths =
    {
        "/api/auth",
        "/api/registration",
        "/api/webhooks",
        "/health",
        "/swagger",
        "/api/health"
    };

    public TenantMiddleware(
        RequestDelegate next,
        ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ApplicationDbContext db, ITenantResolutionService tenantResolutionService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (IsAnonymousPath(path))
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier);
        var tenantIdClaim = context.User?.FindFirst("TenantId");

        if (tenantIdClaim == null)
        {
            var roles = context.User?.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            if (roles != null && roles.Contains("PlatformAdmin"))
            {
                await SetRlsTenantId(db, string.Empty);
                await _next(context);
                return;
            }
        }

        if (tenantIdClaim != null)
        {
            if (!Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                _logger.LogWarning("Invalid TenantId format: {TenantId}", tenantIdClaim.Value);
                context.Response.StatusCode = 401;
                return;
            }

            var clinicInfo = await tenantResolutionService.ResolveClinicAsync(tenantId);

            if (clinicInfo == null || !clinicInfo.Value.Exists)
            {
                _logger.LogWarning("Clinic {ClinicId} not found", tenantId);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Clinic not found" });
                return;
            }

            if (clinicInfo.Value.Status == ClinicStatus.Suspended)
            {
                _logger.LogWarning("Clinic {ClinicId} is suspended", tenantId);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Clinic is suspended. Please contact support." });
                return;
            }

            if (clinicInfo.Value.Status == ClinicStatus.Cancelled)
            {
                _logger.LogWarning("Clinic {ClinicId} is cancelled", tenantId);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Clinic registration was cancelled." });
                return;
            }

            if (clinicInfo.Value.Status == ClinicStatus.PendingPayment && !path.StartsWith("/api/payment") && !path.StartsWith("/api/registration"))
            {
                _logger.LogWarning("Clinic {ClinicId} has pending payment", tenantId);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Payment required. Please complete your subscription payment." });
                return;
            }

            tenantContext.SetTenant(tenantId);
            await SetRlsTenantId(db, tenantId.ToString());
            _logger.LogDebug("Tenant ID from JWT: {TenantId}, Status: {Status}", tenantId, clinicInfo.Value.Status);
        }

        if (userIdClaim != null)
        {
            tenantContext.SetUser(userIdClaim.Value);
        }

        await _next(context);
    }

    private static bool IsAnonymousPath(string path)
    {
        return AllowAnonymousPaths.Any(allowedPath => path.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task SetRlsTenantId(ApplicationDbContext db, string tenantId, CancellationToken ct = default)
    {
        if (!db.Database.IsRelational()) return;

        // Security: validate that tenantId is either empty (platform admin) or a well-formed GUID.
        // An invalid value throws FormatException here rather than reaching the database.
        string configValue;
        if (string.IsNullOrEmpty(tenantId))
        {
            configValue = string.Empty; // Platform admins: RLS policy treats empty string as "bypass"
        }
        else
        {
            // Guid.Parse throws FormatException for any non-GUID input — stops injection at the door
            configValue = Guid.Parse(tenantId).ToString();
        }

        // Use ExecuteSqlRawAsync with an explicit named parameter.
        // This is unambiguously parameterized SQL — configValue never concatenates into the query string.
        await db.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_tenant_id', @tenantId, true)",
            new NpgsqlParameter("@tenantId", configValue));
    }
}
