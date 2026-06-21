using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Contracts;

/// <summary>
/// Paramètres entreprise renvoyés à l'admin (lecture). Regroupe infos tenant, flags SSO par tenant,
/// l'état de configuration SSO GLOBAL (clés présentes côté serveur), et un aperçu équipes.
/// </summary>
public record TenantSettingsDto(
    Guid TenantId,
    string Name,
    string? Description,
    string? Sector,
    bool GoogleSsoEnabled,
    bool MicrosoftSsoEnabled,
    bool GoogleSsoConfigured,      // clé OAuth Google présente côté serveur (global)
    bool MicrosoftSsoConfigured,   // clé OAuth Microsoft présente côté serveur (global)
    bool DefaultTeamsOpen,
    bool TeamsModeEnabled,
    int TeamsCount,
    DateTime CreatedAt
);

/// <summary>Champs éditables des paramètres entreprise (whitelist serveur).</summary>
public record UpdateTenantSettingsRequest(
    [Required] [StringLength(150, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    [StringLength(120)] string? Sector,
    bool GoogleSsoEnabled,
    bool MicrosoftSsoEnabled,
    bool DefaultTeamsOpen
);
