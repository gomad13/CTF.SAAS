using System.Security.Claims;
using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Gestion du catalogue de parcours global (SuperAdmin uniquement).
/// Couvre : CRUD basique des parcours catalogue, attribution/révocation d'accès par tenant,
/// vue inverse (parcours catalogue accessibles par un tenant donné).
/// </summary>
[ApiController]
[Route("api/superadmin/catalog")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminCatalogController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<SuperAdminCatalogController> _logger;

    public SuperAdminCatalogController(AppDbContext db, ILogger<SuperAdminCatalogController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string CurrentEmail()
        => User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

    private string CurrentIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private async Task LogAsync(string action, string description, string severity = "info")
    {
        _db.SuperAdminAuditLogs.Add(new SuperAdminAuditLog
        {
            Action = action,
            Description = description,
            PerformedBy = CurrentEmail(),
            IpAddress = CurrentIp(),
            Severity = severity,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    // ── A. CRUD parcours catalogue ───────────────────────────────────────────

    [HttpGet("parcours")]
    public async Task<IActionResult> ListCatalogPaths(
        [FromQuery] string? sector = null,
        [FromQuery] string? level = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var q = _db.Paths.AsNoTracking().Where(p => p.IsCatalog);
        if (!string.IsNullOrWhiteSpace(sector)) q = q.Where(p => p.Sector == sector);
        if (!string.IsNullOrWhiteSpace(level)) q = q.Where(p => p.Level == level);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(p => p.Status == status);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderBy(p => p.Sector).ThenBy(p => p.Level).ThenBy(p => p.Title)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var pathIds = rows.Select(p => p.Id).ToList();

        var moduleMap = await _db.Modules.AsNoTracking()
            .Where(m => pathIds.Contains(m.PathId))
            .Select(m => new { m.Id, m.PathId })
            .ToListAsync(ct);
        var moduleToPath = moduleMap.ToDictionary(m => m.Id, m => m.PathId);
        var moduleIds = moduleMap.Select(m => m.Id).ToList();
        var challengeModuleIds = await _db.Challenges.AsNoTracking()
            .Where(c => moduleIds.Contains(c.ModuleId))
            .Select(c => c.ModuleId)
            .ToListAsync(ct);
        var countPerPath = pathIds.ToDictionary(
            pid => pid,
            pid => challengeModuleIds.Count(mid => moduleToPath.TryGetValue(mid, out var p) && p == pid)
        );

        var accessCounts = await _db.TenantParcoursAccesses.AsNoTracking()
            .Where(a => pathIds.Contains(a.PathId) && a.RevokedAt == null)
            .GroupBy(a => a.PathId)
            .Select(g => new { PathId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var accessMap = accessCounts.ToDictionary(x => x.PathId, x => x.Count);

        var items = rows.Select(p => new CatalogPathListItemDto(
            p.Id, p.Title, p.Description, p.Level, p.Sector, p.Status,
            p.EstimatedMinutes, p.Tags,
            countPerPath.GetValueOrDefault(p.Id, 0),
            accessMap.GetValueOrDefault(p.Id, 0),
            p.CreatedAt, p.PublishedAt
        )).ToList();

        return Ok(new PagedResult<CatalogPathListItemDto>(items, page, pageSize, total));
    }

    [HttpGet("parcours/{id:guid}")]
    public async Task<IActionResult> GetCatalogPath(Guid id, CancellationToken ct)
    {
        var path = await _db.Paths.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.IsCatalog, ct);
        if (path is null) return NotFound();

        var modules = await _db.Modules.AsNoTracking()
            .Where(m => m.PathId == id)
            .OrderBy(m => m.SortOrder)
            .Select(m => new { m.Id, m.Title, m.SortOrder })
            .ToListAsync(ct);

        var moduleIds = modules.Select(m => m.Id).ToList();
        var allChallenges = await _db.Challenges.AsNoTracking()
            .Where(c => moduleIds.Contains(c.ModuleId))
            .OrderBy(c => c.ModuleId).ThenBy(c => c.SortOrder)
            .ToListAsync(ct);
        var byModule = allChallenges
            .GroupBy(c => c.ModuleId)
            .ToDictionary(g => g.Key, g => g.Select(c => new CatalogChallengeDto(
                c.Id, c.Title, c.Type, c.ContentType, c.Category, c.Difficulty, c.Points, c.SortOrder
            )).ToList());

        var dto = new CatalogPathDetailDto(
            path.Id, path.Title, path.Description, path.Level, path.Sector,
            path.Status, path.EstimatedMinutes, path.Tags, path.IsCatalog,
            path.CreatedAt, path.PublishedAt,
            modules.Select(m => new CatalogModuleDto(
                m.Id, m.Title, m.SortOrder,
                byModule.GetValueOrDefault(m.Id, new List<CatalogChallengeDto>())
            )).ToList()
        );
        return Ok(dto);
    }

    [HttpPost("parcours")]
    public async Task<IActionResult> CreateCatalogPath([FromBody] CreateCatalogPathDto req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest(new { error = "Title is required." });
        var validLevels = new[] { "beginner", "intermediate", "advanced" };
        if (!validLevels.Contains(req.Level)) return BadRequest(new { error = "Invalid level." });

        var now = DateTime.UtcNow;
        var path = new LearningPath
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Type = "catalog",
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            Level = req.Level,
            Status = "draft",
            Version = 1,
            IsCatalog = true,
            Sector = req.Sector?.Trim(),
            EstimatedMinutes = req.EstimatedMinutes,
            Tags = req.Tags?.Trim(),
            CreatedBy = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            CreatedAt = now,
            PublishedAt = null
        };
        _db.Paths.Add(path);
        await _db.SaveChangesAsync(ct);
        await LogAsync("catalog.path.create", $"Created catalog path '{path.Title}' ({path.Id}) sector={path.Sector}");
        return Ok(new { id = path.Id });
    }

    [HttpPut("parcours/{id:guid}")]
    public async Task<IActionResult> UpdateCatalogPath(Guid id, [FromBody] UpdateCatalogPathDto req, CancellationToken ct)
    {
        var path = await _db.Paths.FirstOrDefaultAsync(p => p.Id == id && p.IsCatalog, ct);
        if (path is null) return NotFound();

        path.Title = req.Title.Trim();
        path.Description = req.Description?.Trim();
        if (!string.IsNullOrWhiteSpace(req.Level)) path.Level = req.Level;
        if (req.Sector is not null) path.Sector = req.Sector.Trim();
        if (req.EstimatedMinutes.HasValue) path.EstimatedMinutes = req.EstimatedMinutes;
        if (req.Tags is not null) path.Tags = req.Tags.Trim();
        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            var validStatus = new[] { "draft", "published", "archived" };
            if (!validStatus.Contains(req.Status)) return BadRequest(new { error = "Invalid status." });
            path.Status = req.Status;
            if (req.Status == "published" && path.PublishedAt is null)
                path.PublishedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        await LogAsync("catalog.path.update", $"Updated catalog path '{path.Title}' ({path.Id})");
        return NoContent();
    }

    [HttpDelete("parcours/{id:guid}")]
    public async Task<IActionResult> DeactivateCatalogPath(Guid id, CancellationToken ct)
    {
        var path = await _db.Paths.FirstOrDefaultAsync(p => p.Id == id && p.IsCatalog, ct);
        if (path is null) return NotFound();
        path.Status = "archived";
        await _db.SaveChangesAsync(ct);
        await LogAsync("catalog.path.archive", $"Archived catalog path '{path.Title}' ({path.Id})", "warning");
        return NoContent();
    }

    // ── B. Gestion d'accès par parcours ──────────────────────────────────────

    [HttpGet("parcours/{id:guid}/access")]
    public async Task<IActionResult> GetPathAccess(Guid id, CancellationToken ct)
    {
        var pathExists = await _db.Paths.AsNoTracking().AnyAsync(p => p.Id == id && p.IsCatalog, ct);
        if (!pathExists) return NotFound();

        var tenants = await _db.Tenants.AsNoTracking()
            .Where(t => t.IsActive && t.Id != Guid.Empty)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);

        var accesses = await _db.TenantParcoursAccesses.AsNoTracking()
            .Where(a => a.PathId == id)
            .ToListAsync(ct);

        var userIds = accesses.SelectMany(a => new[] { a.GrantedBy, a.RevokedBy ?? Guid.Empty })
            .Where(g => g != Guid.Empty).Distinct().ToList();
        var userEmails = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        var result = tenants.Select(t =>
        {
            var a = accesses.FirstOrDefault(x => x.TenantId == t.Id);
            var hasAccess = a is not null && a.RevokedAt == null;
            return new CatalogAccessTenantDto(
                t.Id, t.Name, hasAccess,
                a?.GrantedAt,
                a is not null && userEmails.TryGetValue(a.GrantedBy, out var g) ? g : null,
                a?.RevokedAt,
                a?.RevokedBy is Guid rb && userEmails.TryGetValue(rb, out var r) ? r : null
            );
        }).OrderBy(x => x.TenantName).ToList();

        return Ok(result);
    }

    [HttpPost("parcours/{id:guid}/access")]
    public async Task<IActionResult> GrantAccess(Guid id, [FromBody] GrantAccessRequestDto req, CancellationToken ct)
    {
        var path = await _db.Paths.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (path is null) return NotFound();
        if (!path.IsCatalog) return BadRequest(new { error = "Only catalog paths can have tenant access granted." });

        if (req.TenantIds is null || req.TenantIds.Count == 0) return BadRequest(new { error = "No tenantIds provided." });

        var now = DateTime.UtcNow;
        var superAdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var granted = 0;

        foreach (var tenantId in req.TenantIds.Distinct())
        {
            if (tenantId == Guid.Empty) continue;
            var tenantExists = await _db.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenantId, ct);
            if (!tenantExists) continue;

            var existing = await _db.TenantParcoursAccesses
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PathId == id, ct);
            if (existing is null)
            {
                _db.TenantParcoursAccesses.Add(new TenantParcoursAccess
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PathId = id,
                    GrantedAt = now,
                    GrantedBy = superAdminUserId,
                    RevokedAt = null,
                    RevokedBy = null
                });
                granted++;
            }
            else if (existing.RevokedAt is not null)
            {
                existing.RevokedAt = null;
                existing.RevokedBy = null;
                existing.GrantedAt = now;
                existing.GrantedBy = superAdminUserId;
                granted++;
            }
        }
        await _db.SaveChangesAsync(ct);
        await LogAsync("catalog.access.grant", $"Granted path {id} to {granted} tenant(s)");
        return Ok(new { granted });
    }

    [HttpDelete("parcours/{id:guid}/access/{tenantId:guid}")]
    public async Task<IActionResult> RevokeAccess(Guid id, Guid tenantId, CancellationToken ct)
    {
        var access = await _db.TenantParcoursAccesses
            .FirstOrDefaultAsync(a => a.PathId == id && a.TenantId == tenantId, ct);
        if (access is null || access.RevokedAt is not null) return NotFound();

        access.RevokedAt = DateTime.UtcNow;
        access.RevokedBy = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await _db.SaveChangesAsync(ct);
        await LogAsync("catalog.access.revoke", $"Revoked path {id} from tenant {tenantId}", "warning");
        return NoContent();
    }

    // ── C. Vue inverse : parcours catalogue pour un tenant donné ─────────────

    [HttpGet("/api/superadmin/tenants/{tenantId:guid}/catalog")]
    public async Task<IActionResult> GetTenantCatalog(Guid tenantId, CancellationToken ct)
    {
        var tenantExists = await _db.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenantId, ct);
        if (!tenantExists) return NotFound();

        var paths = await _db.Paths.AsNoTracking()
            .Where(p => p.IsCatalog && p.Status != "archived")
            .OrderBy(p => p.Sector).ThenBy(p => p.Title)
            .ToListAsync(ct);

        var accesses = await _db.TenantParcoursAccesses.AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .ToListAsync(ct);

        var result = paths.Select(p =>
        {
            var a = accesses.FirstOrDefault(x => x.PathId == p.Id);
            var hasAccess = a is not null && a.RevokedAt == null;
            return new TenantCatalogAccessDto(
                p.Id, p.Title, p.Sector, p.Level, p.EstimatedMinutes,
                hasAccess,
                hasAccess ? a!.GrantedAt : null
            );
        }).ToList();
        return Ok(result);
    }

    [HttpPost("/api/superadmin/tenants/{tenantId:guid}/catalog")]
    public async Task<IActionResult> GrantPathsToTenant(Guid tenantId, [FromBody] GrantPathsToTenantDto req, CancellationToken ct)
    {
        var tenantExists = await _db.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenantId, ct);
        if (!tenantExists) return NotFound();

        var now = DateTime.UtcNow;
        var superAdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var granted = 0;

        foreach (var pathId in req.PathIds.Distinct())
        {
            var path = await _db.Paths.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pathId, ct);
            if (path is null || !path.IsCatalog) continue;

            var existing = await _db.TenantParcoursAccesses
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PathId == pathId, ct);
            if (existing is null)
            {
                _db.TenantParcoursAccesses.Add(new TenantParcoursAccess
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PathId = pathId,
                    GrantedAt = now,
                    GrantedBy = superAdminUserId
                });
                granted++;
            }
            else if (existing.RevokedAt is not null)
            {
                existing.RevokedAt = null;
                existing.RevokedBy = null;
                existing.GrantedAt = now;
                existing.GrantedBy = superAdminUserId;
                granted++;
            }
        }
        await _db.SaveChangesAsync(ct);
        await LogAsync("catalog.access.grant-tenant", $"Granted {granted} catalog path(s) to tenant {tenantId}");
        return Ok(new { granted });
    }

    // ── Batch matrix ────────────────────────────────────────────────────────
    public record BatchEntry(Guid TenantId, Guid ParcoursId);
    public record BatchRequest(List<BatchEntry> Grants, List<BatchEntry> Revokes);

    [HttpPost("assignments/batch")]
    public async Task<IActionResult> BatchAssignments([FromBody] BatchRequest req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var sa = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var grantedCount = 0;
        var revokedCount = 0;
        var errors = new List<object>();

        // Pre-filter valid catalog paths to avoid DB round-trips per iteration
        var allIds = (req?.Grants ?? new()).Select(e => e.ParcoursId)
            .Concat((req?.Revokes ?? new()).Select(e => e.ParcoursId)).Distinct().ToList();
        var catalogPathIds = await _db.Paths.AsNoTracking()
            .Where(p => allIds.Contains(p.Id) && p.IsCatalog)
            .Select(p => p.Id).ToListAsync(ct);

        foreach (var g in req?.Grants ?? new())
        {
            if (g.TenantId == Guid.Empty) continue;
            if (!catalogPathIds.Contains(g.ParcoursId))
            {
                errors.Add(new { g.TenantId, g.ParcoursId, error = "not_catalog_path" });
                continue;
            }
            var existing = await _db.TenantParcoursAccesses
                .FirstOrDefaultAsync(a => a.TenantId == g.TenantId && a.PathId == g.ParcoursId, ct);
            if (existing is null)
            {
                _db.TenantParcoursAccesses.Add(new TenantParcoursAccess
                {
                    Id = Guid.NewGuid(),
                    TenantId = g.TenantId,
                    PathId = g.ParcoursId,
                    GrantedAt = now,
                    GrantedBy = sa
                });
                grantedCount++;
            }
            else if (existing.RevokedAt is not null)
            {
                existing.RevokedAt = null;
                existing.RevokedBy = null;
                existing.GrantedAt = now;
                existing.GrantedBy = sa;
                grantedCount++;
            }
        }

        foreach (var r in req?.Revokes ?? new())
        {
            var existing = await _db.TenantParcoursAccesses
                .FirstOrDefaultAsync(a => a.TenantId == r.TenantId && a.PathId == r.ParcoursId, ct);
            if (existing is null || existing.RevokedAt is not null) continue;
            existing.RevokedAt = now;
            existing.RevokedBy = sa;
            revokedCount++;
        }

        if (grantedCount > 0 || revokedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            await LogAsync("catalog.access.batch",
                $"Batch: granted={grantedCount}, revoked={revokedCount}");
        }

        return Ok(new { granted = grantedCount, revoked = revokedCount, errors });
    }

    [HttpDelete("/api/superadmin/tenants/{tenantId:guid}/catalog/{pathId:guid}")]
    public async Task<IActionResult> RevokePathFromTenant(Guid tenantId, Guid pathId, CancellationToken ct)
    {
        var access = await _db.TenantParcoursAccesses
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PathId == pathId, ct);
        if (access is null || access.RevokedAt is not null) return NotFound();

        access.RevokedAt = DateTime.UtcNow;
        access.RevokedBy = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await _db.SaveChangesAsync(ct);
        await LogAsync("catalog.access.revoke-tenant", $"Revoked path {pathId} from tenant {tenantId}", "warning");
        return NoContent();
    }

    // ── D. Stats d'un parcours catalogue ─────────────────────────────────────

    [HttpGet("parcours/{id:guid}/stats")]
    public async Task<IActionResult> GetPathStats(Guid id, CancellationToken ct)
    {
        var pathExists = await _db.Paths.AsNoTracking().AnyAsync(p => p.Id == id && p.IsCatalog, ct);
        if (!pathExists) return NotFound();

        var accessTenantIds = await _db.TenantParcoursAccesses.AsNoTracking()
            .Where(a => a.PathId == id && a.RevokedAt == null)
            .Select(a => a.TenantId)
            .ToListAsync(ct);

        var totalUsers = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && accessTenantIds.Contains(u.TenantId))
            .CountAsync(ct);

        var progresses = await _db.Progresses.AsNoTracking()
            .Where(pr => pr.PathId == id && accessTenantIds.Contains(pr.TenantId))
            .Select(pr => new { pr.UserId, pr.Status, pr.Percent })
            .ToListAsync(ct);

        var started = progresses.Count(p => p.Status != "not_started" && p.Percent > 0);
        var completed = progresses.Count(p => p.Status == "completed" || p.Percent >= 100);
        var avg = progresses.Count > 0 ? progresses.Average(p => (double)p.Percent) : 0.0;

        return Ok(new CatalogPathStatsDto(
            id, accessTenantIds.Count, totalUsers, started, completed, Math.Round(avg, 1)
        ));
    }
}
