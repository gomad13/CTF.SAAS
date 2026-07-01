using Microsoft.EntityFrameworkCore;
using CTF.Api.Data;
using CTF.Api.Models;

namespace CTF.Api.Services;

/// <summary>
/// Service unique de calcul de progression d'un parcours pour un user donné.
/// Formule canonique : (#challenges complétés) / (#challenges totaux du parcours) * 100.
///
/// « Complété » = présence dans <c>Submissions (IsCorrect=true)</c> OU dans
/// <c>ChallengeCompletions</c> (les deux tables sont unies).
/// </summary>
public class ProgressCalculationService
{
    private readonly AppDbContext _db;
    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

    public ProgressCalculationService(AppDbContext db) { _db = db; }

    public sealed record PathProgressResult(
        int Completed,
        int Total,
        int Percent,
        string Status,
        IReadOnlyList<Guid> CompletedChallengeIds,
        IReadOnlyList<Guid> AllChallengeIds,
        IReadOnlyList<ModuleBreakdown> Modules
    );

    public sealed record ModuleBreakdown(
        Guid ModuleId,
        string Title,
        int SortOrder,
        int Total,
        int Completed,
        int Percent
    );

    public async Task<PathProgressResult> CalculateAsync(
        Guid userId, Guid pathId, Guid tenantId, CancellationToken ct = default)
    {
        var modules = await _db.Modules
            .AsNoTracking()
            .Where(m => (m.TenantId == tenantId || m.TenantId == DemoTenantId) && m.PathId == pathId)
            .OrderBy(m => m.SortOrder)
            .Select(m => new { m.Id, m.Title, m.SortOrder })
            .ToListAsync(ct);
        var moduleIds = modules.Select(m => m.Id).ToList();

        var challenges = await _db.Challenges
            .AsNoTracking()
            .Where(c => (c.TenantId == tenantId || c.TenantId == DemoTenantId) && moduleIds.Contains(c.ModuleId))
            .Select(c => new { c.Id, c.ModuleId })
            .ToListAsync(ct);
        var challengeIds = challenges.Select(c => c.Id).ToList();

        // Union Submissions (correct) + ChallengeCompletions
        var solvedFromSubmissions = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.UserId == userId
                && (s.TenantId == tenantId || s.TenantId == DemoTenantId)
                && s.IsCorrect
                && challengeIds.Contains(s.ChallengeId))
            .Select(s => s.ChallengeId)
            .Distinct()
            .ToListAsync(ct);

        var solvedFromCompletions = await _db.ChallengeCompletions
            .AsNoTracking()
            .Where(cc => cc.UserId == userId && challengeIds.Contains(cc.ChallengeId))
            .Select(cc => cc.ChallengeId)
            .Distinct()
            .ToListAsync(ct);

        var completedSet = new HashSet<Guid>(solvedFromSubmissions);
        foreach (var id in solvedFromCompletions) completedSet.Add(id);

        var total = challenges.Count;
        var completed = completedSet.Count;
        var percent = total == 0 ? 0 : (int)Math.Round((double)completed * 100.0 / total);
        var status = StatusFor(completed, total);

        var moduleBreakdown = modules.Select(m =>
        {
            var mCh = challenges.Where(c => c.ModuleId == m.Id).ToList();
            var mTotal = mCh.Count;
            var mCompleted = mCh.Count(c => completedSet.Contains(c.Id));
            var mPercent = mTotal == 0 ? 0 : (int)Math.Round((double)mCompleted * 100.0 / mTotal);
            return new ModuleBreakdown(m.Id, m.Title, m.SortOrder, mTotal, mCompleted, mPercent);
        }).ToList();

        return new PathProgressResult(completed, total, percent, status, completedSet.ToList(), challengeIds, moduleBreakdown);
    }

    /// <summary>
    /// Calcule ET persiste <c>Progresses.Percent</c> / <c>Status</c> pour un user/path.
    /// Utilisé à chaque fois qu'un challenge est complété.
    /// </summary>
    public async Task<PathProgressResult> RecalculateAndPersistAsync(
        Guid userId, Guid pathId, Guid tenantId, CancellationToken ct = default)
    {
        var result = await CalculateAsync(userId, pathId, tenantId, ct);

        var progress = await _db.Progresses
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId && p.PathId == pathId, ct);

        var now = DateTime.UtcNow;
        if (progress == null)
        {
            _db.Progresses.Add(new Progress
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                PathId = pathId,
                Percent = result.Percent,
                Status = result.Status,
                StartedAt = now,
                UpdatedAt = now,
                CompletedAt = result.Status == "completed" ? now : null,
            });
        }
        else
        {
            progress.Percent = result.Percent;
            progress.Status = result.Status;
            progress.UpdatedAt = now;
            if (result.Status == "completed" && progress.CompletedAt == null)
                progress.CompletedAt = now;
            else if (result.Status != "completed")
                progress.CompletedAt = null;
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }

    public static string StatusFor(int completed, int total)
    {
        if (total <= 0) return "not_started";
        if (completed <= 0) return "not_started";
        if (completed >= total) return "completed";
        return "in_progress";
    }
}
