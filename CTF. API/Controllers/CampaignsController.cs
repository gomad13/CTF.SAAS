using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/admin/campaigns")]
[Authorize(Roles = "admin,SuperAdmin")]
public class AdminCampaignsController : ControllerBase
{
    private readonly ICampaignsService _service;
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public AdminCampaignsController(ICampaignsService service, AppDbContext db, TenantContext tenant)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
    }

    private async Task<Guid?> TenantIdOrForbidden(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return null;
        if (!await ModeToggleHelper.IsEnabledAsync(_db, tenantId, ModeToggleHelper.Mode.Campaigns, ct)) return null;
        return tenantId;
    }

    // ── Liste enrichie (V2) ───────────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<List<CampaignSummaryDto>>> GetAll(
        [FromQuery] string? status, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        return Ok(await _service.GetAllSummariesAsync(tenantId.Value, status, ct));
    }

    // ── Création V2 — accepte CreateCampaignRequest (contents + assign) ──
    [HttpPost]
    public async Task<ActionResult<CampaignDetailDto>> Create(
        [FromBody] CreateCampaignRequest req, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        try
        {
            var result = await _service.CreateV2Async(tenantId.Value, User.GetUserId(), req, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("available-content")]
    public async Task<ActionResult<List<AvailableContentDto>>> GetAvailableContent(CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        return Ok(await _service.GetAvailableContentAsync(tenantId.Value, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        var c = await _service.GetDetailAsync(tenantId.Value, id, ct);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CampaignDetailDto>> Update(
        Guid id, [FromBody] UpdateCampaignRequest req, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        try
        {
            return Ok(await _service.UpdateAsync(tenantId.Value, id, req, ct));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("introuvable", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Upcoming"))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        var ok = await _service.DeleteAsync(tenantId.Value, id, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(
        Guid id, [FromBody] AssignEmployeesRequest req, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        try
        {
            await _service.AssignEmployeesAsync(tenantId.Value, id, req, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/dashboard")]
    public async Task<ActionResult<CampaignDashboardDto>> GetDashboard(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        try
        {
            return Ok(await _service.GetDashboardAsync(tenantId.Value, id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ── Efficacité (lecture seule, agrégation) — VRAIES données tenant ─────────
    /// <summary>Synthèse : participation/complétion de toutes les campagnes (comparaison).</summary>
    [HttpGet("efficacy")]
    public async Task<ActionResult<CampaignsEfficacyDto>> GetEfficacySummary(CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        return Ok(await ComputeEfficacySummaryAsync(tenantId.Value, ct));
    }

    /// <summary>Efficacité détaillée d'une campagne (participation, complétion, courbe, résultats scénarios).</summary>
    [HttpGet("{id:guid}/efficacy")]
    public async Task<ActionResult<CampaignEfficacyDto>> GetEfficacy(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Campaigns mode is not enabled." });
        var camp = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value, ct);
        if (camp is null) return NotFound();
        return Ok(await ComputeEfficacyAsync(tenantId.Value, camp, ct));
    }

    private static (int started, int completed) CountUsers(List<(Guid UserId, string Status)> prog)
    {
        var byUser = prog.GroupBy(p => p.UserId).ToList();
        var started = byUser.Count(g => g.Any(x => x.Status != "NotStarted"));
        var completed = byUser.Count(g => g.Any() && g.All(x => x.Status == "Completed"));
        return (started, completed);
    }

    private async Task<CampaignsEfficacyDto> ComputeEfficacySummaryAsync(Guid tenantId, CancellationToken ct)
    {
        var camps = await _db.Campaigns.AsNoTracking().Where(c => c.TenantId == tenantId && !c.IsArchived)
            .Select(c => new { c.Id, c.Name, c.Status }).ToListAsync(ct);
        if (camps.Count == 0) return new CampaignsEfficacyDto(new List<CampaignEfficacyRowDto>());
        var campIds = camps.Select(c => c.Id).ToList();

        var assign = await _db.CampaignAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && campIds.Contains(a.CampaignId))
            .Select(a => new { a.CampaignId, a.UserId }).ToListAsync(ct);
        var progress = await _db.CampaignProgresses.AsNoTracking()
            .Where(p => p.TenantId == tenantId && campIds.Contains(p.CampaignId))
            .Select(p => new { p.CampaignId, p.UserId, p.Status }).ToListAsync(ct);

        var rows = camps.Select(c =>
        {
            var total = assign.Count(a => a.CampaignId == c.Id);
            var prog = progress.Where(p => p.CampaignId == c.Id).Select(p => (p.UserId, p.Status)).ToList();
            var (started, completed) = CountUsers(prog);
            var participation = total > 0 ? (int)Math.Round(100.0 * started / total) : 0;
            var completion = total > 0 ? (int)Math.Round(100.0 * completed / total) : 0;
            return new CampaignEfficacyRowDto(c.Id, c.Name, c.Status ?? "Upcoming", total, participation, completion);
        }).ToList();
        return new CampaignsEfficacyDto(rows);
    }

    private async Task<CampaignEfficacyDto> ComputeEfficacyAsync(Guid tenantId, Campaign camp, CancellationToken ct)
    {
        var assignedUsers = await _db.CampaignAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CampaignId == camp.Id).Select(a => a.UserId).ToListAsync(ct);
        var total = assignedUsers.Count;

        var progress = await _db.CampaignProgresses.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.CampaignId == camp.Id)
            .Select(p => new { p.UserId, p.Status, p.IsSuccess, p.CompletedAt }).ToListAsync(ct);

        var (started, completed) = CountUsers(progress.Select(p => (p.UserId, p.Status)).ToList());
        var participation = total > 0 ? (int)Math.Round(100.0 * started / total) : 0;
        var completion = total > 0 ? (int)Math.Round(100.0 * completed / total) : 0;
        var successRows = progress.Where(p => p.IsSuccess != null).ToList();
        var avgSuccess = successRows.Count > 0 ? (int)Math.Round(100.0 * successRows.Count(x => x.IsSuccess == true) / successRows.Count) : 0;

        var completedDates = progress.Where(p => p.Status == "Completed" && p.CompletedAt != null).Select(p => p.CompletedAt!.Value).ToList();
        var trend = BuildCompletionTrend(camp, completedDates);
        var scenario = await ComputeScenarioResultAsync(tenantId, camp.Id, assignedUsers, ct);

        return new CampaignEfficacyDto(camp.Id, camp.Name, camp.Status ?? "Upcoming", total, started, completed,
            participation, completion, avgSuccess, trend, scenario);
    }

    /// <summary>Courbe cumulée des complétions sur la période (jour si &lt;=21j, sinon semaine).</summary>
    private static List<CampaignEfficacyPointDto> BuildCompletionTrend(Campaign camp, List<DateTime> completedAts)
    {
        var start = camp.StartDate.Date;
        var end = camp.EndDate.Date;
        var now = DateTime.UtcNow.Date;
        if (end > now) end = now;
        if (end < start || completedAts.Count == 0) return new List<CampaignEfficacyPointDto>();
        var step = (end - start).Days + 1 > 21 ? 7 : 1;
        var points = new List<CampaignEfficacyPointDto>();
        var cumulative = 0;
        for (var d = start; d <= end; d = d.AddDays(step))
        {
            var bucketEnd = d.AddDays(step);
            cumulative += completedAts.Count(x => x.Date >= d && x.Date < bucketEnd);
            points.Add(new CampaignEfficacyPointDto(d.ToString("dd/MM"), cumulative));
        }
        return points;
    }

    private async Task<CampaignScenarioResultDto?> ComputeScenarioResultAsync(Guid tenantId, Guid campaignId, List<Guid> assignedUsers, CancellationToken ct)
    {
        var templateIds = await _db.CampaignContents.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.CampaignId == campaignId && c.ContentType == "Scenario")
            .Select(c => c.ContentId).ToListAsync(ct);
        if (templateIds.Count == 0) return null;
        if (assignedUsers.Count == 0) return new CampaignScenarioResultDto(templateIds.Count, 0, 0, 0, 0, 0);

        var instanceIds = await _db.ScenarioInstances.AsNoTracking()
            .Where(i => i.TenantId == tenantId && templateIds.Contains(i.TemplateId) && assignedUsers.Contains(i.TargetUserId))
            .Select(i => i.Id).ToListAsync(ct);
        if (instanceIds.Count == 0) return new CampaignScenarioResultDto(templateIds.Count, 0, 0, 0, 0, 0);

        var stepIds = await _db.ScenarioInstanceSteps.AsNoTracking()
            .Where(s => instanceIds.Contains(s.InstanceId)).Select(s => s.Id).ToListAsync(ct);
        if (stepIds.Count == 0) return new CampaignScenarioResultDto(templateIds.Count, 0, 0, 0, 0, 0);

        var attack = await _db.ScenarioEmails.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.IsAttackStep && e.InstanceStepId != null && stepIds.Contains(e.InstanceStepId.Value))
            .Select(e => new { e.FirstClickAt, e.ReportedAt }).ToListAsync(ct);
        var attackCount = attack.Count;
        var clicked = attack.Count(a => a.FirstClickAt != null);
        var reported = attack.Count(a => a.ReportedAt != null);
        var clickRate = attackCount > 0 ? (int)Math.Round(100.0 * clicked / attackCount) : 0;
        var reportRate = attackCount > 0 ? (int)Math.Round(100.0 * reported / attackCount) : 0;
        return new CampaignScenarioResultDto(templateIds.Count, attackCount, clicked, reported, clickRate, reportRate);
    }
}

[ApiController]
[Route("api/campaigns")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignsService _service;
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public CampaignsController(ICampaignsService service, AppDbContext db, TenantContext tenant)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
    }

    // Note : l'endpoint GET /api/campaigns/status est servi par ModesStatusController
    // (helper transverse pour tous les modes : analytics, compliance, teams, campaigns).
    // Pas de duplication ici.

    [HttpGet("active")]
    public async Task<ActionResult<List<CampaignDto>>> Active(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Ok(new List<CampaignDto>());
        var userId = User.GetUserId();
        return Ok(await _service.GetActiveForUserAsync(tenantId, userId, ct));
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<EmployeeCampaignDto>>> MyCampaigns(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Ok(new List<EmployeeCampaignDto>());
        var userId = User.GetUserId();
        return Ok(await _service.GetMyCampaignsAsync(userId, tenantId, ct));
    }
}
