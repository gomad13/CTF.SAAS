using System.Security.Claims;

namespace CTF.Api.Security;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.Claims.First(c => c.Type == "user_id").Value;
        return Guid.Parse(value);
    }

    public static Guid GetTenantId(this ClaimsPrincipal user)
    {
        var value = user.Claims.First(c => c.Type == "tenant_id").Value;
        return Guid.Parse(value);
    }
}
