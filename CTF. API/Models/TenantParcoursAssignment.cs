namespace CTF.Api.Models;

/// <summary>
/// Activation d'un parcours catalogue par l'Admin d'un tenant pour ses users.
/// Distinct de TenantParcoursAccess (qui est l'accord SuperAdmin → tenant).
/// Un parcours catalogue accordé mais pas activé n'est PAS visible par les users,
/// pour laisser le contrôle à l'Admin entreprise (modèle SaaS à la carte).
///
/// Scope :
///  - "global" = visible par tous les users du tenant
///  - "teams_only" = visible uniquement via TeamParcoursAssignments (Admin doit assigner aux équipes)
/// </summary>
public class TenantParcoursAssignment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PathId { get; set; }

    /// <summary>"global" (tous les users du tenant) ou "teams_only" (gated par TeamParcoursAssignments).</summary>
    public string Scope { get; set; } = "global";

    public DateTime ActivatedAt { get; set; }
    public Guid ActivatedBy { get; set; }

    /// <summary>Désactivation soft : la progression des users reste conservée.</summary>
    public DateTime? DeactivatedAt { get; set; }
    public Guid? DeactivatedBy { get; set; }

    public bool IsActive => DeactivatedAt is null;
}
