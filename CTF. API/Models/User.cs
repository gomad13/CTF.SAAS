using System.Text.Json.Serialization;

namespace CTF.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? TeamId { get; set; }

    public string Email { get; set; } = default!;
    public string? DisplayName { get; set; }

    public string Role { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public string? PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Dernière activité (soumission challenge, connexion, etc.). Mis à jour périodiquement.</summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>Dernière modification admin (role, team, status) — pour audit annuaire.</summary>
    public DateTime? UpdatedAt { get; set; }

    // ✅ Ajout pour import CSV / admin panel
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;

    // ── SSO providers ────────────────────────────────────────────────────────
    /// <summary>Google OAuth stable subject (claim `sub`).</summary>
    public string? GoogleSubjectId { get; set; }
    /// <summary>Microsoft OAuth stable subject.</summary>
    public string? MicrosoftSubjectId { get; set; }
    /// <summary>URL de l'avatar issu du provider SSO.</summary>
    public string? AvatarUrl { get; set; }
    /// <summary>"password" | "google" | "microsoft" | "multi".</summary>
    public string? AuthProvider { get; set; }

    /// <summary>
    /// Consentement explicite de l'employé pour servir d'expéditeur fictif dans
    /// les scénarios de phishing simulé (Pilier 1). Activable / désactivable
    /// par l'employé lui-même depuis son profil. Aucune donnée réelle n'est
    /// jamais envoyée — seul le From rendu utilise prénom + nom de l'employé.
    /// </summary>
    public bool ConsentsToBeFictionalSender { get; set; } = false;

    /// <summary>
    /// M3 — Double authentification par email activée pour ce compte (optionnelle, par utilisateur).
    /// Si true, le login email+mot de passe exige la saisie d'un code reçu par email avant de délivrer
    /// la session. Le login SSO délègue le MFA au provider.
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Empreinte de sécurité de session. Incluse dans le JWT (claim `sstamp`) et re-vérifiée à chaque
    /// requête. La faire tourner (reset/changement de mot de passe) invalide IMMÉDIATEMENT tous les JWT
    /// existants de l'utilisateur (révocation de session multi-appareils). null = comptes historiques
    /// (aucune vérification tant que non défini). Jamais exposé.
    /// </summary>
    [JsonIgnore]
    public string? SecurityStamp { get; set; }
}
