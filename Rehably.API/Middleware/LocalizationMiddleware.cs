using System.Globalization;

namespace Rehably.API.Middleware;

/// <summary>
/// Middleware for handling request localization based on Accept-Language header.
/// Supports English (en) and Arabic (ar).
/// </summary>
public class LocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LocalizationMiddleware> _logger;
    private readonly string[] _supportedCultures = { "en", "ar" };
    private const string DefaultCulture = "en";

    /// <summary>
    /// Initializes a new instance of the LocalizationMiddleware.
    /// </summary>
    public LocalizationMiddleware(RequestDelegate next, ILogger<LocalizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var acceptLanguage = context.Request.Headers.AcceptLanguage.ToString();

        var culture = DetermineCulture(acceptLanguage);

        var cultureInfo = new CultureInfo(culture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        context.Response.Headers["X-Content-Language"] = culture;

        _logger.LogDebug("Request culture set to {Culture}", culture);

        await _next(context);
    }

    /// <summary>
    /// Determines the appropriate culture based on the Accept-Language header.
    /// </summary>
    private string DetermineCulture(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return DefaultCulture;
        }

        var languages = acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var lang in languages)
        {
            var parts = lang.Split(';');
            var cultureName = parts[0].Trim();

            var twoLetterCode = cultureName.Length >= 2
                ? cultureName.Substring(0, 2).ToLowerInvariant()
                : cultureName.ToLowerInvariant();

            if (_supportedCultures.Contains(twoLetterCode))
            {
                return twoLetterCode;
            }
        }

        return DefaultCulture;
    }
}

/// <summary>
/// Extension methods for registering the localization middleware.
/// </summary>
public static class LocalizationMiddlewareExtensions
{
    /// <summary>
    /// Adds the localization middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseRequestLocalization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LocalizationMiddleware>();
    }
}
