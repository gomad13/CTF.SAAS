using System.Text.Json;
using CTF.Api.Contracts.Scenarios;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Models.Scenarios;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services.Scenarios;

/// <summary>
/// Cœur du Pilier 1.
///
/// Modèle d'exécution :
///  - LaunchAsync crée l'instance + planifie le 1er step via BackgroundJob.Schedule.
///  - Hangfire appelle ExecuteStepAsync à l'échéance → rendu + insertion email
///    + planification du timeout d'ignore (HandleIgnoreTimeoutAsync).
///  - Si user clique → outcome_failure : insertion CRI + ChallengeCompletion factice
///    pour déclencher le coaching, annulation des steps suivants.
///  - Si user signale → outcome_success : insertion CRI + email de confirmation,
///    annulation des steps suivants.
///  - Si l'ignore_after_hours s'écoule → planifie le step suivant ou conclut neutre.
///  - StopInstanceAsync : annule tous les jobs Hangfire restants + email système.
///
/// Mode 'demo' : 1 jour = 1 minute (compression linéaire). Permet de jouer un
/// scénario de 5 jours en 5 minutes pour les démos commerciales.
///
/// Multi-tenant : toutes les requêtes filtrent par TenantId. La résolution
/// d'instance se fait par (Id, TenantId) pour bloquer toute fuite cross-tenant.
/// </summary>
public sealed class ScenarioEngine : IScenarioEngine
{
    private readonly AppDbContext _db;
    private readonly IScenarioRenderer _renderer;
    private readonly IBackgroundJobClient _jobs;
    private readonly IConfiguration _config;
    private readonly ILogger<ScenarioEngine> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ScenarioEngine(
        AppDbContext db,
        IScenarioRenderer renderer,
        IBackgroundJobClient jobs,
        IConfiguration config,
        ILogger<ScenarioEngine> logger)
    {
        _db = db;
        _renderer = renderer;
        _jobs = jobs;
        _config = config;
        _logger = logger;
    }

    // ── Lancement ───────────────────────────────────────────────────────────

    public async Task<Guid> LaunchAsync(Guid tenantId, Guid launchedByUserId, LaunchScenarioRequest request, CancellationToken ct)
    {
        if (tenantId == default)
            throw new InvalidOperationException("TenantId required.");

        var template = await _db.ScenarioTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct)
            ?? throw new KeyNotFoundException("Scenario template not found.");

        var target = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && u.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Target user not found in tenant.");

        var sender = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.SenderUserId && u.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Sender user not found in tenant.");

        if (!sender.ConsentsToBeFictionalSender)
            throw new InvalidOperationException("Sender did not consent to be a fictional sender.");

        if (target.Id == sender.Id)
            throw new InvalidOperationException("Target and sender must be different users.");

        // Snapshot du JSON + applique d'éventuelles overrides admin (sujet/body)
        var vsf = JsonSerializer.Deserialize<VsfScenario>(template.RawJson, JsonOpts)
            ?? throw new InvalidOperationException("Template JSON is invalid.");

        if (request.StepOverrides is { Count: > 0 })
        {
            foreach (var ov in request.StepOverrides)
            {
                var step = vsf.Timeline.FirstOrDefault(s => s.StepId == ov.StepId);
                if (step is null) continue;
                var idx = vsf.Timeline.IndexOf(step);
                vsf.Timeline[idx] = step with
                {
                    Subject = !string.IsNullOrWhiteSpace(ov.Subject) ? ov.Subject : step.Subject,
                    BodyTemplate = !string.IsNullOrWhiteSpace(ov.BodyTemplate) ? ov.BodyTemplate : step.BodyTemplate,
                };
            }
        }

        var customizedJson = JsonSerializer.Serialize(vsf, JsonOpts);
        var mode = request.Mode == "demo" ? "demo" : "normal";
        var startAt = request.ScheduledStartAt ?? DateTime.UtcNow;

