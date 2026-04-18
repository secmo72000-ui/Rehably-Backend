using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rehably.Domain.Enums;

namespace Rehably.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSubscriptionAttribute : Attribute, IAuthorizationFilter
{
    public SubscriptionStatus[] AllowedStatuses { get; set; } =
    [
        SubscriptionStatus.Active,
        SubscriptionStatus.Trial
    ];

    public bool CheckUsageLimits { get; set; } = false;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Items["SubscriptionStatus"] is not SubscriptionStatus currentStatus)
        {
            context.Result = new StatusCodeResult(401);
            return;
        }

        if (!AllowedStatuses.Contains(currentStatus))
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                error = new
                {
                    code = "SUBSCRIPTION_INVALID",
                    message = GetStatusMessage(currentStatus)
                }
            })
            {
                StatusCode = 402
            };
            return;
        }

        if (CheckUsageLimits && context.HttpContext.Items["UsageWithinLimits"] is bool withinLimits && !withinLimits)
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                error = new
                {
                    code = "USAGE_LIMIT_EXCEEDED",
                    message = "You have exceeded the limits of your current subscription plan."
                }
            })
            {
                StatusCode = 402
            };
        }
    }

    private static string GetStatusMessage(SubscriptionStatus status)
    {
        return status switch
        {
            SubscriptionStatus.Trial => "Your trial period has expired.",
            SubscriptionStatus.Suspended => "Your subscription has been suspended.",
            SubscriptionStatus.Cancelled => "Your subscription has been cancelled.",
            SubscriptionStatus.Expired => "Your subscription has expired.",
            _ => "Your subscription is not active."
        };
    }
}
