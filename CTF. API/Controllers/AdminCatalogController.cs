using System.Security.Claims;
using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Catalogue self-service côté Admin tenant : voir les parcours accordés,
/// activer/désactiver pour les users du tenant, choisir scope (global vs teams_only).
/// </summary>
[ApiController]
[Route("api/admin/catalog")]
[Authorize(Roles = "admin,SuperAdmin")]
public class AdminCatalogController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly ParcoursVisibilityService _visibility;

    public AdminCatalogController(AppDbContext db, TenantContext tenant, ParcoursVisibilityService visibility)
    {
        _db = db;
        _tenant = tenant;
        _visibility = visibility;
    }

    private Guid CurrentTenantId() => _tenant.TenantId ?? Guid.Empty;
    private Guid CurrentUserId() => User.GetUserId();

    // ── GET /api/admin/catalog ───────────────────────────────────────────────
    // Retourne TOUS les parcours catalogue + statut pour mon tenant.
    [HttpGet]
    public async Task<IActionResult> GetAdminCatalog(CancellationToken ct)
    {
        var tenantId = CurrentTenantId();
        if (tenantId == Guid.Empty) return Unauthorized();

        var paths = await _db.Paths.AsNoTracking()
            .Where(p => p.IsCatalog && p.Status != "archived")
            .OrderBy(p => p.Sector).ThenBy(p => p.Level).ThenBy(p => p.Title)
            .ToListAsync(ct);

        var pathIds = paths.Select(p => p.Id).ToList();

        var grants = await _db.TenantParcoursAccesses.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.RevokedAt == null && pathIds.Contains(a.PathId))
            .Select(a => a.PathId).ToListAsync(ct);

        var activations = await _db.TenantParcoursAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.DeactivatedAt == null && pathIds.Contains(a.PathId))
            .ToDictionaryAsync(a => a.PathId, a => a.Scope, ct);

        var teamUses = await _db.TeamParcoursAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && pathIds.Contains(a.PathId))
            .GroupBy(a => a.PathId)
            .Select(g => new { PathId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PathId, x => x.Count, ct);

        var moduleMap = await _db.Modules.AsNoTracking()
            .Where(m => pathIds.Contains(m.PathId))
            .Select(m => new { m.Id, m.PathId })
            .ToListAsync(ct);
        var moduleIds = moduleMap.Select(m => m.Id).ToList();
        var challengeModuleIds = await _db.Challenges.AsNoTracking()
            .Where(c => moduleIds.Contains(c.ModuleId))
            .Select(c => c.ModuleId).ToListAsync(ct);

        var items = paths.Select(p =>
        {
            var isGranted = grants.Contains(p.Id);
            var isActivated = activations.ContainsKey(p.Id);
            string status;
            if (!isGranted) status = "not_granted";
            else if (!isActivated) status = "granted_inactive";
            else status = activations[p.Id] == "global" ? "activated_global" : "activated_teams_only";

            var challenges = challengeModuleIds.Count(mid => moduleMap.Any(m => m.Id == mid && m.PathId == p.Id));
            return new AdminCatalogItemDto(
                p.Id, p.Title, p.Description, p.Level, p.Sector, p.EstimatedMinutes, p.Tags,
                challenges, status, teamUses.GetValueOrDefault(p.Id, 0)
            );
        }).ToList();

        return Ok(items);
    }

    // ── POST /api/admin/catalog/{id}/activate ────────────────────────────────
    [HttpPost("{pathId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid pathId, [FromBody] ActivatePathDto req, CancellationToken ct)
    {
        var tenantId = CurrentTenantId();
        if (tenantId == Guid.Empty) return Unauthorized();

        var path = await _db.Paths.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pathId, ct);
        if (path is null) return NotFound();
        if (!path.IsCatalog) return BadRequest(new { error = "Not a catalog path." });

        var granted = await _db.TenantParcoursAccesses.AsNoTracking()
            .AnyAsync(a => a.TenantId == tenantId && a.PathId == pathId && a.RevokedAt == null, ct);
        if (!granted) return Conflict(new { error = "Path not granted to your tenant." });

        var scope = (req?.Mode ?? "global").ToLowerInvariant();
        if (scope != "global" && scope != "teams_only")
            return BadRequest(new { error = "mode must be 'global' or 'teams_only'." });

        var now = DateTime.UtcNow;
        var userId = CurrentUserId();

        var existing = await _db.TenantParcoursAssignments
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PathId == pathId, ct);
        if (existing is null)
        {
            _db.TenantParcoursAssignments.Add(new TenantParcoursAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PathId = pathId,
                Scope = scope,
                ActivatedAt = now,
                ActivatedBy = userId
            });
        }
        else
        {
            existing.Scope = scope;
            existing.DeactivatedAt = null;
            existing.DeactivatedBy = null;
            existing.ActivatedAt = now;
            existing.ActivatedBy = userId;
        }
        await _db.SaveChangesAsync(ct);

        // Si scope=global, propager via Assignments pour rétro-compat /api/assignments/mine
        if (scope == "global")
            await PropagateToAllUsersAsync(tenantId, pathId, userId, now, ct);

        return Ok(new { status = "ok", mode = scope });
    }

    // ── POST /api/admin/catalog/{id}/deactivate ──────────────────────────────
    [HttpPost("{pathId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid pathId, CancellationToken ct)
    {
        var tenantId = CurrentTenantId();
        if (tenantId == Guid.Empty) return Unauthorized();
        var userId = CurrentUserId();

        var existing = await _db.TenantParcoursAssignments
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PathId == pathId, ct);
        if (existing is null || existing.DeactivatedAt is not null) return NotFound();

        existing.DeactivatedAt = DateTime.UtcNow;
        existing.DeactivatedBy = userId;

        // Retire aussi les Assignments propagés (mais conserve Progresses pour reprise ultérieure)
        var propagated = await _db.Assignments
            .Where(a => a.TenantId == tenantId && a.PathId == pathId)
            .ToListAsync(ct);
        _db.Assignments.RemoveRange(propagated);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── GET /api/admin/parcours/available & /assigned ────────────────────────
    // Routes absolues pour respecter la nomenclature du prompt
    [HttpGet("/api/admin/parcours/available")]
    public async Task<IActionResult> Available(CancellationToken ct)
    {
        var tenantId = CurrentTenantId();
        if (tenantId == Guid.Empty) return Unauthorized();

        var availableIds = await _visibility.AvailableToActivateAsync(tenantId, ct);
        var paths = await _db.Paths.AsNoTracking()
            .Where(p => availableIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Sector, p.Level, p.EstimatedMinutes })
            .ToListAsync(ct);
        return Ok(paths);
    }

    [HttpGet("/api/admin/parcours/assigned")]
    public async Task<IActionResult> Assigned(CancellationToken ct)
    {
        var tenantId = CurrentTenantId();
        if (tenantId == Guid.Empty) return Unauthorized();

        var assigns = await _visibility.ActivatedByAdminAsync(tenantId, ct);
        var assignIds = assigns.Select(a => a.PathId).ToList();
        var paths = await _db.Paths.AsNoTracking()
            .Where(p => assignIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        var items = assigns.Select(a =>
        {
            var p = paths.GetValueOrDefault(a.PathId);
            return new
            {
                a.PathId,
                Title = p?.Title,
                p?.Sector,
                p?.Level,
                a.Scope,
                a.ActivatedAt,
                a.ActivatedBy
            };
        });
        return Ok(items);
    }

    // ── GET /api/admin/catalog/{pathId}/fiche — vitrine STATIQUE lecture seule ──
    // Contenu réel du parcours pour la fiche immersive (modules, thèmes, exemple). Aucune réponse exposée.
    [HttpGet("{pathId:guid}/fiche")]
    public async Task<IActionResult> GetFiche(Guid pathId, CancellationToken ct)
    {
        if (CurrentTenantId() == Guid.Empty) return Unauthorized();

        var path = await _db.Paths.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pathId && p.IsCatalog && p.Status != "archived", ct);
        if (path is null) return NotFound();

        var modules = await _db.Modules.AsNoTracking()
            .Where(m => m.PathId == pathId).OrderBy(m => m.SortOrder)
            .Select(m => new { m.Id, m.Title, m.SortOrder }).ToListAsync(ct);
        var moduleIds = modules.Select(m => m.Id).ToList();

        var challenges = await _db.Challenges.AsNoTracking()
            .Where(c => moduleIds.Contains(c.ModuleId) && c.Status == "published")
            .Select(c => new { c.ModuleId, c.Title, c.Instructions, c.InstructionTitle, c.InstructionBody, c.InstructionShortReminder, c.Category, c.SortOrder })
            .ToListAsync(ct);

        var moduleDtos = modules
            .Select(m => new CatalogFicheModuleDto(m.Title, challenges.Count(c => c.ModuleId == m.Id)))
            .ToList();

        var themes = challenges
            .Where(c => !string.IsNullOrWhiteSpace(c.Category))
            .GroupBy(c => c.Category!.Trim().ToLowerInvariant())
            .Select(g => g.First().Category!.Trim())
            .Take(12).ToList();

        var order = modules.ToDictionary(m => m.Id, m => m.SortOrder);
        var example = challenges
            .OrderBy(c => order.GetValueOrDefault(c.ModuleId, 0)).ThenBy(c => c.SortOrder)
            .Select(c => new CatalogFicheExampleDto(
                string.IsNullOrWhiteSpace(c.InstructionTitle) ? c.Title : c.InstructionTitle!,
                Trunc(FirstNonEmpty(c.InstructionBody, c.InstructionShortReminder, c.Instructions), 320),
                c.Category))
            .FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Body));

        return Ok(new CatalogFicheDto(
            path.Id, path.Title, path.Description, path.Level, path.Sector, path.EstimatedMinutes, path.Tags,
            modules.Count, challenges.Count, moduleDtos, themes, example));
    }

    private static string FirstNonEmpty(params string?[] xs) => xs.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
    private static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n).TrimEnd() + "…";

    // ── Helper : propagation Assignments lors d'activation globale ───────────
    private async Task PropagateToAllUsersAsync(Guid tenantId, Guid pathId, Guid adminId, DateTime now, CancellationToken ct)
    {
        var userIds = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .Select(u => u.Id).ToListAsync(ct);

        var already = await _db.Assignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.PathId == pathId && userIds.Contains(a.UserId))
            .Select(a => a.UserId).ToListAsync(ct);

        var toAssign = userIds.Except(already).ToList();
        foreach (var uid in toAssign)
        {
            _db.Assignments.Add(new Assignment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = uid,
                PathId = pathId,
                Status = Assignment.Statuses.Assigned,
                AssignedBy = adminId,
                AssignedAt = now,
                UpdatedAt = now
            });
        }
        if (toAssign.Count > 0) await _db.SaveChangesAsync(ct);
    }
}

public record AdminCatalogItemDto(
    Guid Id,
    string Title,
    string? Description,
    string? Level,
    string? Sector,
    int? EstimatedMinutes,
    string? Tags,
    int ChallengesCount,
    string Status,       // not_granted | granted_inactive | activated_global | activated_teams_only
    int TeamsUsingCount
);

public record ActivatePathDto(string Mode); // "global" | "teams_only"

// ── Fiche vitrine (lecture seule) : contenu réel du parcours, sans réponses ──
public record CatalogFicheModuleDto(string Title, int ChallengeCount);
public record CatalogFicheExampleDto(string Title, string Body, string? Category);
public record CatalogFicheDto(
    Guid Id,
    string Title,
    string? Description,
    string? Level,
    string? Sector,
    int? EstimatedMinutes,
    string? Tags,
    int ModuleCount,
    int ChallengeCount,
    List<CatalogFicheModuleDto> Modules,
    List<string> Themes,
    CatalogFicheExampleDto? Example
);
