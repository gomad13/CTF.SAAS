using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTF.Api.Models;

/// <summary>
/// M3 — Code de double authentification par email (usage unique, expiration courte).
/// Sert deux flux : confirmation d'activation (settings) et vérification au login.
/// Sécurité : ni le code ni le jeton "pending" ne sont stockés en clair — uniquement leurs
/// empreintes SHA-256. Le code est à usage unique, expire vite, et le nombre de tentatives est borné.
/// </summary>
public class TwoFactorCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>Empreinte SHA-256 (base64) du code à 6 chiffres. Jamais le code en clair.</summary>
    [JsonIgnore]
    [MaxLength(64)]
    public string CodeHash { get; set; } = default!;

    /// <summary>
    /// Empreinte SHA-256 du jeton opaque déposé dans le cookie HttpOnly `twofa_pending` au login.
    /// Null pour les codes de confirmation d'activation (l'utilisateur est déjà authentifié).
    /// </summary>
    [JsonIgnore]
    [MaxLength(64)]
    public string? PendingTokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public int Attempts { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
