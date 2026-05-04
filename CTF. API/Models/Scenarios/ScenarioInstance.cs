using System.Text.Json.Serialization;

namespace CTF.Api.Models.Scenarios;

/// <summary>
/// Exécution concrète d'un <see cref="ScenarioTemplate"/> contre un employé
/// cible. Contient l'état courant de la state-machine et le snapshot du
/// scénario au moment du lancement (immuable pendant l'exécution).
/// </summary>
public class ScenarioInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid TemplateId { get; set; }

    /// <summary>Snapshot personnalisé du scénario (timeline + characters + outcomes) au lancement.</summary>
    public string CustomizedJson { get; set; } = "{}";

    /// <summary>Employé cible du scénario.</summary>
    public Guid TargetUserId { get; set; }

    /// <summary>Employé qui prête son identité comme expéditeur fictif (consentement requis).</summary>
    public Guid SenderUserId { get; set; }

    /// <summary>Admin qui a lancé l'instance.</summary>
    public Guid LaunchedByUserId { get; set; }

    /// <summary>"normal" (1 jour = 1 jour) | "demo" (1 jour = 1 minute).</summary>
    public string Mode { get; set; } = "normal";

    /// <summary>"scheduled" | "running" | "completed" | "stopped" | "failed".</summary>
    public string Status { get; set; } = "scheduled";

    /// <summary>step_id en cours (référence dans CustomizedJson.timeline).</summary>
    public string? CurrentStepId { get; set; }

    /// <summary>Données arbitraires de la state machine (jsonb).</summary>
    public string StateData { get; set; } = "{}";

    public DateTime ScheduledStartAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>"manual_admin_stop" | "outcome_failure" | "outcome_success" | "outcome_neutral" | null.</summary>
    public string? StopReason { get; set; }
}
