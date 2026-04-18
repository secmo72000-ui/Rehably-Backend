using System.Security.Claims;
using System.Text.Json;

namespace Rehably.API.Middleware;

public class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var user = context.User;

        if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
        {
            var mustChangePasswordClaim = user.FindFirst("mustChangePassword")?.Value;

            if (mustChangePasswordClaim == "true")
            {
                var path = context.Request.Path.Value ?? string.Empty;

                var isChangePasswordEndpoint = path.StartsWith("/api/auth/change-password", StringComparison.OrdinalIgnoreCase);

                if (!isChangePasswordEndpoint)
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        success = false,
                        error = "You must change your password before accessing this resource"
                    };

                    var json = JsonSerializer.Serialize(response);
                    await context.Response.WriteAsync(json);
                    return;
                }
            }
        }

        await _next(context);
    }
}
