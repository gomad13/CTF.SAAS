using System.Security.Claims;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public AssignmentsController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // ----------------------------
    // Helpers
    // ----------------------------
    private Guid GetUserId()
    {
        var raw = User.FindFirstValue("user_id") ??
                  User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (raw is null || !Guid.TryParse(raw, out var userId))
            throw new InvalidOperationException("Missing or invalid user_id claim.");

        return userId;
    }

    private Guid GetTenantId()
    {
        if (_tenant.TenantId is null)
            throw new InvalidOperationException("Missing tenant context.");

        return _tenant.TenantId.Value;
    }

    // ----------------------------
    // USER: voir ses parcours assignés + progress
    // GET /api/assignments/mine
    // Retourne la liste union (visibility service) avec métadonnées assignement/progression
    // pour rétro-compatibilité avec l'ancien contrat.
    // ----------------------------
    [HttpGet("mine")]
    public async Task<IActionResult> Mine([FromServices] CTF.Api.Services.ParcoursVisibilityService visibility)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        var pathIds = await visibility.VisiblePathIdsForUserAsync(userId);
        if (pathIds.Count == 0) return Ok(Array.Empty<object>());

        var paths = await _db.Paths.AsNoTracking()
            .Where(p => pathIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Level })
            .ToListAsync();

        var progresses = await _db.Progresses.AsNoTracking()
            .Where(p => p.UserId == userId && pathIds.Contains(p.PathId))
            .ToDictionaryAsync(p => p.PathId, p => new { p.Status, p.Percent }, CancellationToken.None);

        var assigns = await _db.Assignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.UserId == userId && pathIds.Contains(a.PathId))
            .ToDictionaryAsync(a => a.PathId, a => a, CancellationToken.None);

        var items = paths.Select(p =>
        {
            assigns.TryGetValue(p.Id, out var a);
            progresses.TryGetValue(p.Id, out var pr);
            return new
            {
                PathId = p.Id,
                PathTitle = p.Title,
                PathLevel = p.Level,
                Status = a?.Status ?? Assignment.Statuses.Assigned,
                DueAt = a?.DueAt,
                AssignedAt = a?.AssignedAt,
                StartedAt = a?.StartedAt,
                CompletedAt = a?.CompletedAt,
                UpdatedAt = a?.UpdatedAt,
                ProgressStatus = pr?.Status ?? "not_started",
                ProgressPercent = pr?.Percent ?? 0
            };
        }).OrderByDescending(x => x.AssignedAt ?? DateTime.MinValue).ToList();

        return Ok(items);
    }

    // ----------------------------
    // USER: start un parcours
    // POST /api/assignments/{pathId}/start
    // ----------------------------
    [HttpPost("{pathId:guid}/start")]
    public async Task<IActionResult> Start(
        Guid pathId,
        [FromServices] CTF.Api.Services.ParcoursVisibilityService visibility)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();
        var now = DateTime.UtcNow;

        var assignment = await _db.Assignments
            .SingleOrDefaultAsync(a =>
                a.TenantId == tenantId &&
                a.UserId == userId &&
                a.PathId == pathId);

        if (assignment is null)
        {
            // Le user peut avoir accès au parcours via global/team/compliance sans Assignment propagée
            // (ex : compliance "all_users" qui n'écrit pas dans Assignments). On auto-provisionne
            // un Assignment si le parcours est visible.
            var visiblePathIds = await visibility.VisiblePathIdsForUserAsync(userId);
            if (!visiblePathIds.Contains(pathId))
                return NotFound(new { message = "Parcours non visible pour cet utilisateur." });

            assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                PathId = pathId,
                Status = Assignment.Statuses.Assigned,
                AssignedAt = now,
                UpdatedAt = now
            };
            _db.Assignments.Add(assignment);
        }

        if (assignment.Status == Assignment.Statuses.Completed ||
            assignment.Status == Assignment.Statuses.Started)
            return NoContent();

        if (assignment.Status != Assignment.Statuses.Assigned)
            return BadRequest(new { message = "Invalid assignment state." });

        assignment.Status = Assignment.Statuses.Started;
        assignment.StartedAt ??= now;
        assignment.UpdatedAt = now;

        var progress = await _db.Progresses
            .SingleOrDefaultAsync(p =>
                p.TenantId == tenantId &&
                p.UserId == userId &&
                p.PathId == pathId);

        if (progress is null)
        {
            _db.Progresses.Add(new Progress
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                PathId = pathId,
                Status = "in_progress",
                Percent = 0,
                StartedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            if (progress.Status == "not_started")
                progress.Status = "in_progress";

            progress.StartedAt ??= now;
            progress.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ----------------------------
    // USER: complete un parcours
    // POST /api/assignments/{pathId}/complete
    // ----------------------------
    [HttpPost("{pathId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid pathId)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();
        var now = DateTime.UtcNow;

        var assignment = await _db.Assignments
            .SingleOrDefaultAsync(a =>
                a.TenantId == tenantId &&
                a.UserId == userId &&
                a.PathId == pathId);

        if (assignment is null)
            return NotFound(new { message = "Assignment not found." });

        if (assignment.Status == Assignment.Statuses.Completed)
            return NoContent();

        if (assignment.Status != Assignment.Statuses.Started)
            return BadRequest(new { message = "Assignment must be started first." });

        var progress = await _db.Progresses
            .SingleOrDefaultAsync(p =>
                p.TenantId == tenantId &&
                p.UserId == userId &&
                p.PathId == pathId);

        if (progress is null || progress.Percent < 100)
            return BadRequest(new { message = "Progress must be 100%." });

        assignment.Status = Assignment.Statuses.Completed;
        assignment.CompletedAt ??= now;
        assignment.UpdatedAt = now;

        progress.Status = "completed";
        progress.CompletedAt ??= now;
        progress.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ----------------------------
    // ADMIN: assigner un parcours
    // POST /api/assignments/assign
    // ----------------------------
    public sealed class AssignRequest
    {
        public Guid UserId { get; set; }
        public Guid PathId { get; set; }
        public DateTime? DueAt { get; set; }
    }

    [HttpPost("assign")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Assign(
        [FromBody] AssignRequest req,
        [FromServices] CTF.Api.Services.ParcoursVisibilityService visibility)
    {
        var tenantId = GetTenantId();
        var adminId = GetUserId();
        var now = DateTime.UtcNow;

        var userExists = await _db.Users
            .AnyAsync(u => u.TenantId == tenantId && u.Id == req.UserId);

        if (!userExists)
            return BadRequest(new { message = "User not found." });

        var visible = await visibility.CanAccessAsync(tenantId, req.PathId);
        if (!visible)
            return BadRequest(new { message = "Path not accessible for your tenant." });

        var assignment = await _db.Assignments
            .SingleOrDefaultAsync(a =>
                a.TenantId == tenantId &&
                a.UserId == req.UserId &&
                a.PathId == req.PathId);

        if (assignment is null)
        {
            _db.Assignments.Add(new Assignment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = req.UserId,
                PathId = req.PathId,
                Status = Assignment.Statuses.Assigned,
                DueAt = req.DueAt,
                AssignedBy = adminId,
                AssignedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            assignment.DueAt = req.DueAt;
            assignment.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
