using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/admin/teams")]
[Authorize(Roles = "admin,SuperAdmin")]
public class AdminTeamsController : ControllerBase
{
    private readonly ITeamsService _service;
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public AdminTeamsController(ITeamsService service, AppDbContext db, TenantContext tenant)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
    }

    private async Task<Guid?> TenantIdOrForbidden(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return null;
        if (!await ModeToggleHelper.IsEnabledAsync(_db, tenantId, ModeToggleHelper.Mode.Teams, ct)) return null;
        return tenantId;
    }

    // ── CRUD ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        return Ok(await _service.GetAllAsync(tenantId.Value, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var dto = await _service.GetByIdAsync(tenantId.Value, id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamDto req, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        return Ok(await _service.CreateAsync(tenantId.Value, req, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamDto req, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var dto = await _service.UpdateAsync(tenantId.Value, id, req, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var ok = await _service.DeleteAsync(tenantId.Value, id, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignLegacy([FromBody] AssignUserToTeamDto req, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var ok = await _service.AssignUserAsync(tenantId.Value, req, ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Membres ───────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        return Ok(await _service.GetMembersAsync(tenantId.Value, id, ct));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMembers(Guid id, [FromBody] AddTeamMembersDto req, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var result = await _service.AddMembersAsync(tenantId.Value, id, req.UserIds, ct);
        // M4 : équipe pleine et rien n'a pu être ajouté → 409 Conflict avec message clair.
        if (result.Added == 0 && result.Rejected > 0)
            return Conflict(result);
        return Ok(result);
    }

    // M4 — utilisateurs du tenant sans équipe (à affecter à l'arrivée)
    [HttpGet("unassigned")]
    public async Task<IActionResult> GetUnassigned(CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        return Ok(await _service.GetUnassignedUsersAsync(tenantId.Value, ct));
    }

    // Candidats à l'ajout dans CETTE équipe : users du tenant de l'équipe pas déjà membres.
    // Source unique = TenantId de l'équipe → corrige le « Aucun utilisateur disponible »
    // dû à un tenant divergent côté liste des utilisateurs.
    [HttpGet("{id:guid}/candidates")]
    public async Task<IActionResult> GetCandidates(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        return Ok(await _service.GetCandidateUsersAsync(tenantId.Value, id, ct));
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var ok = await _service.RemoveMemberAsync(tenantId.Value, id, userId, ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Parcours assignés ────────────────────────────────────────────────

    [HttpGet("{id:guid}/parcours")]
    public async Task<IActionResult> GetParcours(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        return Ok(await _service.GetParcoursAsync(tenantId.Value, id, ct));
    }

    [HttpPost("{id:guid}/parcours")]
    public async Task<IActionResult> AssignParcours(Guid id, [FromBody] AssignParcoursToTeamDto req, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var assignedBy = User.GetUserId();
        var dto = await _service.AssignParcoursAsync(tenantId.Value, id, req, assignedBy, ct);
        return dto is null ? BadRequest(new { error = "Parcours introuvable, équipe introuvable ou déjà assigné." }) : Ok(dto);
    }

    [HttpPut("{id:guid}/parcours/{pathId:guid}")]
    public async Task<IActionResult> UpdateParcours(Guid id, Guid pathId, [FromBody] UpdateTeamParcoursDto req, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var dto = await _service.UpdateParcoursAsync(tenantId.Value, id, pathId, req, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}/parcours/{pathId:guid}")]
    public async Task<IActionResult> RemoveParcours(Guid id, Guid pathId, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var ok = await _service.RemoveParcoursAsync(tenantId.Value, id, pathId, ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Stats ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/stats")]
    public async Task<IActionResult> GetStats(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var dto = await _service.GetStatsAsync(tenantId.Value, id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}

/// <summary>
/// Endpoint user-facing : parcours assignés à l'équipe du user courant.
/// </summary>
[ApiController]
[Route("api/user")]
[Authorize]
public class UserTeamParcoursController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public UserTeamParcoursController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("team-parcours")]
    public async Task<IActionResult> GetTeamParcours(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var userId = User.GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || user.TeamId is null) return Ok(Array.Empty<object>());

        var teamId = user.TeamId.Value;
        var assignments = await _db.TeamParcoursAssignments.AsNoTracking()
            .Where(a => a.TeamId == teamId && a.TenantId == tenantId)
            .ToListAsync(ct);
        if (assignments.Count == 0) return Ok(Array.Empty<object>());

        var team = await _db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId, ct);
        var pathIds = assignments.Select(a => a.PathId).ToList();
        var paths = await _db.Paths.AsNoTracking()
            .Where(p => pathIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Level })
            .ToListAsync(ct);

        var progresses = await _db.Progresses.AsNoTracking()
            .Where(p => p.UserId == userId && pathIds.Contains(p.PathId))
            .ToDictionaryAsync(p => p.PathId, p => new { p.Percent, p.Status }, ct);

        var result = assignments.Select(a =>
        {
            var p = paths.FirstOrDefault(pp => pp.Id == a.PathId);
            progresses.TryGetValue(a.PathId, out var prog);
            return new
            {
                pathId = a.PathId,
                pathTitle = p?.Title ?? "(parcours supprimé)",
                pathLevel = p?.Level,
                teamId,
                teamName = team?.Name,
                deadline = a.Deadline,
                isMandatory = a.IsMandatory,
                progressPercent = prog?.Percent ?? 0,
                progressStatus = prog?.Status ?? "not_started",
            };
        });

        return Ok(result);
    }
}
