namespace CTF.Api.Models;

/// <summary>
/// Accès à un parcours de catalogue (<see cref="LearningPath.IsCatalog"/> = true) accordé
/// à un tenant par le SuperAdmin. Modèle licence à la carte.
///
/// Règles :
/// - UNIQUE (TenantId, PathId) — une seule row actuelle par couple.
/// - Soft delete via <see cref="RevokedAt"/> : la row est conservée pour traçabilité.
/// - <see cref="IsActive"/> calculé = <c>RevokedAt == null</c>.
/// - ON DELETE CASCADE si le parcours ou le tenant est supprimé (via Cascade EF).
/// </summary>
public class TenantParcoursAccess
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Guid PathId { get; set; }

    public DateTime GrantedAt { get; set; }
    public Guid GrantedBy { get; set; }

    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedBy { get; set; }

    /// <summary>Calculé : true si RevokedAt est null.</summary>
    public bool IsActive => RevokedAt is null;
}
