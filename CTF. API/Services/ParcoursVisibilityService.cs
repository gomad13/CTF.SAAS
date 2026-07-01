using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services;

/// <summary>
/// Source unique de vérité pour déterminer quels parcours sont visibles par un tenant
/// ou par un user donné. À ne JAMAIS dupliquer ailleurs.
///
/// 3 niveaux :
///  1. SuperAdmin accorde l'accès catalogue au tenant (TenantParcoursAccess)
///  2. Admin tenant active le parcours pour ses users (TenantParcoursAssignment : scope global / teams_only)
///  3. User voit = (parcours privés du tenant) UNION (catalogue activé globalement OU via équipe OU via compliance)
/// </summary>
public class ParcoursVisibilityService
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    public ParcoursVisibilityService(AppDbContext db, TenantContext tenant) { _db = db; _tenant = tenant; }

    // ── Niveau tenant (admin-side) ──────────────────────────────────────────

    /// <summary>Parcours "accessibles" par le tenant = privés + catalogue accordé (qu'il soit activé ou non).</summary>
    public IQueryable<LearningPath> VisibleFor(Guid tenantId)
    {
        var grantedPathIds = _db.TenantParcoursAccesses
            .Where(a => a.TenantId == tenantId && a.RevokedAt == null)
            .Select(a => a.PathId);

        return _db.Paths
            .Where(p => p.TenantId == tenantId || (p.IsCatalog && grantedPathIds.Contains(p.Id)));
    }

    public async Task<bool> CanAccessAsync(Guid tenantId, Guid pathId, CancellationToken ct = default)
        => await VisibleFor(tenantId).AnyAsync(p => p.Id == pathId, ct);

    public async Task<List<Guid>> VisiblePathIdsAsync(Guid tenantId, CancellationToken ct = default)
        => await VisibleFor(tenantId).Select(p => p.Id).ToListAsync(ct);

    // ── Niveau activation Admin (entre accord et visibilité user) ───────────

    /// <summary>Ids des parcours catalogue accordés mais PAS encore activés par l'Admin.</summary>
    public async Task<List<Guid>> AvailableToActivateAsync(Guid tenantId, CancellationToken ct = default)
    {
        var granted = await _db.TenantParcoursAccesses.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.RevokedAt == null)
            .Select(a => a.PathId).ToListAsync(ct);
        var activated = await _db.TenantParcoursAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.DeactivatedAt == null)
            .Select(a => a.PathId).ToListAsync(ct);
        return granted.Except(activated).ToList();
    }

    /// <summary>Ids des parcours activés par l'Admin (quel que soit le scope).</summary>
    public async Task<List<TenantParcoursAssignment>> ActivatedByAdminAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.TenantParcoursAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.DeactivatedAt == null)
            .ToListAsync(ct);

    // ── Niveau user (règle UNION) ───────────────────────────────────────────

    /// <summary>
    /// Retourne les ids de parcours visibles pour un user :
    ///   (parcours privés du tenant)
    ///   ∪ (parcours catalogue activés globalement par l'Admin du tenant)
    ///   ∪ (parcours catalogue assignés à une équipe dont le user est membre)
    ///   ∪ (parcours catalogue rendus obligatoires via Compliance ciblant ce user)
    ///   ∪ (parcours avec Assignment individuel pour le user — rétro-compat)
    /// Dédoublonné.
    /// </summary>
    public async Task<List<Guid>> VisiblePathIdsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.TenantId, u.TeamId })
            .FirstOrDefaultAsync(ct);
        if (user is null) return new List<Guid>();

        // [MULTI-SOCIETES] La visibilité suit la société ACTIVE (claim/TenantContext),
        // pas la société d'origine du user. Fallback sur le home tenant hors contexte requête.
        var tenantId = _tenant.TenantId ?? user.TenantId;

        // 1) parcours privés du tenant (non catalog)
        var privateIds = await _db.Paths.AsNoTracking()
            .Where(p => p.TenantId == tenantId && !p.IsCatalog)
            .Select(p => p.Id).ToListAsync(ct);

        // 2) catalogue activé globalement par Admin tenant
        var globalActivated = await _db.TenantParcoursAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.DeactivatedAt == null && a.Scope == "global")
            .Select(a => a.PathId).ToListAsync(ct);

        // 3) catalogue assigné à une équipe dont le user est membre
        var teamIds = user.TeamId.HasValue
            ? await _db.TeamParcoursAssignments.AsNoTracking()
                .Where(t => t.TenantId == tenantId && t.TeamId == user.TeamId.Value)
                .Select(t => t.PathId).ToListAsync(ct)
            : new List<Guid>();

        // 4) compliance ciblant ce user (all_users, team, user)
        var mandatories = await _db.MandatoryAssignments.AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .Select(m => new { m.PathId, m.AssignedToType, m.AssignedToId })
            .ToListAsync(ct);
        var complianceIds = mandatories.Where(m =>
            m.AssignedToType == "all_users" ||
            (m.AssignedToType == "user" && m.AssignedToId == userId) ||
            (m.AssignedToType == "team" && user.TeamId.HasValue && m.AssignedToId == user.TeamId.Value)
        ).Select(m => m.PathId).ToList();

        // 5) Assignments individuels (rétro-compat — contient les assignations propagées par les teams + imports)
        var individualIds = await _db.Assignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.UserId == userId)
            .Select(a => a.PathId).ToListAsync(ct);

        // UNION dédoublonnée. On ne garde que les paths accessibles (privés OU catalogue accordé non révoqué).
        var grantedCatalog = await _db.TenantParcoursAccesses.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.RevokedAt == null)
            .Select(a => a.PathId).ToListAsync(ct);

        var all = new HashSet<Guid>();
        foreach (var id in privateIds) all.Add(id);
        foreach (var id in globalActivated.Concat(teamIds).Concat(complianceIds).Concat(individualIds))
        {
            // safety: ne pas exposer un parcours dont l'accès catalogue a été révoqué entre-temps
            var isPrivate = privateIds.Contains(id);
            if (isPrivate || grantedCatalog.Contains(id)) all.Add(id);
        }
        return all.ToList();
    }
}
