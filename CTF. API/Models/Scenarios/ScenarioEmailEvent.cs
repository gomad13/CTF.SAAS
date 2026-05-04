namespace CTF.Api.Models.Scenarios;

/// <summary>
/// Événement d'interaction avec un <see cref="ScenarioEmail"/> : ouverture
/// (pixel), clic (lien tracké), report (signalement). Multi-occurrences
/// autorisées (pour audit), seul le premier événement de chaque type
/// modifie l'état de l'instance.
/// </summary>
public class ScenarioEmailEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmailId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>"opened" | "clicked" | "reported" | "ignored".</summary>
    public string EventType { get; set; } = "";

    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
