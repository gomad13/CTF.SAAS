using System.Security.Claims;

namespace CTF.Api.Middleware;

public class SuperAdminGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SuperAdminGuardMiddleware> _logger;

    public SuperAdminGuardMiddleware(RequestDelegate next, ILogger<SuperAdminGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        if (path.StartsWith("/api/superadmin"))
        {
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var ip   = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (role != "SuperAdmin")
            {
                _logger.LogCritical(
                    "[SUPERADMIN INTRUSION] Role={Role} IP={IP} Path={Path} T={T}",
                    role, ip, path, DateTime.UtcNow);

                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new { error = "Not Found" });
                return;
            }

            _logger.LogWarning("[SUPERADMIN ACCESS] IP={IP} Path={Path}", ip, path);

            context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            context.Response.Headers["Cache-Control"] = "no-store, no-cache";
        }

        await _next(context);
    }
}
