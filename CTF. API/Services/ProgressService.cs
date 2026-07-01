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

    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

    public async Task RecalculateFromChallengeAsync(
        Guid challengeId,
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var pathId = await _db.Challenges
            .Where(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == DemoTenantId))
            .Join(_db.Modules, c => c.ModuleId, m => m.Id, (c, m) => m.PathId)
            .SingleOrDefaultAsync(ct);

        if (pathId == Guid.Empty) return;

        // Délègue au service unique de calcul pour cohérence avec l'endpoint détail.
        var progressCalc = new ProgressCalculationService(_db);
        await progressCalc.RecalculateAndPersistAsync(userId, pathId, tenantId, ct);
    }
}