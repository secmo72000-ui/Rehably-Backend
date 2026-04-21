using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rehably.Application.Contexts;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Domain.Entities.Platform;

namespace Rehably.API.Middleware;

public class SubscriptionLimitMiddleware(RequestDelegate next, ILogger<SubscriptionLimitMiddleware> logger)
{
    private static readonly string[] ExcludedPaths =
    [
        "/api/health",
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/registration",
        "/api/payments",
        "/api/webhooks",
        "/api/public",
        "/swagger",
        "/hangfire",
        "/api/admin/platform/subscriptions/webhook"
    ];

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IUsageService usageService,
        ApplicationDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (ShouldExcludePath(path))
        {
            await next(context);
            return;
        }

        var clinicId = GetClinicId(context, tenantContext);
        if (!clinicId.HasValue || clinicId.Value == Guid.Empty)
        {
            await next(context);
            return;
        }

        var subscription = await dbContext.Subscriptions
            .Where(s => s.ClinicId == clinicId.Value)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            logger.LogWarning("No subscription found for clinic {ClinicId}", clinicId);
            await WriteSubscriptionErrorAsync(context, "No active subscription found. Please complete your registration.");
            return;
        }

        if (!IsValidSubscription(subscription))
        {
            logger.LogWarning("Invalid subscription status {Status} for clinic {ClinicId}", subscription.Status, clinicId);
            await WriteSubscriptionErrorAsync(context, GetSubscriptionStatusMessage(subscription));
            return;
        }

        var featureToCheck = GetFeatureForPath(path);
        if (!string.IsNullOrEmpty(featureToCheck))
        {
            var canUseResult = await usageService.CanUseFeatureAsync(clinicId.Value, featureToCheck);
            if (!canUseResult.IsSuccess || !canUseResult.Value)
            {
                logger.LogWarning("Feature {Feature} not available for clinic {ClinicId}", featureToCheck, clinicId);
                await WriteLimitExceededErrorAsync(context, canUseResult.Error ?? $"You have reached your limit for {featureToCheck}. Please upgrade your plan.");
                return;
            }
        }

        context.Items["SubscriptionStatus"] = subscription.Status;
        context.Items["SubscriptionId"] = subscription.Id;
        context.Items["PackageId"] = subscription.PackageId;

        await next(context);
    }

    private static Guid? GetClinicId(HttpContext context, ITenantContext tenantContext)
    {
        var clinicIdClaim = context.User.FindFirst("ClinicId")?.Value;
        if (Guid.TryParse(clinicIdClaim, out var clinicId) && clinicId != Guid.Empty)
            return clinicId;

        var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId) && tenantId != Guid.Empty)
            return tenantId;

        return tenantContext.TenantId;
    }

    private static bool ShouldExcludePath(string path)
    {
        return ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValidSubscription(Subscription subscription)
    {
        return subscription.Status switch
        {
            SubscriptionStatus.Active => subscription.EndDate >= DateTime.UtcNow,
            SubscriptionStatus.Trial when subscription.TrialEndsAt.HasValue => subscription.TrialEndsAt.Value > DateTime.UtcNow,
            _ => false
        };
    }

    private static string GetSubscriptionStatusMessage(Subscription subscription)
    {
        return subscription.Status switch
        {
            SubscriptionStatus.Trial when (!subscription.TrialEndsAt.HasValue || subscription.TrialEndsAt <= DateTime.UtcNow)
                => "Your trial period has expired. Please subscribe to continue using the service.",
            SubscriptionStatus.Suspended => "Your subscription has been suspended. Please contact support.",
            SubscriptionStatus.Cancelled => "Your subscription has been cancelled. Please contact support to reactivate.",
            SubscriptionStatus.Active when subscription.EndDate < DateTime.UtcNow => "Your subscription has expired. Please renew to continue using the service.",
            _ => "Your subscription is not active. Please contact support."
        };
    }

    private static string? GetFeatureForPath(string path)
    {
        // Only gate quota-based features (things with a numeric limit per plan).
        // Core functional features (appointments, invoices, sessions, reports) are
        // included in all plans and must NOT be gated here — they are not seeded
        // as quota features and checking them always returns "not included".

        if (path.Contains("/patients", StringComparison.OrdinalIgnoreCase))
            return "patients";

        if (path.Contains("/users", StringComparison.OrdinalIgnoreCase) || path.Contains("/staff", StringComparison.OrdinalIgnoreCase))
            return "users";

        if (path.Contains("/storage", StringComparison.OrdinalIgnoreCase) || path.Contains("/files", StringComparison.OrdinalIgnoreCase))
            return "storage";

        // SMS / WhatsApp are quota features (limited sends per month)
        if (path.Contains("/sms", StringComparison.OrdinalIgnoreCase))
            return "sms";

        if (path.Contains("/whatsapp", StringComparison.OrdinalIgnoreCase))
            return "whatsapp";

        return null;
    }

    private static async Task WriteSubscriptionErrorAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.PaymentRequired;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            error = new
            {
                code = "SUBSCRIPTION_REQUIRED",
                message = message,
                requiresSubscription = true
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static async Task WriteLimitExceededErrorAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.PaymentRequired;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            error = new
            {
                code = "USAGE_LIMIT_EXCEEDED",
                message = message,
                requiresUpgrade = true
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
