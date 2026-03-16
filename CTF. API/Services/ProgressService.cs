using Microsoft.EntityFrameworkCore;
using CTF.Api.Data;
using CTF.Api.Models;

namespace CTF.Api.Services;

public class ProgressService
{
    private readonly AppDbContext _db;

    public ProgressService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RecalculateFromChallengeAsync(
        Guid challengeId,
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        // 1) Challenge -> Module -> Path
        var pathId = await _db.Challenges
            .Where(c => c.Id == challengeId)
            .Join(_db.Modules, c => c.ModuleId, m => m.Id, (c, m) => m.PathId)
            .SingleAsync(ct);

        // 2) Total challenges du path
        var total = await _db.Challenges
            .Join(_db.Modules, c => c.ModuleId, m => m.Id, (c, m) => new { c.Id, m.PathId })
            .Where(x => x.PathId == pathId)
            .CountAsync(ct);

        if (total == 0) total = 1;

        // 3) Challenges résolus par le user (distinct)
        var solved = await _db.Submissions
            .Where(s => s.UserId == userId && s.IsCorrect)
            .Join(_db.Challenges, s => s.ChallengeId, c => c.Id, (s, c) => new { s.ChallengeId, c.ModuleId })
            .Join(_db.Modules, x => x.ModuleId, m => m.Id, (x, m) => new { x.ChallengeId, m.PathId })
            .Where(x => x.PathId == pathId)
            .Select(x => x.ChallengeId)
            .Distinct()
            .CountAsync(ct);

        // 4) Calcul du pourcentage
        var percent = (int)Math.Round((double)solved * 100.0 / total);

        // 5) Upsert Progress
        var progress = await _db.Progresses
            .SingleOrDefaultAsync(p => p.PathId == pathId && p.UserId == userId, ct);

        if (progress == null)
        {
            progress = new Progress
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PathId = pathId,
                UserId = userId,
                Percent = percent,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Progresses.Add(progress);
        }
        else
        {
            progress.Percent = percent;
            progress.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
