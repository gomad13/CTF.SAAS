namespace CTF.Api.Models;

public class LearningPath
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Type { get; set; } = default!;
    public string? JobFamily { get; set; }

    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public string? Level { get; set; }
    public string Status { get; set; } = default!;
    public int Version { get; set; } = 1;

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// True = parcours de catalogue global (pas de tenant propriétaire ; accès accordé
    /// par le SuperAdmin via <see cref="TenantParcoursAccess"/>).
    /// False = parcours privé d'un tenant (<see cref="TenantId"/> non nul).
    /// </summary>
    public bool IsCatalog { get; set; } = false;

    /// <summary>Secteur cible (ex : "sante", "cyber-general", "comptabilite", "finance"). Pour filtrage UI.</summary>
    public string? Sector { get; set; }

    /// <summary>Durée estimée (minutes) — cohérente avec le nombre de challenges (~3 min/challenge).</summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>Tags CSV pour recherche/filtrage (ex : "rgpd,sante,hds").</summary>
    public string? Tags { get; set; }
}
