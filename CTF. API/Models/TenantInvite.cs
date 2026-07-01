using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTF.Api.Models;

/// <summary>
/// Types d'invitation QR (voir QR_3_TYPES) :
///  - <see cref="App"/> (Type 1) : invitation « application » vers l'inscription générale
///    Sentys, SANS rattachement entreprise (TenantId null).
///  - <see cref="EnterpriseSignup"/> (Type 2) : nouveau compte -> rattaché à l'entreprise scellée.
///  - <see cref="EnterpriseJoin"/> (Type 3) : compte existant -> rejoint l'entreprise scellée.
/// Techniquement les types 2 et 3 partagent le même lien /join?token= : la page destination
/// bascule inscription-pré-remplie ou rejoindre selon l'état d'authentification.
/// </summary>
public static class InviteTypes
{
    public const string App = "app";
    public const string EnterpriseSignup = "enterprise_signup";
    public const string EnterpriseJoin = "enterprise_join";

    public static readonly string[] All = { App, EnterpriseSignup, EnterpriseJoin };
    public static bool IsValid(string? t) => t is not null && Array.IndexOf(All, t) >= 0;
    public static bool IsEnterprise(string t) => t == EnterpriseSignup || t == EnterpriseJoin;
}

/// <summary>
/// V4 — Invitation sécurisée via QR code / lien. Selon <see cref="InviteType"/> :
/// invitation application (Type 1, sans tenant) ou invitation entreprise (Types 2/3,
/// scellée à <see cref="TenantId"/>). Validité limitée dans le temps, usages bornés,
/// révocable. Le token n'est JAMAIS stocké en clair : seule son empreinte SHA-256
/// (<see cref="TokenHash"/>) est persistée ; le token clair n'existe qu'à la création.
/// </summary>
public class TenantInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tenant que l'invitation permet de rejoindre. NULL pour une invitation « application »
    /// (Type 1) qui ne rattache à aucune entreprise.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>Type d'invitation (voir <see cref="InviteTypes"/>).</summary>
    [MaxLength(32)]
    public string InviteType { get; set; } = InviteTypes.EnterpriseJoin;

    /// <summary>Empreinte SHA-256 (base64) du token d'invitation. Jamais le token en clair.</summary>
    [JsonIgnore]
    [MaxLength(64)]
    public string TokenHash { get; set; } = default!;

    /// <summary>Date d'expiration (UTC). Au-delà, l'invitation est refusée.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Nombre maximum d'utilisations autorisées.</summary>
    public int MaxUses { get; set; }

    /// <summary>Nombre d'utilisations déjà consommées.</summary>
    public int UsedCount { get; set; }

    /// <summary>Admin ayant créé l'invitation.</summary>
    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Révocation manuelle par un admin.</summary>
    public bool IsRevoked { get; set; }
}
