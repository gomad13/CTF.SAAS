namespace CTF.Api.Models;

/// <summary>
/// Assignation d'un parcours (`Path`) à une équipe (`Team`).
/// Règles :
/// - Un parcours peut être assigné à 0..N équipes (UNIQUE (TeamId, PathId)).
/// - Si `IsMandatory = true` et mode Compliance activé, couplage au système d'assignation obligatoire.
/// - Cascade DB : suppression équipe → suppression des assignations, pas des parcours.
/// </summary>
public class TeamParcoursAssignment
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid PathId { get; set; }
    /// <summary>Redondance du TenantId — permet des requêtes tenant-scoped sans jointure.</summary>
    public Guid TenantId { get; set; }

    public DateTime? Deadline { get; set; }
    public bool IsMandatory { get; set; } = false;

    public DateTime AssignedAt { get; set; }
    public Guid AssignedBy { get; set; }
}
