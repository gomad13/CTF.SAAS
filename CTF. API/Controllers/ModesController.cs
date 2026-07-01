using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CTF.Api.Services.ModeToggleHelper;

namespace CTF.Api.Controllers;

// ─── Lecture statuts modes ────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api")]
public class ModesStatusController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public ModesStatusController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("analytics/status")]
    public async Task<ActionResult<ModeStatusDto>> AnalyticsStatus(CancellationToken ct)
        => Ok(new ModeStatusDto(await ReadModeAsync("IsAnalyticsEnabled", ct)));

    [HttpGet("compliance/status")]
    public async Task<ActionResult<ModeStatusDto>> ComplianceStatus(CancellationToken ct)
        => Ok(new ModeStatusDto(await ReadModeAsync("IsComplianceEnabled", ct)));

    [HttpGet("teams/status")]
    public async Task<ActionResult<ModeStatusDto>> TeamsStatus(CancellationToken ct)
        => Ok(new ModeStatusDto(await ReadModeAsync("IsTeamsEnabled", ct)));

    [HttpGet("campaigns/status")]
    public async Task<ActionResult<ModeStatusDto>> CampaignsStatus(CancellationToken ct)
        => Ok(new ModeStatusDto(await ReadModeAsync("IsCampaignsEnabled", ct)));

    private async Task<bool> ReadModeAsync(string column, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return false;
        return await ReadBoolColumnAsync(column, tenantId, ct);
    }

    [HttpGet("modes/all")]
    public async Task<ActionResult<AllModesStatusDto>> All(
        [FromServices] IConfiguration cfg,
        [FromServices] ILogger<ModesStatusController> log,
        CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty)
            return Ok(new AllModesStatusDto(false, false, false, false, false));

        var connStr = cfg.GetConnectionString("DefaultConnection");
        log.LogInformation("modes/all tenantId={TenantId}", tenantId);
        await using var conn = new Npgsql.NpgsqlConnection(connStr);
        await conn.OpenAsync(ct);
        // Inline le Guid pour éliminer tout doute sur le paramétrage Npgsql.
        // tenantId est un Guid validé par le middleware tenant — pas de risque d'injection.
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT ""IsCompetitionModeEnabled"", ""IsAnalyticsEnabled"", ""IsComplianceEnabled"", ""IsTeamsEnabled"", ""IsCampaignsEnabled"" FROM ""Tenants"" WHERE ""Id"" = '{tenantId}'::uuid";
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct))
        {
            log.LogWarning("modes/all no row for tenant {TenantId}", tenantId);
            return Ok(new AllModesStatusDto(false, false, false, false, false));
        }
        var (comp, ana, comp2, teams, camp) = (rd.GetBoolean(0), rd.GetBoolean(1), rd.GetBoolean(2), rd.GetBoolean(3), rd.GetBoolean(4));
        log.LogInformation("modes/all competition={Competition} analytics={Analytics} compliance={Compliance} teams={Teams} campaigns={Campaigns}",
            comp, ana, comp2, teams, camp);
        return Ok(new AllModesStatusDto(comp, ana, comp2, teams, camp));
    }

    private async Task<bool> ReadBoolColumnAsync(string column, Guid tenantId, CancellationToken ct)
    {
        // Connexion dédiée pour éviter toute interférence avec le DbContext partagé.
        var connStr = HttpContext.RequestServices.GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection");
        await using var conn = new Npgsql.NpgsqlConnection(connStr);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT ""{column}"" FROM ""Tenants"" WHERE ""Id"" = @id";
        var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.DbType = System.Data.DbType.Guid; p.Value = tenantId; cmd.Parameters.Add(p);
        var r = await cmd.ExecuteScalarAsync(ct);
        return r is bool b && b;
    }
}

// ─── Toggles admin ────────────────────────────────────────────────────────

[ApiController]
[Authorize(Roles = "admin,SuperAdmin")]
[Route("api/admin")]
public class AdminModesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public AdminModesController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPatch("analytics/toggle")]
    public Task<ActionResult<ToggleModeResponseDto>> ToggleAnalytics([FromBody] ToggleModeRequestDto req, CancellationToken ct)
        => Toggle(Mode.Analytics, req, ct);

    [HttpPatch("compliance/toggle")]
    public Task<ActionResult<ToggleModeResponseDto>> ToggleCompliance([FromBody] ToggleModeRequestDto req, CancellationToken ct)
        => Toggle(Mode.Compliance, req, ct);

    [HttpPatch("teams/toggle")]
    public Task<ActionResult<ToggleModeResponseDto>> ToggleTeams([FromBody] ToggleModeRequestDto req, CancellationToken ct)
        => Toggle(Mode.Teams, req, ct);

    [HttpPatch("campaigns/toggle")]
    public Task<ActionResult<ToggleModeResponseDto>> ToggleCampaigns([FromBody] ToggleModeRequestDto req, CancellationToken ct)
        => Toggle(Mode.Campaigns, req, ct);

    private async Task<ActionResult<ToggleModeResponseDto>> Toggle(Mode mode, ToggleModeRequestDto req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "Body required." });
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();
        var adminId = User.GetUserId();
        var res = await ModeToggleHelper.ToggleAsync(_db, tenantId, adminId, mode, req.IsEnabled, ct);
        return Ok(res);
    }
}