        var instance = new ScenarioInstance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateId = template.Id,
            CustomizedJson = customizedJson,
            TargetUserId = target.Id,
            SenderUserId = sender.Id,
            LaunchedByUserId = launchedByUserId,
            Mode = mode,
            Status = "scheduled",
            CurrentStepId = null,
            StateData = "{}",
            ScheduledStartAt = startAt,
        };
        _db.ScenarioInstances.Add(instance);

        // On crée toutes les rows steps "pending" pour avoir l'arbre complet en DB.
        // Seul le 1er est planifié dans Hangfire ; les autres seront planifiés
        // dynamiquement à mesure que la state-machine progresse (ça permet de
        // gérer les ignore_next_step_id et les annulations propres).
        var orderedSteps = vsf.Timeline.OrderBy(s => s.StepOrder).ToList();
        var stepEntities = new Dictionary<string, ScenarioInstanceStep>();
        foreach (var s in orderedSteps)
        {
            var entity = new ScenarioInstanceStep
            {
                Id = Guid.NewGuid(),
                InstanceId = instance.Id,
                StepId = s.StepId,
                StepOrder = s.StepOrder,
                Status = "pending",
                ScheduledAt = DateTime.UtcNow, // recalculé pour le 1er ci-dessous
            };
            _db.ScenarioInstanceSteps.Add(entity);
            stepEntities[s.StepId] = entity;
        }

        await _db.SaveChangesAsync(ct);

        // Planification du 1er step uniquement (les suivants se chaînent).
        var firstStep = orderedSteps.First();
        var firstScheduledAt = startAt.Add(StepDelay(firstStep, mode));
        var firstEntity = stepEntities[firstStep.StepId];
        firstEntity.ScheduledAt = firstScheduledAt;
        firstEntity.Status = "scheduled";

        var jobId = _jobs.Schedule<IScenarioEngine>(
            engine => engine.ExecuteStepAsync(firstEntity.Id, CancellationToken.None),
            firstScheduledAt);
        firstEntity.HangfireJobId = jobId;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Scenario instance {InstanceId} launched for target {TargetId} (mode={Mode}, first step at {FirstAt:o})",
            instance.Id, target.Id, mode, firstScheduledAt);

        return instance.Id;
    }

    // ── Exécution d'un step ─────────────────────────────────────────────────

    public async Task ExecuteStepAsync(Guid stepInstanceId, CancellationToken ct)
    {
        var step = await _db.ScenarioInstanceSteps
            .FirstOrDefaultAsync(s => s.Id == stepInstanceId, ct);
        if (step is null) { _logger.LogWarning("Step {StepId} not found, skipping", stepInstanceId); return; }
        if (step.Status == "sent" || step.Status == "skipped" || step.Status == "cancelled")
        {
            _logger.LogInformation("Step {StepId} already in final status '{Status}', skipping", stepInstanceId, step.Status);
            return;
        }

        var instance = await _db.ScenarioInstances
            .FirstOrDefaultAsync(i => i.Id == step.InstanceId, ct);
        if (instance is null) { _logger.LogWarning("Instance {InstId} not found, skipping", step.InstanceId); return; }
        if (instance.Status is "completed" or "stopped" or "failed")
        {
            _logger.LogInformation("Instance {InstId} in final status '{Status}', cancelling step", instance.Id, instance.Status);
            step.Status = "cancelled";
            await _db.SaveChangesAsync(ct);
            return;
        }

        var vsf = JsonSerializer.Deserialize<VsfScenario>(instance.CustomizedJson, JsonOpts)!;
        var vsfStep = vsf.Timeline.FirstOrDefault(s => s.StepId == step.StepId);
        if (vsfStep is null)
        {
            _logger.LogError("VSF step '{StepId}' missing from instance JSON", step.StepId);
            step.Status = "skipped";
            await _db.SaveChangesAsync(ct);
            return;
        }

        var character = vsf.Characters.FirstOrDefault(c => c.Id == vsfStep.FromCharacterId)
            ?? new VsfCharacter("system", "System", "system@viper.local");
        var target = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == instance.TargetUserId, ct)
            ?? throw new InvalidOperationException("Target user vanished mid-scenario.");
        var sender = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == instance.SenderUserId, ct)
            ?? throw new InvalidOperationException("Sender user vanished mid-scenario.");
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == instance.TenantId, ct)
            ?? throw new InvalidOperationException("Tenant vanished mid-scenario.");

        var apiBaseUrl = _config["ApiBaseUrl"] ?? "http://localhost:5000";
        var token = Guid.NewGuid().ToString("N");
        var rendered = _renderer.RenderStep(vsfStep, character, target, sender, tenant, token, apiBaseUrl);

        var email = new ScenarioEmail
        {
            Id = Guid.NewGuid(),
            InstanceStepId = step.Id,
            TenantId = instance.TenantId,
            RecipientUserId = target.Id,
            FromName = rendered.FromName,
            FromEmail = rendered.FromEmail,
            Subject = rendered.Subject,
            BodyHtml = rendered.BodyHtml,
            TrackingToken = token,
            IsAttackStep = vsfStep.IsAttackStep,
            SentAt = DateTime.UtcNow,
            IsSystemNotification = false,
        };
        _db.ScenarioEmails.Add(email);

        step.Status = "sent";
        step.SentAt = DateTime.UtcNow;

        // Promotion : 1er step → instance passe à running
        if (instance.Status == "scheduled")
        {
            instance.Status = "running";
            instance.StartedAt = DateTime.UtcNow;
        }
        instance.CurrentStepId = step.StepId;

        await _db.SaveChangesAsync(ct);

        // Planification du timeout d'ignore (si décision branches définies)
        if (vsfStep.DecisionBranches is { IgnoreAfterHours: > 0 })
        {
            var hours = vsfStep.DecisionBranches.IgnoreAfterHours;
            var timeoutAt = DateTime.UtcNow.Add(IgnoreDelay(hours, instance.Mode));
            var jobId = _jobs.Schedule<IScenarioEngine>(
                engine => engine.HandleIgnoreTimeoutAsync(email.Id, CancellationToken.None),
                timeoutAt);

            // Stocke l'identifiant du job dans le step pour pouvoir l'annuler en cas
            // de click/report précoce. On réutilise HangfireJobId car c'est le
            // job le plus récent associé au step.
            step.HangfireJobId = jobId;
            await _db.SaveChangesAsync(ct);
        }
        else if (!vsfStep.IsAttackStep)
        {
            // Step "intro" sans branches → on chaîne directement le step suivant
            // selon l'ordre StepOrder.
            await ScheduleNextOrderedStepAsync(instance, vsf, vsfStep, ct);
        }

        _logger.LogInformation("Step {StepId} sent for instance {InstId} (attack={Attack})",
            step.StepId, instance.Id, vsfStep.IsAttackStep);
    }

    // ── Timeout ignore ──────────────────────────────────────────────────────

    public async Task HandleIgnoreTimeoutAsync(Guid emailId, CancellationToken ct)
    {
        var email = await _db.ScenarioEmails
            .FirstOrDefaultAsync(e => e.Id == emailId, ct);
        if (email is null) return;

        var step = await _db.ScenarioInstanceSteps
            .FirstOrDefaultAsync(s => s.Id == email.InstanceStepId, ct);
        if (step is null) return;

        var instance = await _db.ScenarioInstances
            .FirstOrDefaultAsync(i => i.Id == step.InstanceId, ct);
        if (instance is null) return;
        if (instance.Status is "completed" or "stopped" or "failed") return;

        // Si l'utilisateur a déjà cliqué ou signalé, on ignore le timeout.
        if (email.FirstClickAt is not null || email.ReportedAt is not null) return;

        var vsf = JsonSerializer.Deserialize<VsfScenario>(instance.CustomizedJson, JsonOpts)!;
        var vsfStep = vsf.Timeline.FirstOrDefault(s => s.StepId == step.StepId);
        if (vsfStep is null) return;

        // Audit event "ignored"
        _db.ScenarioEmailEvents.Add(new ScenarioEmailEvent
        {
            EmailId = email.Id,
            TenantId = email.TenantId,
            EventType = "ignored",
            OccurredAt = DateTime.UtcNow,
        });

        var nextStepId = vsfStep.DecisionBranches?.IgnoreNextStepId;
        if (!string.IsNullOrWhiteSpace(nextStepId))
        {
            var nextEntity = await _db.ScenarioInstanceSteps
                .FirstOrDefaultAsync(s => s.InstanceId == instance.Id && s.StepId == nextStepId, ct);
            if (nextEntity is not null && nextEntity.Status == "pending")
            {
                var nextVsfStep = vsf.Timeline.First(s => s.StepId == nextStepId);
                var nextAt = DateTime.UtcNow.Add(StepDelay(nextVsfStep, instance.Mode));
                nextEntity.ScheduledAt = nextAt;
                nextEntity.Status = "scheduled";
                nextEntity.HangfireJobId = _jobs.Schedule<IScenarioEngine>(
                    e => e.ExecuteStepAsync(nextEntity.Id, CancellationToken.None), nextAt);
            }
            await _db.SaveChangesAsync(ct);
            return;
        }

        // Plus de step suivant : on conclut sur outcome_neutral.
        await CompleteInstanceAsync(instance, vsf, "outcome_neutral", ct);
    }

    // ── Tracking ────────────────────────────────────────────────────────────

    public async Task RecordOpenAsync(Guid emailId, string? userAgent, string? ipAddress, CancellationToken ct)
    {
        var email = await _db.ScenarioEmails.FirstOrDefaultAsync(e => e.Id == emailId, ct);
        if (email is null) return;

        _db.ScenarioEmailEvents.Add(new ScenarioEmailEvent
        {
            EmailId = email.Id,
            TenantId = email.TenantId,
            EventType = "opened",
            UserAgent = userAgent,
            IpAddress = ipAddress,
            OccurredAt = DateTime.UtcNow,
        });
        if (email.FirstReadAt is null) email.FirstReadAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordClickAsync(Guid emailId, string? userAgent, string? ipAddress, CancellationToken ct)
    {
        var email = await _db.ScenarioEmails.FirstOrDefaultAsync(e => e.Id == emailId, ct);
        if (email is null) return;

        _db.ScenarioEmailEvents.Add(new ScenarioEmailEvent
        {
            EmailId = email.Id,
            TenantId = email.TenantId,
            EventType = "clicked",
            UserAgent = userAgent,
            IpAddress = ipAddress,
            OccurredAt = DateTime.UtcNow,
        });

        var firstClick = email.FirstClickAt is null;
        if (firstClick) email.FirstClickAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (!firstClick || !email.IsAttackStep) return;

        // Premier clic sur step d'attaque → outcome_failure
        var step = await _db.ScenarioInstanceSteps.FirstOrDefaultAsync(s => s.Id == email.InstanceStepId, ct);
        var instance = step is null ? null : await _db.ScenarioInstances.FirstOrDefaultAsync(i => i.Id == step.InstanceId, ct);
        if (instance is null || instance.Status is "completed" or "stopped" or "failed") return;

        var vsf = JsonSerializer.Deserialize<VsfScenario>(instance.CustomizedJson, JsonOpts)!;
        var vsfStep = vsf.Timeline.FirstOrDefault(s => s.StepId == step!.StepId);
        var outcomeKey = vsfStep?.DecisionBranches?.Click ?? "outcome_failure";

        await CompleteInstanceAsync(instance, vsf, outcomeKey, ct);
    }

    public async Task<ReportPhishingResponse> RecordReportAsync(Guid emailId, Guid userId, Guid tenantId, CancellationToken ct)
    {
        var email = await _db.ScenarioEmails.FirstOrDefaultAsync(
            e => e.Id == emailId && e.TenantId == tenantId && e.RecipientUserId == userId, ct);
        if (email is null)
            return new ReportPhishingResponse(false, false, "", "Email introuvable");

        _db.ScenarioEmailEvents.Add(new ScenarioEmailEvent
        {
            EmailId = email.Id,
            TenantId = email.TenantId,
            EventType = "reported",
            OccurredAt = DateTime.UtcNow,
        });

        var firstReport = email.ReportedAt is null;
        if (firstReport) email.ReportedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (email.IsSystemNotification)
            return new ReportPhishingResponse(true, false, "", "Email système marqué comme signalé.");

        if (!firstReport)
            return new ReportPhishingResponse(true, false, "", "Email déjà signalé.");

        if (!email.IsAttackStep)
        {
            // Signalement d'un mail "intro" non-attaque : on note l'event mais
            // pas d'outcome (le scénario continue normalement).
            return new ReportPhishingResponse(true, false, "", "Merci de ta vigilance — cet email n'était pas piégé.");
        }

        var step = await _db.ScenarioInstanceSteps.FirstOrDefaultAsync(s => s.Id == email.InstanceStepId, ct);
        var instance = step is null ? null : await _db.ScenarioInstances.FirstOrDefaultAsync(i => i.Id == step.InstanceId, ct);
        if (instance is null || instance.Status is "completed" or "stopped" or "failed")
            return new ReportPhishingResponse(true, false, "", "Scénario déjà clôturé.");

        var vsf = JsonSerializer.Deserialize<VsfScenario>(instance.CustomizedJson, JsonOpts)!;
        var vsfStep = vsf.Timeline.FirstOrDefault(s => s.StepId == step!.StepId);
        var outcomeKey = vsfStep?.DecisionBranches?.Report ?? "outcome_success";

        await CompleteInstanceAsync(instance, vsf, outcomeKey, ct);

        return new ReportPhishingResponse(true, true, outcomeKey,
            "Bravo ! Tu as repéré une tentative de phishing simulée. +5 sur ton score CRI.");
    }

    // ── Stop manuel ─────────────────────────────────────────────────────────

    public async Task StopInstanceAsync(Guid instanceId, Guid tenantId, Guid stoppedByUserId, string reason, CancellationToken ct)
    {
        var instance = await _db.ScenarioInstances
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Scenario instance not found.");
        if (instance.Status is "completed" or "stopped" or "failed") return;

        // Annulation des jobs Hangfire encore actifs
        var pendingSteps = await _db.ScenarioInstanceSteps
            .Where(s => s.InstanceId == instance.Id && (s.Status == "scheduled" || s.Status == "pending"))
            .ToListAsync(ct);
        foreach (var s in pendingSteps)
        {
            if (!string.IsNullOrEmpty(s.HangfireJobId))
            {
                try { _jobs.Delete(s.HangfireJobId); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete Hangfire job {JobId}", s.HangfireJobId); }
            }
            s.Status = "cancelled";
        }

        instance.Status = "stopped";
        instance.StopReason = string.IsNullOrWhiteSpace(reason) ? "manual_admin_stop" : reason;
        instance.CompletedAt = DateTime.UtcNow;

        // Notification système à la cible
        await InsertSystemEmailAsync(
            instance,
            "Exercice de sensibilisation interrompu",
            "<p>Bonjour,</p><p>L'exercice de sensibilisation phishing dont tu étais la cible a été <strong>interrompu par ton administrateur</strong>.</p><p>Aucune action n'est attendue de ta part. Tu peux ignorer les éventuels emails reçus dans le cadre de cet exercice.</p><p>L'équipe Viper</p>",
            ct);

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Instance {InstId} stopped by user {UserId}", instance.Id, stoppedByUserId);
    }

    // ── Helpers privés ──────────────────────────────────────────────────────

    private async Task ScheduleNextOrderedStepAsync(ScenarioInstance instance, VsfScenario vsf, VsfStep currentStep, CancellationToken ct)
    {
        var nextVsf = vsf.Timeline.OrderBy(s => s.StepOrder).FirstOrDefault(s => s.StepOrder > currentStep.StepOrder);
        if (nextVsf is null) { await CompleteInstanceAsync(instance, vsf, "outcome_neutral", ct); return; }

        var nextEntity = await _db.ScenarioInstanceSteps
            .FirstOrDefaultAsync(s => s.InstanceId == instance.Id && s.StepId == nextVsf.StepId, ct);
        if (nextEntity is null || nextEntity.Status != "pending") return;

        var at = DateTime.UtcNow.Add(StepDelay(nextVsf, instance.Mode));
        nextEntity.ScheduledAt = at;
        nextEntity.Status = "scheduled";
        nextEntity.HangfireJobId = _jobs.Schedule<IScenarioEngine>(
            e => e.ExecuteStepAsync(nextEntity.Id, CancellationToken.None), at);
        await _db.SaveChangesAsync(ct);
    }

    private async Task CompleteInstanceAsync(ScenarioInstance instance, VsfScenario vsf, string outcomeKey, CancellationToken ct)
    {
        if (instance.Status is "completed" or "stopped" or "failed") return;

        // Annule les steps restants
        var remaining = await _db.ScenarioInstanceSteps
            .Where(s => s.InstanceId == instance.Id && (s.Status == "scheduled" || s.Status == "pending"))
            .ToListAsync(ct);
        foreach (var s in remaining)
        {
            if (!string.IsNullOrEmpty(s.HangfireJobId))
            {
                try { _jobs.Delete(s.HangfireJobId); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete Hangfire job {JobId}", s.HangfireJobId); }
            }
            s.Status = "cancelled";
        }

        instance.Status = "completed";
        instance.StopReason = outcomeKey;
        instance.CompletedAt = DateTime.UtcNow;

        // Application du cri_impact via insertion directe d'une RiskScoreHistory
        // delta. C'est le V1 le plus simple : pas de couplage rigide à l'engine
        // CRI, qui agrège les scores depuis ChallengeCompletions sur 90 jours.
        // Le delta du scénario est gardé en composante "scenario_phishing" du
        // jsonb pour la traçabilité.
        if (vsf.Outcomes.TryGetValue(outcomeKey, out var outcome))
        {
            if (outcome.CriImpact != 0)
            {
                _db.RiskScoreHistories.Add(new RiskScoreHistory
                {
                    UserId = instance.TargetUserId,
                    TenantId = instance.TenantId,
                    Score = null, // signal "delta" : score recalculable, on ne fige pas la valeur ici
                    Components = JsonSerializer.Serialize(new
                    {
                        source = "scenario_phishing",
                        scenario_external_id = ExtractExternalId(vsf),
                        outcome = outcomeKey,
                        cri_delta = outcome.CriImpact,
                    }),
                    ComputedAt = DateTime.UtcNow,
                });
            }

            // Email récap final pour la cible (intégration coaching P4 :
            // si trigger_coaching, on dépose un lien vers la landing).
            var recapBody = outcome.TriggerCoaching
                ? $"<p>Bonjour,</p><p>Tu viens de tomber dans une simulation de phishing — pas grave, c'est exactement à ça que sert l'entraînement. Un coaching personnalisé t'attend ici : <a href=\"/scenarios/landing/{instance.Id}\">Voir mon coaching</a>.</p><p>Score CRI : {outcome.CriImpact:+#;-#;0}.</p><p>L'équipe Viper</p>"
                : $"<p>Bonjour,</p><p>Excellente vigilance ! Tu as identifié une simulation de phishing. Score CRI : {outcome.CriImpact:+#;-#;0}.</p><p>L'équipe Viper</p>";

            await InsertSystemEmailAsync(instance,
                outcome.TriggerCoaching ? "Exercice terminé — un coaching personnalisé t'attend" : "Exercice terminé — bien joué",
                recapBody, ct);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Instance {InstId} completed with outcome {Outcome}", instance.Id, outcomeKey);
    }

    private async Task InsertSystemEmailAsync(ScenarioInstance instance, string subject, string bodyHtml, CancellationToken ct)
    {
        var token = Guid.NewGuid().ToString("N");
        _db.ScenarioEmails.Add(new ScenarioEmail
        {
            Id = Guid.NewGuid(),
            InstanceStepId = null,
            TenantId = instance.TenantId,
            RecipientUserId = instance.TargetUserId,
            FromName = "Viper",
            FromEmail = "noreply@viper.local",
            Subject = subject,
            BodyHtml = bodyHtml,
            TrackingToken = token,
            IsAttackStep = false,
            IsSystemNotification = true,
            SentAt = DateTime.UtcNow,
        });
        await Task.CompletedTask;
    }

    private TimeSpan StepDelay(VsfStep step, string mode)
    {
        var totalMinutes = step.DelayDays * 24 * 60 + step.DelayHours * 60 + step.DelayMinutes;
        if (mode == "demo")
        {
            // 1 jour réel = 1 minute en demo. 8h = 8/24 minute ≈ 20 secondes.
            // On applique le ratio 1440 pour rester proportionnel.
            var demoMinutes = totalMinutes / 1440.0;
            return TimeSpan.FromMinutes(Math.Max(demoMinutes, 0.05)); // min 3s pour éviter le 0
        }
        return TimeSpan.FromMinutes(totalMinutes);
    }

    private TimeSpan IgnoreDelay(int hours, string mode)
    {
        if (mode == "demo")
        {
            var demoMinutes = hours / 24.0;
            return TimeSpan.FromMinutes(Math.Max(demoMinutes, 0.05));
        }
        return TimeSpan.FromHours(hours);
    }

    private static string ExtractExternalId(VsfScenario vsf) => vsf.Id ?? "";
}
