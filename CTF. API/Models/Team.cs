using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Models;

public class Team
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>Nom d'icône Lucide (ex : "Briefcase", "Heart", "Calculator"). 60 chars max.</summary>
    [MaxLength(60)]
    public string? Icon { get; set; }

    /// <summary>Nombre maximum de membres (capacité). Null = pas de limite. Voir M1/M4 : l'affectation est refusée si l'équipe est pleine.</summary>
    public int? MaxMembers { get; set; }

    /// <summary>
    /// B3 — Équipe ouverte (true) : les utilisateurs du tenant peuvent la rejoindre en autonomie.
    /// Fermée (false, défaut) : seul l'admin peut y affecter des membres.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>Manager de l'équipe — user admin ou user du même tenant. Nullable pour équipes sans manager désigné.</summary>
    public Guid? ManagerId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
