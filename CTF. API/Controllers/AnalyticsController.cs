using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "admin,SuperAdmin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _service;
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public AnalyticsController(IAnalyticsService service, AppDbContext db, TenantContext tenant)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AnalyticsOverviewDto>> GetOverview([FromQuery] int days = 30, CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        if (!await ModeToggleHelper.IsEnabledAsync(_db, tenantId, ModeToggleHelper.Mode.Analytics, ct))
            return StatusCode(403, new { error = "Analytics mode is not enabled for this tenant." });

        var dto = await _service.GetOverviewAsync(tenantId, days, ct);
        return Ok(dto);
    }
}
