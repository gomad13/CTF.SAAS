using System.Security.Claims;

namespace CTF.Api.Security;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(c => c.Type == "user_id");
        if (claim == null || !Guid.TryParse(claim.Value, out var id))
            throw new UnauthorizedAccessException("Missing or invalid 'user_id' claim.");
        return id;
    }

    public static Guid GetTenantId(this ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(c => c.Type == "tenant_id");
        if (claim == null || !Guid.TryParse(claim.Value, out var id))
            throw new UnauthorizedAccessException("Missing or invalid 'tenant_id' claim.");
        return id;
    }
}
