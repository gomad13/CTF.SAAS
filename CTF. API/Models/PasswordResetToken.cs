using System.Text.Json.Serialization;

namespace CTF.Api.Models;

/// <summary>
/// Jeton de réinitialisation de mot de passe. Le token BRUT n'est JAMAIS stocké :
/// seul son hash SHA-256 (<see cref="TokenHash"/>) est en base. Usage UNIQUE (<see cref="UsedAt"/>),
/// EXPIRATION courte (30 min). Le token brut n'existe que dans le lien envoyé par email.
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>SHA-256 (base64) du token brut. Jamais le token en clair. Indexé pour le lookup.</summary>
    [JsonIgnore]
    public string TokenHash { get; set; } = default!;

    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>IP de la demande (audit / abus). Jamais le token.</summary>
    public string? RequestIp { get; set; }

    public bool IsUsable => UsedAt == null && ExpiresAt > DateTime.UtcNow;
}
