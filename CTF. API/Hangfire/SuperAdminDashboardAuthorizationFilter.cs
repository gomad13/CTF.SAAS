using System.Security.Claims;
using Hangfire.Dashboard;

namespace CTF.Api.Hangfire;

/// <summary>
/// Restreint l'accès au dashboard Hangfire (/hangfire) aux seuls SuperAdmins.
///
/// Le filtre s'exécute AU SEIN de la pipeline ASP.NET, donc <c>UseAuthentication()</c>
/// a déjà validé le JWT lu depuis le cookie HttpOnly <c>jwt</c> (cf. Program.cs).
/// Le rôle <c>SuperAdmin</c> est porté par le claim <see cref="ClaimTypes.Role"/>
/// (cf. policy AuthZ "AdminOnly" : Program.cs:129).
///
/// Garde-fou prompt CRI V1 : le dashboard ne doit JAMAIS être accessible sans
/// authentification SuperAdmin valide, même en développement. Toute autre branche
/// retourne <c>false</c> (Hangfire répondra 401).
/// </summary>
public sealed class SuperAdminDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (user?.Identity is not { IsAuthenticated: true })
            return false;

        // Le claim de rôle est positionné par le JWT bearer handler.
        // Pas d'appel BD ici (synchronicité requise par IDashboardAuthorizationFilter).
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        return string.Equals(role, "SuperAdmin", StringComparison.Ordinal);
    }
}
