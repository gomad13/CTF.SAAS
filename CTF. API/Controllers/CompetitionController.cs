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

    // ── Classement individuel PUBLIC — TOP 5 nominatif uniquement (RGPD anti-stigmatisation) ──
    // Aucun rang/score nominatif au-delà du top 5 n'est exposé aux membres. La liste complète
    // est réservée à l'admin (api/admin/competition/leaderboard).
    [HttpGet("scoreboard")]
    public async Task<ActionResult<PagedResult<ScoreboardEntryDto>>> GetScoreboard(CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var isEnabled = await _service.GetStatusAsync(tenantId, ct);
        if (!isEnabled) return StatusCode(403, new { error = "Competition mode is not enabled for this tenant." });

        var userId = User.GetUserId();
        var top = await _service.GetTopIndividualsAsync(tenantId, userId, 5, ct);
        return Ok(new PagedResult<ScoreboardEntryDto>(top, 1, 5, top.Count));
    }

    [HttpGet("individual/top5")]
    public async Task<ActionResult<List<ScoreboardEntryDto>>> GetTopIndividuals(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var isEnabled = await _service.GetStatusAsync(tenantId, ct);
        if (!isEnabled) return StatusCode(403, new { error = "Competition mode is not enabled for this tenant." });

        var userId = User.GetUserId();
        return Ok(await _service.GetTopIndividualsAsync(tenantId, userId, 5, ct));
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

    // ── Classement individuel PUBLIC (alias de /scoreboard) — top 5 uniquement (RGPD) ──
    [HttpGet("leaderboard/individual")]
    public Task<ActionResult<PagedResult<ScoreboardEntryDto>>> GetIndividualLeaderboard(CancellationToken ct = default)
        => GetScoreboard(ct);

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
    private readonly ILogger<AdminCompetitionController> _logger;

    public AdminCompetitionController(ICompetitionService service, TenantContext tenant, ILogger<AdminCompetitionController> logger)
    {
        _service = service;
        _tenant = tenant;
        _logger = logger;
    }

    // ── Classement nominatif COMPLET (tous les membres, positions 1..N) — RÉSERVÉ ADMIN ──
    // Même garde-fou que la vue nominative des scénarios (AdminScenariosController). Non exposé aux membres.
    [HttpGet("leaderboard")]
    public async Task<ActionResult<AdminLeaderboardDto>> GetFullLeaderboard(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var adminId = User.GetUserId();
        var ranking = await _service.GetScoreboardAsync(tenantId, adminId, page, pageSize, ct);

        // Traçabilité RGPD : consigner l'accès à une donnée nominative sensible.
        _logger.LogInformation("[COMPETITION] Classement nominatif complet consulté. Tenant={TenantId} Admin={AdminId} page={Page}",
            tenantId, adminId, page);

        return Ok(new AdminLeaderboardDto(
            "Classement nominatif complet — réservé à l'administration (suivi pédagogique). Ces positions individuelles ne sont pas exposées aux membres.",
            ranking));
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
