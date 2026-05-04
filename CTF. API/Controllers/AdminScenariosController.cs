using System.Text.Json;
using CTF.Api.Contracts.Scenarios;
using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services.Scenarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/admin/scenarios")]
[Authorize(Roles = "admin,SuperAdmin")]
public class AdminScenariosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IScenarioEngine _engine;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AdminScenariosController(AppDbContext db, IScenarioEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    // ── Catalogue ───────────────────────────────────────────────────────────

    [HttpGet("catalog")]
    public async Task<ActionResult<List<ScenarioCatalogItemDto>>> GetCatalog(CancellationToken ct)
    {
        var rows = await _db.ScenarioTemplates.AsNoTracking()
            .OrderBy(t => t.Difficulty).ThenBy(t => t.Name)
            .ToListAsync(ct);

        var items = rows.Select(t =>
        {
            var emailCount = 0;
            var attackCount = 0;
            try
            {
                var vsf = JsonSerializer.Deserialize<VsfScenario>(t.RawJson, JsonOpts);
                emailCount = vsf?.Timeline?.Count(s => s.Type == "email") ?? 0;
                attackCount = vsf?.Timeline?.Count(s => s.IsAttackStep) ?? 0;
            }
            catch { /* JSON corrompu : on tolère, le seeder relogue déjà l'erreur */ }

            return new ScenarioCatalogItemDto(
                t.Id, t.ExternalId, t.Version, t.Name, t.Description,
                t.Category, t.Difficulty, t.DurationDays, emailCount, attackCount);
        }).ToList();

        return Ok(items);
    }

    [HttpGet("catalog/{templateId:guid}")]
    public async Task<ActionResult<object>> GetCatalogDetail(Guid templateId, CancellationToken ct)
    {
        var t = await _db.ScenarioTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == templateId, ct);
        if (t is null) return NotFound();

        VsfScenario? vsf = null;
        try { vsf = JsonSerializer.Deserialize<VsfScenario>(t.RawJson, JsonOpts); }
        catch { /* tolérant */ }

        return Ok(new
        {
            t.Id, t.ExternalId, t.Version, t.Name, t.Description,
            t.Category, t.Difficulty, t.DurationDays,
            characters = vsf?.Characters,
            timeline = vsf?.Timeline,
            outcomes = vsf?.Outcomes,
        });
    }

    // ── Lancement ───────────────────────────────────────────────────────────

    [HttpPost("launch")]
    public async Task<ActionResult<object>> Launch([FromBody] LaunchScenarioRequest body, CancellationToken ct)
    {
        if (body is null) return BadRequest(new { error = "Body required." });
        if (body.Mode is not ("normal" or "demo"))
            return BadRequest(new { error = "Mode must be 'normal' or 'demo'." });

        var tenantId = User.GetTenantId();
        var launchedBy = User.GetUserId();

        try
        {
            var instanceId = await _engine.LaunchAsync(tenantId, launchedBy, body, ct);
            return Ok(new { instanceId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Instances ───────────────────────────────────────────────────────────

    [HttpGet("instances")]
    public async Task<ActionResult<List<ScenarioInstanceListItemDto>>> ListInstances(CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var rows = await (
            from i in _db.ScenarioInstances.AsNoTracking()
            join t in _db.ScenarioTemplates.AsNoTracking() on i.TemplateId equals t.Id
            join target in _db.Users.AsNoTracking() on i.TargetUserId equals target.Id
            join sender in _db.Users.AsNoTracking() on i.SenderUserId equals sender.Id
            where i.TenantId == tenantId
            orderby i.ScheduledStartAt descending
            select new
            {
                i.Id, t.Name, t.ExternalId, t.Category,
                TargetEmail = target.Email, TargetFirst = target.FirstName, TargetLast = target.LastName,
                SenderEmail = sender.Email, SenderFirst = sender.FirstName, SenderLast = sender.LastName,
                i.Mode, i.Status, i.CurrentStepId, i.ScheduledStartAt, i.StartedAt, i.CompletedAt, i.StopReason
            })
            .Take(500)
            .ToListAsync(ct);

        var instanceIds = rows.Select(r => r.Id).ToList();

        // Stats par instance via les emails (1 query pour tout)
        var emails = await (
            from e in _db.ScenarioEmails.AsNoTracking()
            join s in _db.ScenarioInstanceSteps.AsNoTracking() on e.InstanceStepId equals s.Id
            where instanceIds.Contains(s.InstanceId) && e.TenantId == tenantId
            select new { s.InstanceId, e.FirstReadAt, e.FirstClickAt, e.ReportedAt })
            .ToListAsync(ct);

        var stats = emails
            .GroupBy(e => e.InstanceId)
            .ToDictionary(g => g.Key, g => new
            {
                Sent = g.Count(),
                Opened = g.Count(x => x.FirstReadAt != null),
                Clicked = g.Count(x => x.FirstClickAt != null),
                Reported = g.Count(x => x.ReportedAt != null),
            });

        var items = rows.Select(r =>
        {
            stats.TryGetValue(r.Id, out var s);
            return new ScenarioInstanceListItemDto(
                r.Id, r.Name, r.ExternalId, r.Category,
                r.TargetEmail, $"{r.TargetFirst} {r.TargetLast}".Trim(),
                r.SenderEmail, $"{r.SenderFirst} {r.SenderLast}".Trim(),
                r.Mode, r.Status, r.CurrentStepId,
                r.ScheduledStartAt, r.StartedAt, r.CompletedAt, r.StopReason,
                s?.Sent ?? 0, s?.Opened ?? 0, s?.Clicked ?? 0, s?.Reported ?? 0);
        }).ToList();

        return Ok(items);
    }

    [HttpGet("instances/{id:guid}")]
    public async Task<ActionResult<ScenarioInstanceDetailDto>> GetInstance(Guid id, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var row = await (
            from i in _db.ScenarioInstances.AsNoTracking()
            join t in _db.ScenarioTemplates.AsNoTracking() on i.TemplateId equals t.Id
            join target in _db.Users.AsNoTracking() on i.TargetUserId equals target.Id
            join sender in _db.Users.AsNoTracking() on i.SenderUserId equals sender.Id
            where i.Id == id && i.TenantId == tenantId
            select new
            {
                i.Id, i.TenantId, i.TemplateId, i.TargetUserId, i.SenderUserId, i.LaunchedByUserId,
                t.Name, t.ExternalId, t.Category,
                TargetEmail = target.Email, TargetFirst = target.FirstName, TargetLast = target.LastName,
                SenderEmail = sender.Email, SenderFirst = sender.FirstName, SenderLast = sender.LastName,
                i.Mode, i.Status, i.CurrentStepId, i.ScheduledStartAt, i.StartedAt, i.CompletedAt, i.StopReason
            }).FirstOrDefaultAsync(ct);

        if (row is null) return NotFound();

        var steps = await _db.ScenarioInstanceSteps.AsNoTracking()
            .Where(s => s.InstanceId == id)
            .OrderBy(s => s.StepOrder)
            .Select(s => new ScenarioInstanceStepDto(s.Id, s.StepId, s.StepOrder, s.Status, s.ScheduledAt, s.SentAt))
            .ToListAsync(ct);

        var stepIdSet = steps.Select(s => s.Id).ToList();
        var emails = await _db.ScenarioEmails.AsNoTracking()
            .Where(e => e.InstanceStepId != null && stepIdSet.Contains(e.InstanceStepId!.Value) && e.TenantId == tenantId)
            .OrderBy(e => e.SentAt)
            .Select(e => new ScenarioInstanceEmailDto(
                e.Id, e.Subject, e.FromName, e.FromEmail, e.IsAttackStep,
                e.SentAt, e.FirstReadAt, e.FirstClickAt, e.ReportedAt))
            .ToListAsync(ct);

        return Ok(new ScenarioInstanceDetailDto(
            row.Id, row.TenantId, row.TemplateId, row.Name, row.ExternalId, row.Category,
            row.TargetUserId, row.TargetEmail, $"{row.TargetFirst} {row.TargetLast}".Trim(),
            row.SenderUserId, row.SenderEmail, $"{row.SenderFirst} {row.SenderLast}".Trim(),
            row.LaunchedByUserId, row.Mode, row.Status, row.CurrentStepId,
            row.ScheduledStartAt, row.StartedAt, row.CompletedAt, row.StopReason,
            steps, emails));
    }

    [HttpPost("instances/{id:guid}/stop")]
    public async Task<IActionResult> StopInstance(Guid id, [FromBody] StopScenarioInstanceRequest body, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var stoppedBy = User.GetUserId();
        try
        {
            await _engine.StopInstanceAsync(id, tenantId, stoppedBy, body?.Reason ?? "manual_admin_stop", ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Senders éligibles ───────────────────────────────────────────────────

    [HttpGet("eligible-senders")]
    public async Task<ActionResult<List<EligibleSenderDto>>> GetEligibleSenders(CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var rows = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive && u.ConsentsToBeFictionalSender)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new EligibleSenderDto(u.Id, u.FirstName, u.LastName, u.Email))
            .ToListAsync(ct);
        return Ok(rows);
    }

    // Liste complète des employés du tenant avec leur flag de consentement
    // expéditeur fictif. Le wizard de lancement affiche TOUS les employés
    // (badge vert/rouge selon le consentement), contrairement à
    // /eligible-senders qui filtre déjà côté serveur. Endpoint dédié pour
    // ne pas casser le contrat existant et garder une seule responsabilité
    // par route.
    [HttpGet("employees")]
    public async Task<ActionResult<List<EmployeeWithConsentDto>>> GetEmployeesWithConsent(CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var rows = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new EmployeeWithConsentDto(
                u.Id, u.FirstName, u.LastName, u.Email, u.Role, u.ConsentsToBeFictionalSender))
            .ToListAsync(ct);
        return Ok(rows);
    }
}
