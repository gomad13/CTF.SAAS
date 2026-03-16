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
    // ✅ ADMIN: renvoie tous les paths du tenant
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("admin");

        if (isAdmin)
        {
            var itemsAdmin = await _db.Paths
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId)
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

        // USER: seulement assignés
        var items = await _db.Assignments
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.UserId == userId)
            .Join(_db.Paths,
                a => a.PathId,
                p => p.Id,
                (a, p) => new { a, p })
            .Where(x => x.p.TenantId == tenantId)
            .OrderByDescending(x => x.a.AssignedAt)
            .Select(x => new
            {
                x.p.Id,
                x.p.Type,
                x.p.JobFamily,
                x.p.Title,
                x.p.Level,
                x.p.Status,
                x.p.Version,
                x.p.CreatedAt,
                x.p.PublishedAt,

                assignmentStatus = x.a.Status,
                x.a.DueAt,
                x.a.AssignedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/paths/{id}
    // ✅ ADMIN: accès à tout
    // ✅ USER: accès seulement si assigné
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne([FromRoute] Guid id)
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("admin");

        if (!isAdmin)
        {
            var assigned = await _db.Assignments
                .AsNoTracking()
                .AnyAsync(a => a.TenantId == tenantId && a.UserId == userId && a.PathId == id);

            if (!assigned)
                return Forbid();
        }

        var path = await _db.Paths
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Id == id)
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
            .Where(m => m.TenantId == tenantId && m.PathId == id)
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
            .Where(c => c.TenantId == tenantId && moduleIds.Contains(c.ModuleId))
            .OrderBy(c => c.CreatedAt)
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
