namespace CTF.Api.Models.Scenarios;

/// <summary>
/// Étape planifiée d'une <see cref="ScenarioInstance"/>. Une ligne = une
/// occurrence d'un step VSF prévue à un moment précis. Les jobs Hangfire
/// référencent cette ligne pour matérialiser leurs envois.
/// </summary>
public class ScenarioInstanceStep
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InstanceId { get; set; }

    /// <summary>step_id du JSON VSF (ex. "email_3_attaque").</summary>
    public string StepId { get; set; } = "";

    public int StepOrder { get; set; }

    /// <summary>"pending" | "scheduled" | "sent" | "skipped" | "cancelled".</summary>
    public string Status { get; set; } = "pending";

    public DateTime ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }

    /// <summary>Identifiant du job Hangfire planifié pour cet envoi (pour annulation).</summary>
    public string? HangfireJobId { get; set; }
}
