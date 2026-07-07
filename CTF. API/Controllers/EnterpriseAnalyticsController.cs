using System.Globalization;
using System.Text;
using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>Analytics — onglet ENTREPRISE. VRAIES données du tenant (TenantId du JWT), aucune donnée démo en fallback.</summary>
[ApiController]
[Route("api/analytics/enterprise")]
[Authorize(Roles = "admin,SuperAdmin")]
public class EnterpriseAnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public EnterpriseAnalyticsController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private static readonly string[] MoisFr =
        { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc." };

    private static string Band(int s) => s >= 80 ? "Excellent" : s >= 60 ? "Bon" : s >= 40 ? "Moyen" : "À renforcer";
    private static string Norm(string s) => s.Trim().ToLowerInvariant();

    /// <summary>Résout le tenant du JWT et vérifie l'accès Analytics. Error != null si refusé.</summary>
    private async Task<(ActionResult? Error, Guid TenantId)> GuardAsync(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return (Unauthorized(), Guid.Empty);
        if (!await ModeToggleHelper.IsEnabledAsync(_db, tenantId, ModeToggleHelper.Mode.Analytics, ct))
            return (StatusCode(403, new { error = "Analytics mode is not enabled for this tenant." }), tenantId);
        return (null, tenantId);
    }

    // ── Bloc phare : Points faibles à renforcer ───────────────────────────────
    [HttpGet("weak-topics")]
    public async Task<ActionResult<EnterpriseWeakTopicsDto>> GetWeakTopics([FromQuery] int top = 5, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        return Ok(await ComputeWeakTopicsAsync(tenantId, top, ct));
    }

    private async Task<EnterpriseWeakTopicsDto> ComputeWeakTopicsAsync(Guid tenantId, int top, CancellationToken ct)
    {
        if (top < 1) top = 5;
        if (top > 20) top = 20;

        // Complétions du tenant (hors démo) jointes à la catégorie du challenge
        var rows = await (
            from cc in _db.ChallengeCompletions.AsNoTracking()
            join ch in _db.Challenges.AsNoTracking() on cc.ChallengeId equals ch.Id
            where cc.TenantId == tenantId && !cc.IsDemo && ch.Category != null && ch.Category != ""
            select new { Cat = ch.Category!, cc.ScorePercent }
        ).ToListAsync(ct);

        var themeChallenges = await _db.Challenges.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status == "published" && c.Category != null && c.Category != "")
            .Select(c => new { c.Category, c.Id })
            .ToListAsync(ct);
        var tenantUsers = await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId, ct);

        // Normalisation en mémoire (regroupement insensible casse/espaces ; libellé canonique = 1re occurrence)
        var chPerTheme = themeChallenges.GroupBy(c => Norm(c.Category!))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).Distinct().Count());
        var labelPerTheme = themeChallenges.GroupBy(c => Norm(c.Category!))
            .ToDictionary(g => g.Key, g => g.First().Category!.Trim());

        var evaluated = rows.GroupBy(r => Norm(r.Cat))
            .Select(g =>
            {
                var completions = g.Count();
                var avgScore = (int)Math.Round(g.Average(r => r.ScorePercent));
                var chCount = chPerTheme.TryGetValue(g.Key, out var c) ? c : 1;
                var maxSlots = Math.Max(1, tenantUsers) * Math.Max(1, chCount);
                var completionRate = Math.Min(100, (int)Math.Round(100.0 * completions / maxSlots));
                var mastery = (int)Math.Round(avgScore * completionRate / 100.0);
                var label = labelPerTheme.TryGetValue(g.Key, out var l) ? l : g.First().Cat.Trim();
                return new WeakTopicDto(label, avgScore, completionRate, mastery, completions);
            })
            .Where(t => t.Completions >= 3) // seuil de fiabilité
            .OrderBy(t => t.Mastery)
            .ToList();

        return new EnterpriseWeakTopicsDto(evaluated.Take(top).ToList(), evaluated.Count);
    }

    // ── Risque cyber global + courbe ──────────────────────────────────────────
    [HttpGet("risk")]
    public async Task<ActionResult<EnterpriseRiskDto>> GetRisk([FromQuery] int months = 6, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        return Ok(await ComputeRiskAsync(tenantId, months, ct));
    }

    private async Task<EnterpriseRiskDto> ComputeRiskAsync(Guid tenantId, int months, CancellationToken ct)
    {
        if (months < 1) months = 1;
        if (months > 24) months = 24;

        var scores = await _db.RiskScoreHistories.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.Score != null)
            .Select(r => new { r.UserId, Score = r.Score!.Value, r.ComputedAt })
            .ToListAsync(ct);

        if (scores.Count == 0)
            return new EnterpriseRiskDto(null, "—", new List<RiskPointDto>(), 0);

        var latestPerUser = scores.GroupBy(x => x.UserId)
            .Select(g => g.OrderByDescending(x => x.ComputedAt).First().Score).ToList();
        var globalScore = (int)Math.Round(latestPerUser.Average());
        var usersScored = latestPerUser.Count;

        // Courbe mensuelle : moyenne des scores du mois ; report du dernier connu si mois vide
        var since = DateTime.UtcNow.Date.AddMonths(-(months - 1));
        var start = new DateTime(since.Year, since.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        int? last = null;
        var trend = new List<RiskPointDto>();
        foreach (var i in Enumerable.Range(0, months))
        {
            var m = start.AddMonths(i);
            var pts = scores.Where(x => x.ComputedAt.Year == m.Year && x.ComputedAt.Month == m.Month).ToList();
            if (pts.Count > 0) last = (int)Math.Round(pts.Average(p => p.Score));
            trend.Add(new RiskPointDto(MoisFr[m.Month - 1], last ?? globalScore));
        }

        return new EnterpriseRiskDto(globalScore, Band(globalScore), trend, usersScored);
    }

    // ── Engagement ────────────────────────────────────────────────────────────
    [HttpGet("engagement")]
    public async Task<ActionResult<EnterpriseEngagementDto>> GetEngagement(CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        return Ok(await ComputeEngagementAsync(tenantId, ct));
    }

    private async Task<EnterpriseEngagementDto> ComputeEngagementAsync(Guid tenantId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var d7 = now.AddDays(-7);
        var d30 = now.AddDays(-30);

        var totalUsers = await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId, ct);
        var active7d = await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.LastActivityAt != null && u.LastActivityAt >= d7, ct);
        var active30d = await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.LastActivityAt != null && u.LastActivityAt >= d30, ct);
        var neverConnected = await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.LastLoginAt == null, ct);
        var totalCompletions = await _db.ChallengeCompletions.AsNoTracking().CountAsync(cc => cc.TenantId == tenantId && !cc.IsDemo, ct);
        var usersWithActivity = await _db.ChallengeCompletions.AsNoTracking()
            .Where(cc => cc.TenantId == tenantId && !cc.IsDemo).Select(cc => cc.UserId).Distinct().CountAsync(ct);

        var participationRate = totalUsers > 0 ? (int)Math.Round(100.0 * usersWithActivity / totalUsers) : 0;
        var avgPerActive = usersWithActivity > 0 ? (int)Math.Round((double)totalCompletions / usersWithActivity) : 0;

        return new EnterpriseEngagementDto(totalUsers, active7d, active30d, neverConnected, participationRate, totalCompletions, avgPerActive);
    }

    // ── Export CSV du rapport entreprise ──────────────────────────────────────
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] int months = 6, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;

        var weak = await ComputeWeakTopicsAsync(tenantId, 20, ct);
        var risk = await ComputeRiskAsync(tenantId, months, ct);
        var eng = await ComputeEngagementAsync(tenantId, ct);

        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("Rapport Analytics Entreprise");
        sb.AppendLine($"Genere le;{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", ci)} UTC");
        sb.AppendLine();
        sb.AppendLine("Risque cyber global");
        sb.AppendLine($"Score global;{(risk.GlobalScore?.ToString(ci) ?? "n/a")};Niveau;{risk.Band};Users evalues;{risk.UsersScored}");
        sb.AppendLine();
        sb.AppendLine("Engagement");
        sb.AppendLine($"Total users;{eng.TotalUsers};Actifs 7j;{eng.Active7d};Actifs 30j;{eng.Active30d};Jamais connectes;{eng.NeverConnected};Participation %;{eng.ParticipationRate}");
        sb.AppendLine();
        sb.AppendLine("Points faibles a renforcer;thème;score moyen;taux completion;maitrise;completions");
        foreach (var t in weak.Topics)
            sb.AppendLine($";{t.Theme.Replace(';', ',')};{t.AvgScore};{t.CompletionRate};{t.Mastery};{t.Completions}");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"analytics-entreprise-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
