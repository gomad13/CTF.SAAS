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
        // Endpoints publics (pas de tenant requis dans le JWT) :
        // - /api/health : monitoring externe
        // - /api/auth/* : login/register/forgot-password/reset-password/oauth (incl. SSO callbacks Google/Microsoft)
        // - /api/test/* : outils dev (en mode Development uniquement, contrôlé ailleurs)
        // - /api/feedback (POST anonyme accepté ; SuperAdmin GET/PATCH passe par le middleware classique avec son JWT)
        // - /api/scenario-tracking/* : pixels d'ouverture + redirections de clic
        //   (l'utilisateur n'est pas authentifié quand il ouvre l'email dans son
        //   client de mail simulé Inbox — le tracking_token GUID v4 fait office
        //   d'identifiant unique non-énumérable, c'est le pattern standard).
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api/health") ||
            path.StartsWithSegments("/api/auth") ||
            path.StartsWithSegments("/api/test") ||
            path.StartsWithSegments("/api/scenario-tracking") ||
            path.StartsWithSegments("/api/legal") ||
            path.StartsWithSegments("/api/support") ||
            (path.Equals("/api/feedback", StringComparison.OrdinalIgnoreCase) &&
             HttpMethods.IsPost(context.Request.Method)))
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

        // ✅ Appliquer le tenant au niveau DB pour RLS (uniquement en provider relationnel —
        //    InMemory ne supporte pas le SQL brut, c'est utilisé en tests d'intégration).
        if (db.Database.IsRelational())
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_app_tenant({tenantFromJwt})"
            );
        }

        await _next(context);
    }
}
