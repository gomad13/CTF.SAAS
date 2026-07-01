using CTF.Api.Contracts;
using CTF.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CTF.Api.Services;

public interface IAnalyticsService
{
    Task<AnalyticsOverviewDto> GetOverviewAsync(Guid tenantId, int daysWindow, CancellationToken ct);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public AnalyticsService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(Guid tenantId, int daysWindow, CancellationToken ct)
    {
        daysWindow = Math.Clamp(daysWindow, 7, 365);
        var cacheKey = $"analytics:{tenantId}:{daysWindow}";
        if (_cache.TryGetValue<AnalyticsOverviewDto>(cacheKey, out var cached) && cached is not null)
            return cached;

        var from30 = DateTime.UtcNow.AddDays(-30);
        var from7 = DateTime.UtcNow.AddDays(-7);
        var fromWindow = DateTime.UtcNow.AddDays(-daysWindow);

        var completionsQ = _db.ChallengeCompletions.AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.IsDemo);

        var activeUsers7 = await completionsQ.Where(c => c.CompletedAt >= from7).Select(c => c.UserId).Distinct().CountAsync(ct);
        var activeUsers30 = await completionsQ.Where(c => c.CompletedAt >= from30).Select(c => c.UserId).Distinct().CountAsync(ct);
        var totalCompletions = await completionsQ.CountAsync(ct);
        var averageScore = await completionsQ.AnyAsync(ct) ? (int)await completionsQ.AverageAsync(c => c.ScorePercent, ct) : 0;

        var progressQ = _db.Progresses.AsNoTracking().Where(p => p.TenantId == tenantId);
        var avgCompletion = await progressQ.AnyAsync(ct) ? (int)await progressQ.AverageAsync(p => (double)p.Percent, ct) : 0;

        var activityRows = await completionsQ
            .Where(c => c.CompletedAt >= fromWindow)
            .Select(c => new { c.CompletedAt })
            .ToListAsync(ct);
        var activityRaw = activityRows
            .GroupBy(x => x.CompletedAt.Date)
            .Select(g => new ActivityPointDto(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        var byPath = await _db.ChallengeCompletions.AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.IsDemo)
            .Join(_db.Challenges.AsNoTracking(), c => c.ChallengeId, ch => ch.Id, (c, ch) => new { c, ch })
            .Join(_db.Modules.AsNoTracking(), x => x.ch.ModuleId, m => m.Id, (x, m) => new { x.c, m })
            .GroupBy(x => new { x.m.PathId })
            .Select(g => new { g.Key.PathId, Completions = g.Count(), Avg = (int)g.Average(x => x.c.ScorePercent) })
            .ToListAsync(ct);

        var pathTitles = await _db.Paths.AsNoTracking()
            .Where(p => byPath.Select(b => b.PathId).Contains(p.Id))
            .Select(p => new { p.Id, p.Title })
            .ToListAsync(ct);

        var byPathDto = byPath.Join(pathTitles, b => b.PathId, p => p.Id,
            (b, p) => new CompletionByPathDto(b.PathId, p.Title, b.Completions, b.Avg)).ToList();

        var byType = await _db.ChallengeCompletions.AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.IsDemo)
            .Join(_db.Challenges.AsNoTracking(), c => c.ChallengeId, ch => ch.Id, (c, ch) => ch.Type)
            .GroupBy(t => t)
            .Select(g => new ChallengeTypeStatDto(g.Key, g.Count()))
            .ToListAsync(ct);

        var kpis = new AnalyticsKpisDto(activeUsers7, activeUsers30, totalCompletions, averageScore, avgCompletion);
        var dto = new AnalyticsOverviewDto(kpis, activityRaw, byPathDto, byType);

        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }
}
