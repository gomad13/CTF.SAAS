using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Models;

public class Campaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = default!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Status historiquement stocké mais on le recalcule à la lecture
    // via ICampaignsService.ResolveStatus(). Valeurs canoniques :
    // "Upcoming" | "Active" | "Completed".
    [MaxLength(20)]
    public string Status { get; set; } = "Upcoming";

    // V2 : true = toute l'entreprise (tous les users actifs du tenant).
    public bool AssignedToWholeTenant { get; set; } = false;

    // Archivage doux pour les campagnes Active/Completed que l'admin
    // veut retirer de la liste sans casser l'historique de progression.
    public bool IsArchived { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Ancienne table : lien direct campagne ↔ parcours.
// Conservée pour rétro-compatibilité avec les migrations antérieures —
// les nouvelles écritures passent par CampaignContent (généralisé
// parcours OU scénario). Voir CampaignsService.
public class CampaignPath
{
    public Guid CampaignId { get; set; }
    public Guid PathId { get; set; }
}

public class CampaignTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }

    [MaxLength(20)]
    public string TargetType { get; set; } = "all";

    public Guid? TargetId { get; set; }
}

public class CampaignParticipation
{
    public Guid CampaignId { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public int CompletedParcoursCount { get; set; } = 0;
    public int TotalScore { get; set; } = 0;
    public DateTime? LastActivityAt { get; set; }
}

// ── V2 ─────────────────────────────────────────────────────────────────────
// Contenu inclus dans une campagne. Type discriminé : parcours OU scénario.
// Remplace CampaignPath pour les nouvelles écritures.
public class CampaignContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public Guid TenantId { get; set; }

    // "Parcours" | "Scenario"
    [MaxLength(20)]
    public string ContentType { get; set; } = "Parcours";

    // FK vers LearningPath.Id ou ScenarioTemplate.Id selon ContentType.
    public Guid ContentId { get; set; }

    public int DisplayOrder { get; set; } = 0;
}

// Assignation explicite d'un employé à une campagne. Indépendant des
// CampaignTarget historiques (qui restent pour le ciblage all/team/user).
public class CampaignAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

// Progression individuelle d'un employé sur UN contenu spécifique de la campagne.
// Calculée à partir des sources : Progress (parcours) ou ScenarioInstance (scénario).
public class CampaignProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid CampaignContentId { get; set; }

    // "NotStarted" | "InProgress" | "Completed" | "Failed"
    [MaxLength(20)]
    public string Status { get; set; } = "NotStarted";

    public double? CompletionPercentage { get; set; }
    public bool? IsSuccess { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
