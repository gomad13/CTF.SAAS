using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/admin/compliance")]
[Authorize(Roles = "admin,SuperAdmin")]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceService _service;
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public ComplianceController(IComplianceService service, AppDbContext db, TenantContext tenant)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ComplianceOverviewDto>> Overview(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();
        if (!await ModeToggleHelper.IsEnabledAsync(_db, tenantId, ModeToggleHelper.Mode.Compliance, ct))
            return StatusCode(403, new { error = "Compliance mode is not enabled." });
        return Ok(await _service.GetOverviewAsync(tenantId, ct));
    }

    [HttpPost("assignments")]
    public async Task<ActionResult<MandatoryAssignmentDto>> Create([FromBody] CreateMandatoryAssignmentDto req, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();
        if (!await ModeToggleHelper.IsEnabledAsync(_db, tenantId, ModeToggleHelper.Mode.Compliance, ct))
            return StatusCode(403, new { error = "Compliance mode is not enabled." });
        try
        {
            var dto = await _service.CreateAsync(tenantId, User.GetUserId(), req, ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("assignments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();
        var ok = await _service.DeleteAsync(tenantId, id, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("run-notifications")]
    public async Task<IActionResult> RunNotifications(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();
        if (!await ModeToggleHelper.IsEnabledAsync(_db, tenantId, ModeToggleHelper.Mode.Compliance, ct))
            return StatusCode(403, new { error = "Compliance mode is not enabled." });
        var n = await _service.RunNotificationsAsync(tenantId, ct);
        return Ok(new { created = n });
    }
}

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public NotificationsController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Ok(Array.Empty<object>());
        var userId = User.GetUserId();
        var items = await _db.Notifications.AsNoTracking()
            .Where(n => n.TenantId == tenantId && n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new { n.Id, n.Type, n.Message, n.Link, n.IsRead, n.CreatedAt })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        var userId = User.GetUserId();
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.UserId == userId, ct);
        if (n is null) return NotFound();
        n.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
