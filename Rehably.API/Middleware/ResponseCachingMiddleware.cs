using Microsoft.Net.Http.Headers;

namespace Rehably.API.Middleware;

/// <summary>
/// Middleware for adding cache headers to responses for GET requests.
/// Helps improve performance by enabling client-side and proxy caching.
/// </summary>
public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseCachingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the ResponseCachingMiddleware.
    /// </summary>
    public ResponseCachingMiddleware(RequestDelegate next, ILogger<ResponseCachingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var cacheControl = context.Request.Headers[HeaderNames.CacheControl].ToString();

            if (cacheControl.Contains("no-cache", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
                context.Response.Headers[HeaderNames.Pragma] = "no-cache";
                context.Response.Headers[HeaderNames.Expires] = "0";
            }
            else
            {
                var path = context.Request.Path.Value ?? string.Empty;

                if (IsCacheablePublicEndpoint(path))
                {
                    context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=300";
                    _logger.LogDebug("Applied public caching to {Path}", path);
                }
                else if (IsCacheablePrivateEndpoint(path))
                {
                    context.Response.Headers[HeaderNames.CacheControl] = "private, max-age=60";
                    _logger.LogDebug("Applied private caching to {Path}", path);
                }
                else
                {
                    context.Response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
                    context.Response.Headers[HeaderNames.Pragma] = "no-cache";
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Determines if the endpoint is a public cacheable resource.
    /// </summary>
    private static bool IsCacheablePublicEndpoint(string path)
    {
        return path.Contains("/api/public/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/api/packages", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/api/features", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the endpoint is a private cacheable resource.
    /// </summary>
    private static bool IsCacheablePrivateEndpoint(string path)
    {
        return path.Contains("/api/tenant/library", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/api/tenant/subscriptions", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Extension methods for registering the response caching middleware.
/// </summary>
public static class ResponseCachingMiddlewareExtensions
{
    /// <summary>
    /// Adds the response caching middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseResponseCachingHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResponseCachingMiddleware>();
    }
}
