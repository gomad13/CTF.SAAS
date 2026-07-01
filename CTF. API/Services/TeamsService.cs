using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services;

public interface ITeamsService
{
    Task<List<TeamDto>> GetAllAsync(Guid tenantId, CancellationToken ct);
    Task<TeamDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<TeamDto> CreateAsync(Guid tenantId, CreateTeamDto req, CancellationToken ct);
    Task<TeamDto?> UpdateAsync(Guid tenantId, Guid id, UpdateTeamDto req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct);

    // Legacy assignation 1 user à 1 équipe — conservé pour compat
    Task<bool> AssignUserAsync(Guid tenantId, AssignUserToTeamDto req, CancellationToken ct);

    // Nouveaux endpoints
    Task<List<TeamMemberDto>> GetMembersAsync(Guid tenantId, Guid teamId, CancellationToken ct);
    Task<AddTeamMembersResultDto> AddMembersAsync(Guid tenantId, Guid teamId, List<Guid> userIds, CancellationToken ct);
    Task<bool> RemoveMemberAsync(Guid tenantId, Guid teamId, Guid userId, CancellationToken ct);

    // M4 — affectation à l'arrivée : lister les utilisateurs du tenant sans équipe
    Task<List<UnassignedUserDto>> GetUnassignedUsersAsync(Guid tenantId, CancellationToken ct);

    // Candidats à l'ajout dans UNE équipe : users du tenant DE L'ÉQUIPE pas déjà membres.
    // Scopé sur le TenantId de l'équipe (et non sur un tenant « effectif » ambigu).
    Task<List<UnassignedUserDto>> GetCandidateUsersAsync(Guid tenantId, Guid teamId, CancellationToken ct);

    // B4/B5 — côté utilisateur (role User) : parcourir, rejoindre, quitter, voir ses équipes.
    Task<List<UserTeamDto>> BrowseTeamsForUserAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<List<MyTeamDto>> GetMyTeamsAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<JoinLeaveResultDto> JoinTeamAsync(Guid tenantId, Guid userId, Guid teamId, CancellationToken ct);
    Task<JoinLeaveResultDto> LeaveTeamAsync(Guid tenantId, Guid userId, Guid teamId, CancellationToken ct);

    Task<List<TeamParcoursDto>> GetParcoursAsync(Guid tenantId, Guid teamId, CancellationToken ct);
    Task<TeamParcoursDto?> AssignParcoursAsync(Guid tenantId, Guid teamId, AssignParcoursToTeamDto req, Guid assignedBy, CancellationToken ct);
    Task<TeamParcoursDto?> UpdateParcoursAsync(Guid tenantId, Guid teamId, Guid pathId, UpdateTeamParcoursDto req, CancellationToken ct);
    Task<bool> RemoveParcoursAsync(Guid tenantId, Guid teamId, Guid pathId, CancellationToken ct);

    Task<TeamStatsDto?> GetStatsAsync(Guid tenantId, Guid teamId, CancellationToken ct);
}

public class TeamsService : ITeamsService
{
    private readonly AppDbContext _db;
    private readonly ProgressCalculationService _progress;

    public TeamsService(AppDbContext db, ProgressCalculationService progress)
    {
        _db = db;
        _progress = progress;
    }

    // ── CRUD teams ─────────────────────────────────────────────────────────

