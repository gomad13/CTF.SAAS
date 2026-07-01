using CTF.Api.Contracts;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/competition")]
[Authorize]
public class CompetitionController : ControllerBase
{
    private readonly ICompetitionService _service;
    private readonly TenantContext _tenant;

    public CompetitionController(ICompetitionService service, TenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("status")]
    public async Task<ActionResult<CompetitionStatusDto>> GetStatus(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty)
            return Ok(new CompetitionStatusDto(false));

        var isEnabled = await _service.GetStatusAsync(tenantId, ct);
        return Ok(new CompetitionStatusDto(isEnabled));
    }

    [HttpGet("scoreboard")]
    public async Task<ActionResult<PagedResult<ScoreboardEntryDto>>> GetScoreboard(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var isEnabled = await _service.GetStatusAsync(tenantId, ct);
        if (!isEnabled) return StatusCode(403, new { error = "Competition mode is not enabled for this tenant." });

        var userId = User.GetUserId();
        var result = await _service.GetScoreboardAsync(tenantId, userId, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("podium")]
    public async Task<ActionResult<PodiumDto>> GetPodium(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var isEnabled = await _service.GetStatusAsync(tenantId, ct);
        if (!isEnabled) return StatusCode(403, new { error = "Competition mode is not enabled for this tenant." });

        var userId = User.GetUserId();
        var podium = await _service.GetPodiumAsync(tenantId, userId, ct);
        return Ok(podium);
    }

    // ── Classement individuel (alias explicite de /scoreboard) ───────────────
    [HttpGet("leaderboard/individual")]
    public Task<ActionResult<PagedResult<ScoreboardEntryDto>>> GetIndividualLeaderboard(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => GetScoreboard(page, pageSize, ct);

    // ── Classement par equipe (score = somme des membres) ────────────────────
    [HttpGet("leaderboard/teams")]
    public async Task<ActionResult<List<TeamLeaderboardEntryDto>>> GetTeamLeaderboard(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var isEnabled = await _service.GetStatusAsync(tenantId, ct);
        if (!isEnabled) return StatusCode(403, new { error = "Competition mode is not enabled for this tenant." });

        var userId = User.GetUserId();
        var teams = await _service.GetTeamLeaderboardAsync(tenantId, userId, ct);
        return Ok(teams);
    }

    [HttpGet("podium/teams")]
    public async Task<ActionResult<TeamPodiumDto>> GetTeamPodium(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var isEnabled = await _service.GetStatusAsync(tenantId, ct);
        if (!isEnabled) return StatusCode(403, new { error = "Competition mode is not enabled for this tenant." });

        var userId = User.GetUserId();
        return Ok(await _service.GetTeamPodiumAsync(tenantId, userId, ct));
    }

    // ── Rang de l'utilisateur connecte (individuel + son equipe) ─────────────
    [HttpGet("my-rank")]
    public async Task<ActionResult<MyRankDto>> GetMyRank(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var isEnabled = await _service.GetStatusAsync(tenantId, ct);
        if (!isEnabled) return StatusCode(403, new { error = "Competition mode is not enabled for this tenant." });

        var userId = User.GetUserId();
        return Ok(await _service.GetMyRankAsync(tenantId, userId, ct));
    }

    // ── Enregistrement du temps de resolution (bonus rapidite) ───────────────
    [HttpPost("duration")]
    public async Task<IActionResult> RecordDuration([FromBody] RecordDurationRequestDto req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "Body required." });
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var userId = User.GetUserId();
        var ok = await _service.RecordDurationAsync(tenantId, userId, req.ChallengeId, req.DurationSeconds, ct);
        return Ok(new { recorded = ok });
    }
}

[ApiController]
[Route("api/admin/competition")]
[Authorize(Roles = "admin,SuperAdmin")]
public class AdminCompetitionController : ControllerBase
{
    private readonly ICompetitionService _service;
    private readonly TenantContext _tenant;

    public AdminCompetitionController(ICompetitionService service, TenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpPatch("toggle")]
    public async Task<ActionResult<ToggleCompetitionResponseDto>> Toggle(
        [FromBody] ToggleCompetitionRequestDto req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "Body required." });

        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var adminId = User.GetUserId();
        var result = await _service.ToggleAsync(tenantId, adminId, req.IsEnabled, ct);
        return Ok(result);
    }
}
