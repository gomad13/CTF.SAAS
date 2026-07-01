using Microsoft.EntityFrameworkCore;
using CTF.Api.Data;
using CTF.Api.Models;

namespace CTF.Api.Services;

/// <summary>
/// Service transactionnel de suppression d'un tenant.
/// Responsabilités :
/// - Interdire la suppression du tenant Demo (Guid.Empty)
/// - Snapshot des volumes avant suppression (audit RGPD)
/// - Suppression cascade explicite de toutes les tables tenant-scopées
/// - Journalisation SuperAdmin (critical)
///
/// Les tables <c>SuperAdminAuditLogs</c> et <c>SuperAdmins</c> ne sont PAS touchées :
/// elles sont globales et protégées par procédure hors-API.
/// </summary>
public class TenantDeletionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TenantDeletionService> _logger;

    public TenantDeletionService(AppDbContext db, ILogger<TenantDeletionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public sealed record TenantSnapshot(
        string Name,
        int Users,
        int Paths,
        int Modules,
        int Challenges,
        int Progresses,
        int Submissions,
        int ChallengeCompletions,
        int Assignments,
        int MandatoryAssignments,
        int Teams,
        int Campaigns,
        int Announcements,
        int AuditLogs,
        int TenantEmailDomains,
        int TenantLicenses,
        int RefreshTokens,
        int Notifications
    );

    public sealed class TenantDeletionException : Exception
    {
        public TenantDeletionException(string message) : base(message) { }
    }

    /// <summary>
    /// Supprime un tenant et toutes ses données dépendantes en une transaction.
    /// Lance <see cref="TenantDeletionException"/> sur garde-fou (Demo).
    /// </summary>
    public async Task<TenantSnapshot> DeleteTenantAsync(
        Guid tenantId, string performedBy, string ipAddress, CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
            throw new TenantDeletionException("Cannot delete demo tenant");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new TenantDeletionException("Tenant introuvable.");

        // Snapshot AVANT toute mutation.
        var userIds = await _db.Users
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var snapshot = new TenantSnapshot(
            Name: tenant.Name,
            Users: userIds.Count,
            Paths: await _db.Paths.CountAsync(p => p.TenantId == tenantId, ct),
            Modules: await _db.Modules.CountAsync(m => m.TenantId == tenantId, ct),
            Challenges: await _db.Challenges.CountAsync(c => c.TenantId == tenantId, ct),
            Progresses: await _db.Progresses.CountAsync(p => p.TenantId == tenantId, ct),
            Submissions: await _db.Submissions.CountAsync(s => s.TenantId == tenantId, ct),
            ChallengeCompletions: await _db.ChallengeCompletions.CountAsync(cc => userIds.Contains(cc.UserId), ct),
            Assignments: await _db.Assignments.CountAsync(a => a.TenantId == tenantId, ct),
            MandatoryAssignments: await _db.MandatoryAssignments.CountAsync(m => m.TenantId == tenantId, ct),
            Teams: await _db.Teams.CountAsync(t => t.TenantId == tenantId, ct),
            Campaigns: await _db.Campaigns.CountAsync(c => c.TenantId == tenantId, ct),
            Announcements: await _db.Announcements.CountAsync(a => a.TenantId == tenantId, ct),
            AuditLogs: await _db.AuditLogs.CountAsync(a => a.TenantId == tenantId, ct),
            TenantEmailDomains: await _db.TenantEmailDomains.CountAsync(d => d.TenantId == tenantId, ct),
            TenantLicenses: await _db.TenantLicenses.CountAsync(l => l.TenantId == tenantId, ct),
            RefreshTokens: await _db.RefreshTokens.CountAsync(rt => userIds.Contains(rt.UserId), ct),
            Notifications: await _db.Notifications.CountAsync(n => n.TenantId == tenantId, ct)
        );

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Ordre : enfants de users d'abord (refresh tokens, completions), puis tenant-scoped enfants,
            // puis parents, puis users, puis tenant.

            await _db.RefreshTokens
                .Where(rt => userIds.Contains(rt.UserId))
                .ExecuteDeleteAsync(ct);

            await _db.ChallengeCompletions
                .Where(cc => userIds.Contains(cc.UserId))
                .ExecuteDeleteAsync(ct);

            await _db.Submissions
                .Where(s => s.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Progresses
                .Where(p => p.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            // Campagnes : participations → cibles → paths liés → campagnes
            var campaignIds = await _db.Campaigns
                .Where(c => c.TenantId == tenantId)
                .Select(c => c.Id).ToListAsync(ct);
            if (campaignIds.Count > 0)
            {
                await _db.CampaignParticipations
                    .Where(cp => campaignIds.Contains(cp.CampaignId))
                    .ExecuteDeleteAsync(ct);
                await _db.CampaignTargets
                    .Where(ct2 => campaignIds.Contains(ct2.CampaignId))
                    .ExecuteDeleteAsync(ct);
                await _db.CampaignPaths
                    .Where(cp => campaignIds.Contains(cp.CampaignId))
                    .ExecuteDeleteAsync(ct);
            }
            await _db.Campaigns
                .Where(c => c.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.MandatoryAssignments
                .Where(m => m.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Assignments
                .Where(a => a.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Teams
                .Where(t => t.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Notifications
                .Where(n => n.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Announcements
                .Where(a => a.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Challenges
                .Where(c => c.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Modules
                .Where(m => m.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Paths
                .Where(p => p.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.TenantEmailDomains
                .Where(d => d.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.TenantLicenses
                .Where(l => l.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.AuditLogs
                .Where(a => a.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Users
                .Where(u => u.TenantId == tenantId)
                .ExecuteDeleteAsync(ct);

            await _db.Tenants
                .Where(t => t.Id == tenantId)
                .ExecuteDeleteAsync(ct);

            // Audit SuperAdmin — après succès des deletes, avant commit final.
            _db.SuperAdminAuditLogs.Add(new SuperAdminAuditLog
            {
                Action = "DELETE_TENANT",
                Description =
                    $"Hard-deleted tenant '{snapshot.Name}' ({tenantId}) — " +
                    $"users={snapshot.Users}, paths={snapshot.Paths}, modules={snapshot.Modules}, " +
                    $"challenges={snapshot.Challenges}, progresses={snapshot.Progresses}, " +
                    $"submissions={snapshot.Submissions}, completions={snapshot.ChallengeCompletions}, " +
                    $"assignments={snapshot.Assignments}, teams={snapshot.Teams}, " +
                    $"campaigns={snapshot.Campaigns}, licenses={snapshot.TenantLicenses}",
                PerformedBy = performedBy,
                IpAddress = ipAddress,
                Severity = "critical",
                CreatedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            _logger.LogCritical("[SUPERADMIN ACTION] Tenant {Id} ({Name}) deleted by {User}", tenantId, snapshot.Name, performedBy);
            return snapshot;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Tenant deletion rolled back for {Id}", tenantId);
            throw;
        }
    }
}
