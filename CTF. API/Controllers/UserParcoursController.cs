using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Endpoint user-facing : liste "Mes parcours" = union des visibilités selon règle centralisée.
/// Remplace progressivement /api/assignments/mine qui ne lit que Assignments.
/// </summary>
[ApiController]
[Route("api/user/parcours")]
[Authorize]
public class UserParcoursController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ParcoursVisibilityService _visibility;

    public UserParcoursController(AppDbContext db, ParcoursVisibilityService visibility)
    {
        _db = db;
        _visibility = visibility;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tenantId = User.FindFirst(System.Security.Claims.ClaimTypes.GroupSid)?.Value;

        var pathIds = await _visibility.VisiblePathIdsForUserAsync(userId, ct);
        if (pathIds.Count == 0) return Ok(Array.Empty<object>());

        var paths = await _db.Paths.AsNoTracking()
            .Where(p => pathIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Description, p.Level, p.Sector, p.EstimatedMinutes, p.Tags })
            .ToListAsync(ct);

        var progresses = await _db.Progresses.AsNoTracking()
            .Where(pr => pathIds.Contains(pr.PathId) && pr.UserId == userId)
            .ToDictionaryAsync(pr => pr.PathId, pr => new { pr.Status, pr.Percent, pr.StartedAt, pr.CompletedAt }, ct);

        var assignments = await _db.Assignments.AsNoTracking()
            .Where(a => a.UserId == userId && pathIds.Contains(a.PathId))
            .ToDictionaryAsync(a => a.PathId, a => new { a.Status, a.DueAt, a.AssignedAt }, ct);

        var mandatory = await _db.MandatoryAssignments.AsNoTracking()
            .Where(m => pathIds.Contains(m.PathId))
            .Select(m => new { m.PathId, m.Deadline })
            .ToListAsync(ct);
        var deadlines = mandatory.GroupBy(m => m.PathId)
            .ToDictionary(g => g.Key, g => (DateTime?)g.Min(x => x.Deadline));

        var result = paths.Select(p =>
        {
            progresses.TryGetValue(p.Id, out var prog);
            assignments.TryGetValue(p.Id, out var a);
            var hasMandatory = deadlines.TryGetValue(p.Id, out var due);
            return new
            {
                pathId = p.Id,
                title = p.Title,
                description = p.Description,
                level = p.Level,
                sector = p.Sector,
                estimatedMinutes = p.EstimatedMinutes,
                tags = p.Tags,
                progressStatus = prog?.Status ?? "not_started",
                progressPercent = prog?.Percent ?? 0,
                assignmentStatus = a?.Status,
                dueAt = a?.DueAt ?? due,
                isMandatory = hasMandatory
            };
        }).OrderByDescending(x => x.isMandatory).ThenBy(x => x.title).ToList();

        return Ok(result);
    }
}
