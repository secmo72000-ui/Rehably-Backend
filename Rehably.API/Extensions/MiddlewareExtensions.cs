using Rehably.API.Middleware;

namespace Rehably.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<TenantMiddleware>();
        }

        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ErrorHandlerMiddleware>();
        }

        public static IApplicationBuilder UseFeatureRequirement(this IApplicationBuilder app)
        {
            return app.UseMiddleware<FeatureRequirementMiddleware>();
        }

        public static IApplicationBuilder UseSubscriptionLimit(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SubscriptionLimitMiddleware>();
        }
    }
}
