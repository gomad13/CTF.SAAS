using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTF.Api.Controllers;

/// <summary>
/// B4/B5 — Équipes côté UTILISATEUR (tout rôle). Isolation stricte sur le tenant du JWT :
/// un user ne voit/rejoint que les équipes de SON entreprise, et ne peut rejoindre qu'une
/// équipe OUVERTE. Le mode Équipes doit être activé pour le tenant.
/// </summary>
[ApiController]
[Route("api/user-teams")]
[Authorize]
public class UserTeamsController : ControllerBase
{
    private readonly ITeamsService _service;
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public UserTeamsController(ITeamsService service, AppDbContext db, TenantContext tenant)
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

    /// <summary>Liste des équipes de l'entreprise du user (avec IsOpen / IsMember / IsFull).</summary>
    [HttpGet]
    public async Task<IActionResult> Browse(CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        return Ok(await _service.BrowseTeamsForUserAsync(tenantId.Value, User.GetUserId(), ct));
    }

    /// <summary>Les équipes auxquelles le user appartient (nom, description, membres).</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        return Ok(await _service.GetMyTeamsAsync(tenantId.Value, User.GetUserId(), ct));
    }

    /// <summary>Rejoindre une équipe OUVERTE de son tenant (capacité respectée).</summary>
    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var res = await _service.JoinTeamAsync(tenantId.Value, User.GetUserId(), id, ct);
        return res.Success ? Ok(res) : Conflict(res);
    }

    /// <summary>Quitter une équipe dont on est membre.</summary>
    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
    {
        var tenantId = await TenantIdOrForbidden(ct);
        if (tenantId is null) return StatusCode(403, new { error = "Teams mode is not enabled." });
        var res = await _service.LeaveTeamAsync(tenantId.Value, User.GetUserId(), id, ct);
        return res.Success ? Ok(res) : Conflict(res);
    }
}
