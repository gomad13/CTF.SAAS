using System.Text.Json;
using CTF.Api.Contracts.Scenarios;
using CTF.Api.Data;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Détails d'une landing page post-clic ou post-report. L'employé authentifié
/// récupère ici les hints du step + les outcomes pour orchestrer le coaching
/// IA (Pilier 4) côté frontend. Filtre strict user + tenant : un employé ne
/// peut consulter que la landing d'un email reçu par lui.
/// </summary>
[ApiController]
[Route("api/scenarios/landing")]
[Authorize(Roles = "user,admin,SuperAdmin")]
public class ScenariosLandingController : ControllerBase
{
    private readonly AppDbContext _db;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ScenariosLandingController(AppDbContext db) { _db = db; }

    [HttpGet("{token}")]
    public async Task<ActionResult<object>> Get(string token, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        var email = await _db.ScenarioEmails.AsNoTracking()
            .FirstOrDefaultAsync(e => e.TrackingToken == token
                && e.RecipientUserId == userId
                && e.TenantId == tenantId, ct);
        if (email is null) return NotFound();

        var hints = new List<string>();
        Guid? instanceId = null;
        string? scenarioName = null;
        string? scenarioCategory = null;

        if (email.InstanceStepId is not null)
        {
            var step = await _db.ScenarioInstanceSteps.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == email.InstanceStepId, ct);
            if (step is not null)
            {
                instanceId = step.InstanceId;
                var instance = await _db.ScenarioInstances.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == step.InstanceId, ct);
                if (instance is not null)
                {
                    var template = await _db.ScenarioTemplates.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == instance.TemplateId, ct);
                    scenarioName = template?.Name;
                    scenarioCategory = template?.Category;
                    try
                    {
                        var vsf = JsonSerializer.Deserialize<VsfScenario>(instance.CustomizedJson, JsonOpts);
                        var vsfStep = vsf?.Timeline?.FirstOrDefault(s => s.StepId == step.StepId);
                        if (vsfStep?.Hints is { Count: > 0 }) hints = vsfStep.Hints;
                    }
                    catch { /* tolérant */ }
                }
            }
        }

        return Ok(new
        {
            emailId = email.Id,
            instanceId,
            subject = email.Subject,
            fromName = email.FromName,
            fromEmail = email.FromEmail,
            isAttackStep = email.IsAttackStep,
            wasClicked = email.FirstClickAt is not null,
            wasReported = email.ReportedAt is not null,
            scenarioName,
            scenarioCategory,
            hints,
        });
    }
}
