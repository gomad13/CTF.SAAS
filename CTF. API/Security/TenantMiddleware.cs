using Microsoft.EntityFrameworkCore;
using CTF.Api.Data;

namespace CTF.Api.Security;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenant, AppDbContext db)
    {
        // Laisser /api/health libre
        if (context.Request.Path.StartsWithSegments("/api/health"))
        {
            await _next(context);
            return;
        }

        // Si pas authentifié -> 401 (plus logique que 400)
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized.");
            return;
        }

        // Tenant uniquement depuis le JWT (claim tenant_id)
        var claimTenant = context.User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;

        if (!Guid.TryParse(claimTenant, out var tenantFromJwt))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Missing tenant in JWT (claim tenant_id).");
            return;
        }

        tenant.TenantId = tenantFromJwt;

        // ✅ Appliquer le tenant au niveau DB pour RLS
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_app_tenant({tenantFromJwt})"
        );

        await _next(context);
    }
}
