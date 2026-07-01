using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
