using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/paths")]
public class LearningPathsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public LearningPathsController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // GET /api/paths
    // ✅ USER: renvoie uniquement les paths assignés
    // ✅ ADMIN: renvoie tous les paths visibles par le tenant (privés + catalogue accordé)
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] CTF.Api.Services.ParcoursVisibilityService visibility)
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("admin");

        if (isAdmin)
        {
            var itemsAdmin = await visibility.VisibleFor(tenantId)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Type,
                    p.JobFamily,
                    p.Title,
                    p.Level,
                    p.Status,
                    p.Version,
                    p.CreatedAt,
                    p.PublishedAt
                })
                .ToListAsync();

            return Ok(itemsAdmin);
        }

        // USER: parcours visibles via règle centralisée
        var pathIds = await visibility.VisiblePathIdsForUserAsync(userId);
        if (pathIds.Count == 0) return Ok(Array.Empty<object>());

        var allPaths = await _db.Paths.AsNoTracking()
            .Where(p => pathIds.Contains(p.Id))
            .ToListAsync();

        var assignMap = await _db.Assignments.AsNoTracking()
            .Where(a => a.UserId == userId && pathIds.Contains(a.PathId))
            .ToDictionaryAsync(a => a.PathId, a => a);

        var items = allPaths.Select(p =>
        {
            assignMap.TryGetValue(p.Id, out var a);
            return new
            {
                p.Id,
                p.Type,
                p.JobFamily,
                p.Title,
                p.Level,
                p.Status,
                p.Version,
                p.CreatedAt,
                p.PublishedAt,
                assignmentStatus = a?.Status,
                dueAt = a?.DueAt,
                assignedAt = a?.AssignedAt
            };
        }).OrderByDescending(x => x.assignedAt ?? x.CreatedAt).ToList();

        return Ok(items);
    }

    // GET /api/paths/{id}
    // ✅ ADMIN: accès aux parcours visibles (privés du tenant OU catalogue accordé)
    // ✅ USER: accès seulement si assigné
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(
        [FromRoute] Guid id,
        [FromServices] CTF.Api.Services.ParcoursVisibilityService visibility)
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("admin");

        if (!isAdmin)
        {
            var visibleForUser = await visibility.VisiblePathIdsForUserAsync(userId);
            if (!visibleForUser.Contains(id))
                return Forbid();
        }
        else
        {
            var visible = await visibility.CanAccessAsync(tenantId, id);
            if (!visible)
                return NotFound();
        }

        var path = await _db.Paths
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Type,
                p.JobFamily,
                p.Title,
                p.Description,
                p.Level,
                p.Status,
                p.Version,
                p.CreatedAt,
                p.PublishedAt
            })
            .SingleOrDefaultAsync();

        if (path == null)
            return NotFound();

        var modules = await _db.Modules
            .AsNoTracking()
            .Where(m => m.PathId == id)
            .OrderBy(m => m.SortOrder)
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.SortOrder,
                m.CreatedAt
            })
            .ToListAsync();

        var moduleIds = modules.Select(m => m.Id).ToList();

        var challenges = await _db.Challenges
            .AsNoTracking()
            .Where(c => moduleIds.Contains(c.ModuleId))
            .OrderBy(c => c.SortOrder).ThenBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.ModuleId,
                c.Type,
                c.Title,
                c.Instructions,
                c.Difficulty,
                c.Points,
                c.Status,
                c.CreatedAt,
                c.PublishedAt
            })
            .ToListAsync();

        return Ok(new
        {
            path,
            modules = modules.Select(m => new
            {
                m.Id,
                m.Title,
                m.SortOrder,
                m.CreatedAt,
                challenges = challenges.Where(c => c.ModuleId == m.Id)
            })
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/paths/{pathId}/progress
    // Progression agrégée par path pour l'utilisateur courant.
    // Source unique de vérité : union(Submissions.IsCorrect, ChallengeCompletions).
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("{pathId:guid}/progress")]
    public async Task<IActionResult> GetProgress(
        [FromRoute] Guid pathId,
        [FromServices] CTF.Api.Services.ProgressCalculationService progressCalc)
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId = User.GetUserId();

        var r = await progressCalc.CalculateAsync(userId, pathId, tenantId);

        return Ok(new
        {
            pathId,
            totalChallenges = r.Total,
            completedChallengesCount = r.Completed,
            percent = r.Percent,
            status = r.Status,
            completedChallengeIds = r.CompletedChallengeIds,
            modules = r.Modules.Select(m => new
            {
                moduleId = m.ModuleId,
                title = m.Title,
                sortOrder = m.SortOrder,
                total = m.Total,
                completed = m.Completed,
                percent = m.Percent,
            }),
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/paths/{pathId}/next-challenge?afterChallengeId=...
    // Prochain challenge non complété dans l'ordre Modules.SortOrder → Challenges.SortOrder.
    // Renvoie { nextChallengeId, moduleId, isLastOfPath } ou 204 si parcours complet.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("{pathId:guid}/next-challenge")]
    public async Task<IActionResult> GetNextChallenge(
        [FromRoute] Guid pathId,
        [FromQuery] Guid? afterChallengeId)
    {
        var tenantId = _tenant.TenantId!.Value;
        var demoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");
        var userId = User.GetUserId();

        var modules = await _db.Modules
            .AsNoTracking()
            .Where(m => (m.TenantId == tenantId || m.TenantId == demoTenantId) && m.PathId == pathId)
            .OrderBy(m => m.SortOrder)
            .Select(m => new { m.Id, m.SortOrder })
            .ToListAsync();

        var moduleIds = modules.Select(m => m.Id).ToList();

        var ordered = await _db.Challenges
            .AsNoTracking()
            .Where(c => (c.TenantId == tenantId || c.TenantId == demoTenantId) && moduleIds.Contains(c.ModuleId))
            .Select(c => new { c.Id, c.ModuleId, c.SortOrder, c.CreatedAt })
            .ToListAsync();

        // Ordre global : SortOrder du module, puis SortOrder du challenge, puis CreatedAt
        var sorted = ordered
            .Select(c => new
            {
                c.Id,
                c.ModuleId,
                ModuleSort = modules.First(m => m.Id == c.ModuleId).SortOrder,
                c.SortOrder,
                c.CreatedAt,
            })
            .OrderBy(x => x.ModuleSort).ThenBy(x => x.SortOrder).ThenBy(x => x.CreatedAt)
            .ToList();

        var completedIds = new HashSet<Guid>();
        var subIds = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsCorrect && sorted.Select(x => x.Id).Contains(s.ChallengeId))
            .Select(s => s.ChallengeId)
            .Distinct()
            .ToListAsync();
        foreach (var id in subIds) completedIds.Add(id);

        var ccIds = await _db.ChallengeCompletions
            .AsNoTracking()
            .Where(cc => cc.UserId == userId && sorted.Select(x => x.Id).Contains(cc.ChallengeId))
            .Select(cc => cc.ChallengeId)
            .Distinct()
            .ToListAsync();
        foreach (var id in ccIds) completedIds.Add(id);

        // On part du challenge après `afterChallengeId` (si fourni), sinon depuis le début
        int startIndex = 0;
        if (afterChallengeId.HasValue)
        {
            var idx = sorted.FindIndex(x => x.Id == afterChallengeId.Value);
            if (idx >= 0) startIndex = idx + 1;
        }

        // Cherche le prochain non complété à partir de startIndex, puis en revenant au début si rien
        for (int offset = 0; offset < sorted.Count; offset++)
        {
            var i = (startIndex + offset) % sorted.Count;
            if (!completedIds.Contains(sorted[i].Id))
            {
                return Ok(new
                {
                    nextChallengeId = sorted[i].Id,
                    moduleId = sorted[i].ModuleId,
                    isLastOfPath = false,
                });
            }
        }

        // Tous complétés
        return Ok(new
        {
            nextChallengeId = (Guid?)null,
            moduleId = (Guid?)null,
            isLastOfPath = true,
        });
    }

    // DTO pour la création
    public sealed record CreateLearningPathRequest(
        string Type,
        string Title,
        string Status,
        string? JobFamily,
        string? Description,
        string? Level
    );

    // POST /api/paths  ✅ ADMIN ONLY
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLearningPathRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;
        var createdBy = User.GetUserId();

        var path = new LearningPath
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = req.Type,
            JobFamily = req.JobFamily,
            Title = req.Title,
            Description = req.Description,
            Level = req.Level,
            Status = req.Status,
            Version = 1,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.Paths.Add(path);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = path.Id }, new { path.Id });
    }

    // POST /api/paths/{id}/publish   ✅ ADMIN ONLY
    [Authorize(Roles = "admin")]
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish([FromRoute] Guid id)
    {
        var tenantId = _tenant.TenantId!.Value;

        var path = await _db.Paths
            .SingleOrDefaultAsync(p => p.TenantId == tenantId && p.Id == id);

        if (path == null)
            return NotFound();

        path.Status = "published";
        path.PublishedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { path.Id, path.Status, path.PublishedAt });
    }
}
