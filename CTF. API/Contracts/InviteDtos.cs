using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Contracts;

/// <summary>Création d'une invitation (admin). Durée en heures + nombre max d'usages.</summary>
public record CreateInviteRequest(
    [Range(1, 720)] int ExpiresInHours,   // 1h .. 30j
    [Range(1, 1000)] int MaxUses
);

/// <summary>
/// Réponse à la création : contient le token EN CLAIR (une seule fois) + l'URL complète à
/// encoder dans le QR. Le token n'est plus jamais renvoyé après cet appel.
/// </summary>
public record CreatedInviteDto(
    Guid Id,
    string Token,
    string JoinUrl,
    DateTime ExpiresAt,
    int MaxUses
);

/// <summary>Invitation listée côté admin — JAMAIS le token ni son empreinte.</summary>
public record InviteDto(
    Guid Id,
    DateTime ExpiresAt,
    int MaxUses,
    int UsedCount,
    bool IsRevoked,
    bool IsExpired,
    DateTime CreatedAt
);

/// <summary>Soumission d'un token par un utilisateur connecté pour rejoindre le tenant.</summary>
public record RedeemInviteRequest(
    [Required] string Token
);

/// <summary>Résultat d'un redeem réussi.</summary>
public record RedeemResultDto(
    Guid TenantId,
    string TenantName
);
