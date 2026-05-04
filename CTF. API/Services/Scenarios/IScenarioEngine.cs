using CTF.Api.Contracts.Scenarios;

namespace CTF.Api.Services.Scenarios;

/// <summary>
/// Orchestre la vie d'un scénario narratif :
///  - Lancement : valide l'éligibilité, crée l'instance + steps, planifie le 1er envoi.
///  - Exécution : un step = un email rendu + livré dans l'inbox + tracking + planif step suivant.
///  - Tracking : enregistre opens / clicks / reports, déclenche les outcomes.
///  - Stop : annule jobs Hangfire restants + envoie une notification système à la cible.
///
/// Toutes les méthodes sont strictement multi-tenant (Guard sur tenantId).
/// </summary>
public interface IScenarioEngine
{
    /// <summary>Lance une instance et planifie le 1er step.</summary>
    Task<Guid> LaunchAsync(Guid tenantId, Guid launchedByUserId, LaunchScenarioRequest request, CancellationToken ct);

    /// <summary>
    /// Exécute un step : appelée par Hangfire à l'échéance. Rend l'email,
    /// l'insère dans l'inbox, planifie le prochain step (ou le timeout d'ignore).
    /// Tolérante aux re-runs : un step déjà 'sent' est skippé.
    /// </summary>
    Task ExecuteStepAsync(Guid stepInstanceId, CancellationToken ct);

    /// <summary>
    /// Sera appelée par Hangfire après ignore_after_hours si rien ne s'est
    /// passé. Si ignore_next_step_id est défini, planifie le step suivant.
    /// Sinon, conclut sur outcome_neutral.
    /// </summary>
    Task HandleIgnoreTimeoutAsync(Guid emailId, CancellationToken ct);

    /// <summary>Enregistre un open (premier event compte pour FirstReadAt).</summary>
    Task RecordOpenAsync(Guid emailId, string? userAgent, string? ipAddress, CancellationToken ct);

    /// <summary>
    /// Enregistre un click et déclenche outcome_failure (si is_attack_step).
    /// Idempotent : un re-clic n'enregistre pas un nouvel outcome.
    /// </summary>
    Task RecordClickAsync(Guid emailId, string? userAgent, string? ipAddress, CancellationToken ct);

    /// <summary>
    /// Signalement par l'employé. Déclenche outcome_success.
    /// Idempotent : un re-report n'enregistre pas un nouvel outcome.
    /// </summary>
    Task<ReportPhishingResponse> RecordReportAsync(Guid emailId, Guid userId, Guid tenantId, CancellationToken ct);

    /// <summary>Stop manuel par admin : annule Hangfire + notif système à la cible.</summary>
    Task StopInstanceAsync(Guid instanceId, Guid tenantId, Guid stoppedByUserId, string reason, CancellationToken ct);
}
