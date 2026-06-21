using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTF.Api.Models;

/// <summary>
/// V4 — Invitation sécurisée pour rejoindre une entreprise (tenant) via QR code / lien.
/// Alternative plus sûre que le partage du TenantId brut : validité limitée dans le temps,
/// nombre d'usages borné, révocable. Le token n'est JAMAIS stocké en clair : seule son
/// empreinte SHA-256 (<see cref="TokenHash"/>) est persistée. Le token clair n'existe qu'au
/// moment de la création (retourné une seule fois pour générer le QR).
/// </summary>
public class TenantInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Tenant que l'invitation permet de rejoindre.</summary>
    public Guid TenantId { get; set; }

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
