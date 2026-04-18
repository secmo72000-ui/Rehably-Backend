using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Rehably.API.Authorization;
using Rehably.Application.Contexts;
using Rehably.Domain.Constants;
using Rehably.Application.Services.Platform;

namespace Rehably.API.Middleware;

public class FeatureRequirementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FeatureRequirementMiddleware> _logger;

    public FeatureRequirementMiddleware(
        RequestDelegate next,
        ILogger<FeatureRequirementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IUsageService usageService)
    {
        var tenantId = tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        var featureAttributes = GetRequireFeatureAttributes(endpoint);
        if (featureAttributes.Count == 0)
        {
            await _next(context);
            return;
        }

        foreach (var featureAttr in featureAttributes)
        {
            var checkResult = await usageService.CanUseFeatureAsync(tenantId.Value, featureAttr.FeatureCode);

            if (!checkResult.IsSuccess)
            {
                var usageResult = await usageService.GetUsageAsync(tenantId.Value, featureAttr.FeatureCode);

                _logger.LogWarning(
                    "Feature requirement failed for tenant {TenantId}, feature {FeatureCode}: {Error}",
                    tenantId.Value, featureAttr.FeatureCode, checkResult.Error);

                context.Response.StatusCode = featureAttr.FeatureCode switch
                {
                    FeatureCodes.Users => 403,
                    FeatureCodes.Storage => 429,
                    FeatureCodes.Sms => 429,
                    FeatureCodes.Email => 429,
                    FeatureCodes.WhatsApp => 429,
                    FeatureCodes.Api => 429,
                    _ => 403
                };

                var errorResponse = new
                {
                    success = false,
                    error = new
                    {
                        code = "FEATURE_LIMIT_EXCEEDED",
                        message = checkResult.Error,
                        featureCode = featureAttr.FeatureCode
                    }
                };

                if (usageResult.IsSuccess && usageResult.Value != null)
                {
                    var usage = usageResult.Value;
                    ((dynamic)errorResponse.error).featureName = usage.FeatureName;
                    ((dynamic)errorResponse.error).usage = new
                    {
                        current = usage.Used,
                        limit = usage.Limit,
                        remaining = usage.Remaining
                    };
                }

                await context.Response.WriteAsJsonAsync(errorResponse);

                return;
            }
        }

        await _next(context);
    }

    private static List<RequireFeatureAttribute> GetRequireFeatureAttributes(Endpoint endpoint)
    {
        var attributes = new List<RequireFeatureAttribute>();

        if (endpoint.Metadata.GetMetadata<RequireFeatureAttribute>() is { } attr)
        {
            attributes.Add(attr);
        }

        var controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (controllerActionDescriptor != null)
        {
            var methodInfo = controllerActionDescriptor.MethodInfo;
            if (methodInfo != null)
            {
                var methodAttributes = methodInfo.GetCustomAttributes<RequireFeatureAttribute>(true);
                attributes.AddRange(methodAttributes);
            }

            if (controllerActionDescriptor.ControllerTypeInfo != null)
            {
                var controllerAttributes = controllerActionDescriptor.ControllerTypeInfo
                    .GetCustomAttributes<RequireFeatureAttribute>(true);
                attributes.AddRange(controllerAttributes);
            }
        }

        return attributes;
    }
}
