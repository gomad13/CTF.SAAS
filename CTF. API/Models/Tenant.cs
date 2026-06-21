namespace CTF.Api.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? SsoProvider { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // ── Paramètres entreprise (admin) ─────────────────────────────────────────
    /// <summary>Description libre de l'entreprise (éditable par l'admin).</summary>
    public string? Description { get; set; }
    /// <summary>Secteur d'activité (éditable par l'admin).</summary>
    public string? Sector { get; set; }

    /// <summary>Connexion Google autorisée pour les membres de ce tenant (défaut true).</summary>
    public bool GoogleSsoEnabled { get; set; } = true;
    /// <summary>Connexion Microsoft autorisée pour les membres de ce tenant (défaut true).</summary>
    public bool MicrosoftSsoEnabled { get; set; } = true;

    /// <summary>Réglage par défaut : les nouvelles équipes sont ouvertes (auto-join) ou fermées.</summary>
    public bool DefaultTeamsOpen { get; set; } = false;

    // Mode 1 : Compétition
    public bool IsCompetitionModeEnabled { get; set; } = false;
    public DateTime? CompetitionModeUpdatedAt { get; set; }
    public Guid? CompetitionModeUpdatedBy { get; set; }

    // Mode 2 : Analytics avancés
    public bool IsAnalyticsEnabled { get; set; } = false;
    public DateTime? AnalyticsUpdatedAt { get; set; }
    public Guid? AnalyticsUpdatedBy { get; set; }

    // Mode 3 : Compliance / Formation obligatoire
    public bool IsComplianceEnabled { get; set; } = false;
    public DateTime? ComplianceUpdatedAt { get; set; }
    public Guid? ComplianceUpdatedBy { get; set; }

    // Mode 4 : Équipes & Départements
    public bool IsTeamsEnabled { get; set; } = false;
    public DateTime? TeamsUpdatedAt { get; set; }
    public Guid? TeamsUpdatedBy { get; set; }

    // Mode 5 : Campagnes
    public bool IsCampaignsEnabled { get; set; } = false;
    public DateTime? CampaignsUpdatedAt { get; set; }
    public Guid? CampaignsUpdatedBy { get; set; }
}
