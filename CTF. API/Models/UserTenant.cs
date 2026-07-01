namespace CTF.Api.Models;

/// <summary>
/// [MULTI-SOCIETES] Appartenance d'un utilisateur à une société (tenant), avec un rôle
/// propre à cette société. Un utilisateur peut posséder plusieurs lignes (plusieurs sociétés) ;
/// une seule est marquée IsDefault (société active par défaut au login).
/// Unicité (UserId, TenantId).
/// </summary>
public class UserTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>Rôle DANS cette société : "user" | "admin" | "owner".</summary>
    public string Role { get; set; } = "user";

    /// <summary>Société active par défaut au login.</summary>
    public bool IsDefault { get; set; } = false;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