    public async Task<List<TeamDto>> GetAllAsync(Guid tenantId, CancellationToken ct)
    {
        var teams = await _db.Teams.AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        if (teams.Count == 0) return new();

        var teamIds = teams.Select(t => t.Id).ToList();

        var memberCounts = await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TenantId == tenantId && teamIds.Contains(m.TeamId))
            .GroupBy(m => m.TeamId)
            .Select(g => new { TeamId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var parcoursCounts = await _db.TeamParcoursAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && teamIds.Contains(a.TeamId))
            .GroupBy(a => a.TeamId)
            .Select(g => new { TeamId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Compliance moyenne = moyenne du Progress.Percent des users de l'équipe sur les parcours assignés à l'équipe
        var teamCompliance = new Dictionary<Guid, int>();
        foreach (var team in teams)
        {
            teamCompliance[team.Id] = await ComputeTeamCompliancePercentAsync(tenantId, team.Id, ct);
        }

        return teams.Select(t => new TeamDto(
            t.Id, t.Name, t.Description, t.Color, t.Icon, t.ManagerId,
            memberCounts.FirstOrDefault(x => x.TeamId == t.Id)?.Count ?? 0,
            parcoursCounts.FirstOrDefault(x => x.TeamId == t.Id)?.Count ?? 0,
            teamCompliance.TryGetValue(t.Id, out var pct) ? pct : 0,
            t.CreatedAt, t.MaxMembers, t.IsOpen)).ToList();
    }

    public async Task<TeamDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var t = await _db.Teams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (t == null) return null;
        var members = await _db.TeamMemberships.CountAsync(m => m.TeamId == id && m.TenantId == tenantId, ct);
        var parcours = await _db.TeamParcoursAssignments.CountAsync(a => a.TeamId == id && a.TenantId == tenantId, ct);
        var compliance = await ComputeTeamCompliancePercentAsync(tenantId, id, ct);
        return new TeamDto(t.Id, t.Name, t.Description, t.Color, t.Icon, t.ManagerId, members, parcours, compliance, t.CreatedAt, t.MaxMembers, t.IsOpen);
    }

    public async Task<TeamDto> CreateAsync(Guid tenantId, CreateTeamDto req, CancellationToken ct)
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = req.Name.Trim(),
            Description = req.Description,
            Color = req.Color,
            Icon = req.Icon,
            ManagerId = req.ManagerId,
            MaxMembers = req.MaxMembers,
            IsOpen = req.IsOpen,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);
        return new TeamDto(team.Id, team.Name, team.Description, team.Color, team.Icon, team.ManagerId, 0, 0, 0, team.CreatedAt, team.MaxMembers, team.IsOpen);
    }

    public async Task<TeamDto?> UpdateAsync(Guid tenantId, Guid id, UpdateTeamDto req, CancellationToken ct)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);
        if (team is null) return null;
        if (!string.IsNullOrWhiteSpace(req.Name)) team.Name = req.Name.Trim();
        if (req.Description != null) team.Description = req.Description;
        if (req.Color != null) team.Color = req.Color;
        if (req.Icon != null) team.Icon = req.Icon;
        if (req.MaxMembers.HasValue) team.MaxMembers = req.MaxMembers.Value;
        if (req.IsOpen.HasValue) team.IsOpen = req.IsOpen.Value;
        if (req.ManagerId.HasValue) team.ManagerId = req.ManagerId.Value == Guid.Empty ? null : req.ManagerId.Value;
        team.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(tenantId, id, ct);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);
        if (team is null) return false;

        // Supprime les appartenances à cette équipe (données users préservées)
        var memberships = await _db.TeamMemberships.Where(m => m.TeamId == id && m.TenantId == tenantId).ToListAsync(ct);
        var affectedUserIds = memberships.Select(m => m.UserId).Distinct().ToList();
        _db.TeamMemberships.RemoveRange(memberships);

        // Supprime les assignations de parcours à cette équipe
        await _db.TeamParcoursAssignments.Where(a => a.TeamId == id && a.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(ct);

        // Recale l'équipe principale des membres détachés (vers une autre équipe ou null)
        foreach (var uid in affectedUserIds) await SyncPrimaryTeamAsync(tenantId, uid, ct);
        return true;
    }

    public async Task<bool> AssignUserAsync(Guid tenantId, AssignUserToTeamDto req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId && u.TenantId == tenantId, ct);
        if (user is null) return false;

        // TeamId null = retrait de TOUTES les équipes (compat héritée).
        if (!req.TeamId.HasValue)
        {
            var all = await _db.TeamMemberships.Where(m => m.TenantId == tenantId && m.UserId == user.Id).ToListAsync(ct);
            _db.TeamMemberships.RemoveRange(all);
            user.TeamId = null;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == req.TeamId.Value && t.TenantId == tenantId, ct);
        if (team is null) return false;

        var already = await _db.TeamMemberships.AnyAsync(m => m.TeamId == team.Id && m.UserId == user.Id, ct);
        if (!already)
        {
            // M4 : respect du nombre max — refus si l'équipe est pleine.
            if (team.MaxMembers.HasValue)
            {
                var count = await _db.TeamMemberships.CountAsync(m => m.TeamId == team.Id, ct);
                if (count >= team.MaxMembers.Value) return false;
            }
            _db.TeamMemberships.Add(new TeamMembership { Id = Guid.NewGuid(), TenantId = tenantId, TeamId = team.Id, UserId = user.Id, JoinedAt = DateTime.UtcNow });
        }
        if (user.TeamId is null) user.TeamId = team.Id; // équipe principale dénormalisée
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // Recale l'« équipe principale » (User.TeamId) sur une appartenance encore valide, ou null.
    private async Task SyncPrimaryTeamAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, ct);
        if (user is null) return;
        if (user.TeamId.HasValue &&
            await _db.TeamMemberships.AnyAsync(m => m.UserId == userId && m.TeamId == user.TeamId.Value, ct))
            return; // l'équipe principale est toujours une appartenance valide
        var fallback = await _db.TeamMemberships
            .Where(m => m.TenantId == tenantId && m.UserId == userId)
            .Select(m => (Guid?)m.TeamId).FirstOrDefaultAsync(ct);
        if (user.TeamId != fallback) { user.TeamId = fallback; await _db.SaveChangesAsync(ct); }
    }

    // ── Membres ────────────────────────────────────────────────────────────

    public async Task<List<TeamMemberDto>> GetMembersAsync(Guid tenantId, Guid teamId, CancellationToken ct)
    {
        var team = await _db.Teams.AsNoTracking().AnyAsync(t => t.Id == teamId && t.TenantId == tenantId, ct);
        if (!team) return new();
        return await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TeamId == teamId && m.TenantId == tenantId)
            .Join(_db.Users.AsNoTracking(), m => m.UserId, u => u.Id, (m, u) => new { u, m.JoinedAt })
            .OrderBy(x => x.u.LastName).ThenBy(x => x.u.FirstName)
            .Select(x => new TeamMemberDto(x.u.Id, x.u.Email, x.u.FirstName ?? "", x.u.LastName ?? "", x.u.Role, x.u.IsActive, x.JoinedAt))
            .ToListAsync(ct);
    }

    public async Task<AddTeamMembersResultDto> AddMembersAsync(Guid tenantId, Guid teamId, List<Guid> userIds, CancellationToken ct)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.TenantId == tenantId, ct);
        if (team is null) return new AddTeamMembersResultDto(0, 0, 0, null, "Équipe introuvable.");

        var currentCount = await _db.TeamMemberships.CountAsync(m => m.TenantId == tenantId && m.TeamId == teamId, ct);

        // Déjà membres de CETTE équipe → ignorés (multi-équipes : être dans une autre équipe n'exclut pas).
        var existingSet = (await _db.TeamMemberships
            .Where(m => m.TeamId == teamId && m.TenantId == tenantId)
            .Select(m => m.UserId).ToListAsync(ct)).ToHashSet();

        // Candidats = utilisateurs valides DU MÊME TENANT (isolation stricte) pas déjà membres.
        var candidates = await _db.Users
            .Where(u => u.TenantId == tenantId && userIds.Contains(u.Id))
            .Select(u => u.Id).ToListAsync(ct);
        var newcomers = candidates.Where(uid => !existingSet.Contains(uid)).ToList();

        var toAdd = newcomers.Count;
        var rejected = 0;

        // M4 : respect du nombre max — on n'ajoute que ce qui rentre, le reste est refusé.
        if (team.MaxMembers.HasValue)
        {
            var available = Math.Max(0, team.MaxMembers.Value - currentCount);
            if (toAdd > available) { rejected = toAdd - available; toAdd = available; }
        }

        if (toAdd > 0)
        {
            var accepted = newcomers.Take(toAdd).ToList();
            var acceptedSet = accepted.ToHashSet();
            var now = DateTime.UtcNow;
            foreach (var uid in accepted)
                _db.TeamMemberships.Add(new TeamMembership { Id = Guid.NewGuid(), TenantId = tenantId, TeamId = teamId, UserId = uid, JoinedAt = now });
            // Équipe principale : ceux qui n'en ont pas encore prennent celle-ci.
            var primaryless = await _db.Users
                .Where(u => u.TenantId == tenantId && acceptedSet.Contains(u.Id) && u.TeamId == null)
                .ToListAsync(ct);
            foreach (var u in primaryless) u.TeamId = teamId;
            await _db.SaveChangesAsync(ct);
        }

        var error = rejected > 0
            ? $"Équipe pleine (capacité {team.MaxMembers} membres) : {rejected} membre(s) non ajouté(s)."
            : null;

        return new AddTeamMembersResultDto(toAdd, rejected, currentCount + toAdd, team.MaxMembers, error);
    }

    public async Task<List<UnassignedUserDto>> GetUnassignedUsersAsync(Guid tenantId, CancellationToken ct)
    {
        // « Sans équipe » = aucune appartenance (dans aucune équipe).
        var memberUserIds = _db.TeamMemberships.Where(m => m.TenantId == tenantId).Select(m => m.UserId);
        return await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && !memberUserIds.Contains(u.Id))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new UnassignedUserDto(u.Id, u.Email, u.FirstName ?? "", u.LastName ?? "", u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<List<UnassignedUserDto>> GetCandidateUsersAsync(Guid tenantId, Guid teamId, CancellationToken ct)
    {
        // On résout les candidats sur le TENANT DE L'ÉQUIPE (source de vérité unique),
        // pas sur un « tenant effectif » qui peut diverger selon le contexte d'appel.
        var team = await _db.Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId && t.TenantId == tenantId, ct);
        if (team is null) return new();

        var memberIds = _db.TeamMemberships.Where(m => m.TeamId == teamId).Select(m => m.UserId);
        return await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == team.TenantId && !memberIds.Contains(u.Id))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new UnassignedUserDto(u.Id, u.Email, u.FirstName ?? "", u.LastName ?? "", u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }

    // ── B4/B5 — côté utilisateur ─────────────────────────────────────────────

    public async Task<List<UserTeamDto>> BrowseTeamsForUserAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var teams = await _db.Teams.AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
        if (teams.Count == 0) return new();

        var teamIds = teams.Select(t => t.Id).ToList();
        var counts = await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TenantId == tenantId && teamIds.Contains(m.TeamId))
            .GroupBy(m => m.TeamId)
            .Select(g => new { TeamId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var myTeamIds = (await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.UserId == userId)
            .Select(m => m.TeamId).ToListAsync(ct)).ToHashSet();

        return teams.Select(t =>
        {
            var count = counts.FirstOrDefault(c => c.TeamId == t.Id)?.Count ?? 0;
            var full = t.MaxMembers.HasValue && count >= t.MaxMembers.Value;
            return new UserTeamDto(t.Id, t.Name, t.Description, t.Color, t.Icon, count, t.MaxMembers,
                t.IsOpen, myTeamIds.Contains(t.Id), full);
        }).ToList();
    }

    public async Task<List<MyTeamDto>> GetMyTeamsAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var myTeamIds = await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.UserId == userId)
            .Select(m => m.TeamId).ToListAsync(ct);
        if (myTeamIds.Count == 0) return new();

        var teams = await _db.Teams.AsNoTracking()
            .Where(t => t.TenantId == tenantId && myTeamIds.Contains(t.Id))
            .OrderBy(t => t.Name).ToListAsync(ct);

        var members = await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TenantId == tenantId && myTeamIds.Contains(m.TeamId))
            .Join(_db.Users.AsNoTracking(), m => m.UserId, u => u.Id, (m, u) => new { m.TeamId, u })
            .OrderBy(x => x.u.LastName).ThenBy(x => x.u.FirstName)
            .Select(x => new { x.TeamId, Member = new UserTeamMemberDto(x.u.Id, x.u.FirstName ?? "", x.u.LastName ?? "", x.u.Role) })
            .ToListAsync(ct);

        return teams.Select(t =>
        {
            var ms = members.Where(x => x.TeamId == t.Id).Select(x => x.Member).ToList();
            return new MyTeamDto(t.Id, t.Name, t.Description, t.Color, t.Icon, t.IsOpen, ms.Count, t.MaxMembers, ms);
        }).ToList();
    }

    public async Task<JoinLeaveResultDto> JoinTeamAsync(Guid tenantId, Guid userId, Guid teamId, CancellationToken ct)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.TenantId == tenantId, ct);
        if (team is null) return new JoinLeaveResultDto(false, "Équipe introuvable dans votre entreprise.");
        if (!team.IsOpen) return new JoinLeaveResultDto(false, "Cette équipe est fermée : seul un administrateur peut vous y affecter.");

        var already = await _db.TeamMemberships.AnyAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
        if (already) return new JoinLeaveResultDto(true, null);

        if (team.MaxMembers.HasValue)
        {
            var count = await _db.TeamMemberships.CountAsync(m => m.TeamId == teamId, ct);
            if (count >= team.MaxMembers.Value)
                return new JoinLeaveResultDto(false, $"Équipe pleine (capacité {team.MaxMembers} membres).");
        }

        _db.TeamMemberships.Add(new TeamMembership { Id = Guid.NewGuid(), TenantId = tenantId, TeamId = teamId, UserId = userId, JoinedAt = DateTime.UtcNow });
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, ct);
        if (user is not null && user.TeamId is null) user.TeamId = teamId; // équipe principale
        await _db.SaveChangesAsync(ct);
        return new JoinLeaveResultDto(true, null);
    }

    public async Task<JoinLeaveResultDto> LeaveTeamAsync(Guid tenantId, Guid userId, Guid teamId, CancellationToken ct)
    {
        var membership = await _db.TeamMemberships
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId && m.TenantId == tenantId, ct);
        if (membership is null) return new JoinLeaveResultDto(false, "Vous n'êtes pas membre de cette équipe.");
        _db.TeamMemberships.Remove(membership);
        await _db.SaveChangesAsync(ct);
        await SyncPrimaryTeamAsync(tenantId, userId, ct);
        return new JoinLeaveResultDto(true, null);
    }

    public async Task<bool> RemoveMemberAsync(Guid tenantId, Guid teamId, Guid userId, CancellationToken ct)
    {
        var membership = await _db.TeamMemberships
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId && m.TenantId == tenantId, ct);
        if (membership is null) return false;
        _db.TeamMemberships.Remove(membership);
        await _db.SaveChangesAsync(ct);
        await SyncPrimaryTeamAsync(tenantId, userId, ct); // recale l'équipe principale si besoin
        return true;
    }

    // ── Parcours ──────────────────────────────────────────────────────────

    public async Task<List<TeamParcoursDto>> GetParcoursAsync(Guid tenantId, Guid teamId, CancellationToken ct)
    {
        var team = await _db.Teams.AnyAsync(t => t.Id == teamId && t.TenantId == tenantId, ct);
        if (!team) return new();

        var assignments = await _db.TeamParcoursAssignments.AsNoTracking()
            .Where(a => a.TeamId == teamId && a.TenantId == tenantId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(ct);

        if (assignments.Count == 0) return new();

        var pathIds = assignments.Select(a => a.PathId).ToList();
        var demoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");
        var paths = await _db.Paths.AsNoTracking()
            .Where(p => pathIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Level })
            .ToListAsync(ct);

        var moduleCounts = await _db.Modules.AsNoTracking()
            .Where(m => pathIds.Contains(m.PathId))
            .GroupBy(m => m.PathId)
            .Select(g => new { PathId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var challengeCounts = await _db.Challenges.AsNoTracking()
            .Join(_db.Modules, c => c.ModuleId, m => m.Id, (c, m) => new { c, m.PathId })
            .Where(x => pathIds.Contains(x.PathId))
            .GroupBy(x => x.PathId)
            .Select(g => new { PathId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // AvgCompletionPercent par parcours : moyenne des Progresses.Percent des membres de l'équipe sur ce parcours
        var memberIds = await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TeamId == teamId && m.TenantId == tenantId)
            .Select(m => m.UserId).ToListAsync(ct);

        var memberProgresses = memberIds.Count == 0 ? new List<ProgressAvgRow>()
            : await _db.Progresses.AsNoTracking()
                .Where(p => memberIds.Contains(p.UserId) && pathIds.Contains(p.PathId))
                .GroupBy(p => p.PathId)
                .Select(g => new ProgressAvgRow(g.Key, (int)Math.Round(g.Average(x => x.Percent))))
                .ToListAsync(ct);

        return assignments.Select(a =>
        {
            var p = paths.FirstOrDefault(pp => pp.Id == a.PathId);
            return new TeamParcoursDto(
                a.PathId,
                p?.Title ?? "(parcours supprimé)",
                p?.Level,
                moduleCounts.FirstOrDefault(m => m.PathId == a.PathId)?.Count ?? 0,
                challengeCounts.FirstOrDefault(c => c.PathId == a.PathId)?.Count ?? 0,
                a.Deadline,
                a.IsMandatory,
                memberProgresses.FirstOrDefault(x => x.PathId == a.PathId)?.Avg ?? 0,
                a.AssignedAt);
        }).ToList();
    }

    private sealed record ProgressAvgRow(Guid PathId, int Avg);

    public async Task<TeamParcoursDto?> AssignParcoursAsync(
        Guid tenantId, Guid teamId, AssignParcoursToTeamDto req, Guid assignedBy, CancellationToken ct)
    {
        var team = await _db.Teams.AnyAsync(t => t.Id == teamId && t.TenantId == tenantId, ct);
        if (!team) return null;

        // Visibilité centralisée : parcours privé du tenant OU parcours catalogue accordé via TenantParcoursAccess
        var grantedPathIds = _db.TenantParcoursAccesses
            .Where(a => a.TenantId == tenantId && a.RevokedAt == null)
            .Select(a => a.PathId);
        var pathExists = await _db.Paths.AnyAsync(
            p => p.Id == req.PathId && (p.TenantId == tenantId || (p.IsCatalog && grantedPathIds.Contains(p.Id))), ct);
        if (!pathExists) return null;

        var already = await _db.TeamParcoursAssignments.AnyAsync(a => a.TeamId == teamId && a.PathId == req.PathId, ct);
        if (already) return null;

        _db.TeamParcoursAssignments.Add(new TeamParcoursAssignment
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            PathId = req.PathId,
            TenantId = tenantId,
            Deadline = req.Deadline,
            IsMandatory = req.IsMandatory,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy,
        });
        await _db.SaveChangesAsync(ct);

        // Auto-propagation : crée les Assignments individuelles pour chaque membre de l'équipe
        // (cohérent avec le modèle user.Assignments existant, pas de nouvelle surface d'appel front).
        var memberIds = await _db.TeamMemberships.Where(m => m.TeamId == teamId && m.TenantId == tenantId).Select(m => m.UserId).ToListAsync(ct);
        foreach (var userId in memberIds)
        {
            var existingAssignment = await _db.Assignments.AnyAsync(
                a => a.TenantId == tenantId && a.UserId == userId && a.PathId == req.PathId, ct);
            if (!existingAssignment)
            {
                _db.Assignments.Add(new Assignment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = userId,
                    PathId = req.PathId,
                    Status = Assignment.Statuses.Assigned,
                    DueAt = req.Deadline,
                    AssignedBy = assignedBy,
                    AssignedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        var list = await GetParcoursAsync(tenantId, teamId, ct);
        return list.FirstOrDefault(p => p.PathId == req.PathId);
    }

    public async Task<TeamParcoursDto?> UpdateParcoursAsync(
        Guid tenantId, Guid teamId, Guid pathId, UpdateTeamParcoursDto req, CancellationToken ct)
    {
        var a = await _db.TeamParcoursAssignments.FirstOrDefaultAsync(
            x => x.TeamId == teamId && x.PathId == pathId && x.TenantId == tenantId, ct);
        if (a is null) return null;

        if (req.Deadline.HasValue || req.IsMandatory.HasValue)
        {
            if (req.Deadline.HasValue) a.Deadline = req.Deadline.Value == default ? null : req.Deadline.Value;
            if (req.IsMandatory.HasValue) a.IsMandatory = req.IsMandatory.Value;
            await _db.SaveChangesAsync(ct);

            // Propager la deadline aux assignments individuelles
            if (req.Deadline.HasValue)
            {
                await _db.Assignments
                    .Where(asg => asg.TenantId == tenantId && asg.PathId == pathId
                        && _db.TeamMemberships.Any(m => m.UserId == asg.UserId && m.TeamId == teamId))
                    .ExecuteUpdateAsync(s => s.SetProperty(asg => asg.DueAt, a.Deadline), ct);
            }
        }

        var list = await GetParcoursAsync(tenantId, teamId, ct);
        return list.FirstOrDefault(x => x.PathId == pathId);
    }

    public async Task<bool> RemoveParcoursAsync(Guid tenantId, Guid teamId, Guid pathId, CancellationToken ct)
    {
        var a = await _db.TeamParcoursAssignments.FirstOrDefaultAsync(
            x => x.TeamId == teamId && x.PathId == pathId && x.TenantId == tenantId, ct);
        if (a is null) return false;
        _db.TeamParcoursAssignments.Remove(a);
        await _db.SaveChangesAsync(ct);
        // On ne retire PAS les Assignments individuelles : la progression reste conservée
        // côté user (RGPD — pas d'effet rétroactif).
        return true;
    }

    // ── Stats ─────────────────────────────────────────────────────────────

    public async Task<TeamStatsDto?> GetStatsAsync(Guid tenantId, Guid teamId, CancellationToken ct)
    {
        var team = await _db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId && t.TenantId == tenantId, ct);
        if (team is null) return null;

        var memberIds = await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TeamId == teamId && m.TenantId == tenantId)
            .Select(m => m.UserId).ToListAsync(ct);

        var parcoursList = await GetParcoursAsync(tenantId, teamId, ct);
        var overall = parcoursList.Count == 0 ? 0 : (int)Math.Round(parcoursList.Average(p => (double)p.AvgCompletionPercent));

        var topMembers = new List<TeamTopMemberDto>();
        if (memberIds.Count > 0)
        {
            var stats = await _db.ChallengeCompletions.AsNoTracking()
                .Where(cc => memberIds.Contains(cc.UserId))
                .GroupBy(cc => cc.UserId)
                .Select(g => new { UserId = g.Key, Completed = g.Count(), AvgScore = (int)Math.Round(g.Average(x => (double)x.ScorePercent)) })
                .OrderByDescending(x => x.Completed)
                .Take(3).ToListAsync(ct);

            var userIds = stats.Select(x => x.UserId).ToList();
            var users = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName })
                .ToListAsync(ct);

            topMembers = stats.Select(s =>
            {
                var u = users.FirstOrDefault(x => x.Id == s.UserId);
                return new TeamTopMemberDto(s.UserId, u?.Email ?? "?", u?.FirstName ?? "", u?.LastName ?? "", s.Completed, s.AvgScore);
            }).ToList();
        }

        return new TeamStatsDto(team.Id, team.Name, memberIds.Count, parcoursList.Count, overall, parcoursList, topMembers);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<int> ComputeTeamCompliancePercentAsync(Guid tenantId, Guid teamId, CancellationToken ct)
    {
        var memberIds = await _db.TeamMemberships.Where(m => m.TeamId == teamId && m.TenantId == tenantId)
            .Select(m => m.UserId).ToListAsync(ct);
        if (memberIds.Count == 0) return 0;

        var pathIds = await _db.TeamParcoursAssignments.Where(a => a.TeamId == teamId && a.TenantId == tenantId)
            .Select(a => a.PathId).ToListAsync(ct);
        if (pathIds.Count == 0) return 0;

        var percents = await _db.Progresses.AsNoTracking()
            .Where(p => memberIds.Contains(p.UserId) && pathIds.Contains(p.PathId))
            .Select(p => p.Percent)
            .ToListAsync(ct);

        if (percents.Count == 0) return 0;
        return (int)Math.Round(percents.Average());
    }
}
