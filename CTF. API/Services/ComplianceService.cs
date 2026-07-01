using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services;

public interface IComplianceService
{
    Task<ComplianceOverviewDto> GetOverviewAsync(Guid tenantId, CancellationToken ct);
    Task<MandatoryAssignmentDto> CreateAsync(Guid tenantId, Guid createdBy, CreateMandatoryAssignmentDto req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<int> RunNotificationsAsync(Guid tenantId, CancellationToken ct);
}

public class ComplianceService : IComplianceService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ComplianceService> _logger;

    public ComplianceService(AppDbContext db, ILogger<ComplianceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ComplianceOverviewDto> GetOverviewAsync(Guid tenantId, CancellationToken ct)
    {
        var assignments = await _db.MandatoryAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Deadline)
            .ToListAsync(ct);

        var pathIds = assignments.Select(a => a.PathId).Distinct().ToList();
        var paths = await _db.Paths.AsNoTracking()
            .Where(p => pathIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title })
            .ToListAsync(ct);

        var dtos = new List<MandatoryAssignmentDto>();
        int totalTargeted = 0;
        int totalCompleted = 0;

        foreach (var a in assignments)
        {
            var path = paths.FirstOrDefault(p => p.Id == a.PathId);
            var targeted = await CountTargetedAsync(tenantId, a, ct);
            var completed = await CountCompletedAsync(tenantId, a, ct);
            var rate = targeted > 0 ? (int)Math.Round(100.0 * completed / targeted) : 0;
            totalTargeted += targeted;
            totalCompleted += completed;

            dtos.Add(new MandatoryAssignmentDto(
                a.Id,
                a.PathId,
                path?.Title ?? "(parcours supprimé)",
                a.AssignedToType,
                a.AssignedToId,
                a.Deadline,
                rate,
                targeted,
                completed,
                a.CreatedAt));
        }

        var overall = totalTargeted > 0 ? (int)Math.Round(100.0 * totalCompleted / totalTargeted) : 100;
        return new ComplianceOverviewDto(assignments.Count, overall, dtos);
    }

    public async Task<MandatoryAssignmentDto> CreateAsync(Guid tenantId, Guid createdBy, CreateMandatoryAssignmentDto req, CancellationToken ct)
    {
        // Visibilité centralisée : parcours privé du tenant OU parcours catalogue accordé
        var grantedPathIds = _db.TenantParcoursAccesses
            .Where(a => a.TenantId == tenantId && a.RevokedAt == null)
            .Select(a => a.PathId);
        var pathExists = await _db.Paths.AsNoTracking().AnyAsync(
            p => p.Id == req.PathId && (p.TenantId == tenantId || (p.IsCatalog && grantedPathIds.Contains(p.Id))), ct);
        if (!pathExists) throw new InvalidOperationException("Parcours introuvable ou non accessible pour ce tenant.");

        var a = new MandatoryAssignment
        {
            TenantId = tenantId,
            PathId = req.PathId,
            AssignedToType = req.AssignedToType,
            AssignedToId = req.AssignedToId,
            Deadline = req.Deadline,
            CreatedBy = createdBy,
        };
        _db.MandatoryAssignments.Add(a);
        await _db.SaveChangesAsync(ct);

        var overview = await GetOverviewAsync(tenantId, ct);
        return overview.Assignments.First(x => x.Id == a.Id);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var a = await _db.MandatoryAssignments.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (a is null) return false;
        _db.MandatoryAssignments.Remove(a);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> RunNotificationsAsync(Guid tenantId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var horizons = new[] { 7, 3, 1 };
        int created = 0;

        var assignments = await _db.MandatoryAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.Deadline > now)
            .ToListAsync(ct);

        foreach (var a in assignments)
        {
            var daysLeft = (int)Math.Ceiling((a.Deadline - now).TotalDays);
            if (!horizons.Contains(daysLeft)) continue;

            var targetedUsers = await GetTargetedUsersAsync(tenantId, a, ct);
            var completed = await _db.Progresses.AsNoTracking()
                .Where(p => p.TenantId == tenantId && p.PathId == a.PathId && p.Status == "completed")
                .Select(p => p.UserId)
                .ToListAsync(ct);
            var pending = targetedUsers.Except(completed).ToList();

            var msg = $"Parcours obligatoire à terminer dans {daysLeft} jour{(daysLeft > 1 ? "s" : "")}.";
            foreach (var userId in pending)
            {
                var exists = await _db.Notifications.AsNoTracking().AnyAsync(n =>
                    n.UserId == userId && n.Type == "compliance_deadline" &&
                    n.CreatedAt >= now.AddHours(-24) && n.Message == msg, ct);
                if (exists) continue;

                _db.Notifications.Add(new Notification
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Type = "compliance_deadline",
                    Message = msg,
                    Link = $"/dashboard/parcours/{a.PathId}",
                });
                created++;
            }
        }

        if (created > 0) await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[COMPLIANCE] Tenant={TenantId} notifications créées={Count}", tenantId, created);
        return created;
    }

    private async Task<int> CountTargetedAsync(Guid tenantId, MandatoryAssignment a, CancellationToken ct)
    {
        var users = await GetTargetedUsersAsync(tenantId, a, ct);
        return users.Count;
    }

    private async Task<int> CountCompletedAsync(Guid tenantId, MandatoryAssignment a, CancellationToken ct)
    {
        var users = await GetTargetedUsersAsync(tenantId, a, ct);
        return await _db.Progresses.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.PathId == a.PathId && p.Status == "completed" && users.Contains(p.UserId))
            .CountAsync(ct);
    }

    private async Task<List<Guid>> GetTargetedUsersAsync(Guid tenantId, MandatoryAssignment a, CancellationToken ct)
    {
        var baseQ = _db.Users.AsNoTracking().Where(u => u.TenantId == tenantId && u.IsActive);
        return a.AssignedToType switch
        {
            "user" when a.AssignedToId.HasValue => await baseQ.Where(u => u.Id == a.AssignedToId.Value).Select(u => u.Id).ToListAsync(ct),
            "team" when a.AssignedToId.HasValue => await baseQ.Where(u => u.TeamId == a.AssignedToId.Value).Select(u => u.Id).ToListAsync(ct),
            _ => await baseQ.Select(u => u.Id).ToListAsync(ct),
        };
    }
}
