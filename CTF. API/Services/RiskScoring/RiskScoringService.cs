using System.Text.Json;
using CTF.Api.Contracts.RiskScore;
using CTF.Api.Data;
using CTF.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services.RiskScoring;

/// <summary>
/// Implémentation du Cyber Resilience Index (CRI).
///
/// Périmètre des données : la formule s'appuie EXCLUSIVEMENT sur la table
/// <c>ChallengeCompletions</c>, qui est le journal officiel des 5 types de
/// challenges interactifs cités dans le prompt CRI V1
/// (<c>ceo_fraud, mailbox, multichoice, password_quiz, phishing_ai</c>).
///
/// Adaptation BD : tous les challenges interactifs ont <c>Challenges.Type = "interactive"</c>
/// et le sous-type discriminant est dans <c>Challenges.ContentType</c>. On filtre
/// donc sur <c>ContentType</c> pour distinguer les 5 types CRI.
///
/// La table <c>Submissions</c> est ignorée car les types simples (quiz, scenario,
/// email…) ne sont pas dans les 5 catégories du CRI et viendraient bruiter le
/// calcul de diversité.
///
/// Adaptation vs spec d'origine :
/// — la base ne contient ni <c>StartedAt</c> ni <c>SubmittedAt</c> distincts pour
///   un même challenge interactif, uniquement <c>CompletedAt</c>. La composante
///   « vitesse de complétion » est donc adaptée : on mesure la médiane des
///   intervalles entre 2 complétions consécutives sur les 90 jours. Plus l'user
///   complète régulièrement, plus le score vitesse est élevé. Cette proxy est
///   documentée dans le rapport final.
///
/// Sécurité : toutes les requêtes filtrent par <c>TenantId</c>. Aucune exception.
/// </summary>
public sealed class RiskScoringService : IRiskScoringService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RiskScoringService> _logger;

    private const int LookbackDays = 90;
    private const int RegressionWindowDays = 30;
    private const int MinAttemptsForScore = 3;
    private const int SuccessThresholdPercent = 70;
    private const int RegressionThresholdPercent = 50;
    private const int TotalChallengeTypes = 5;
    private const int RegressionPenaltyPerEvent = 20;
    private const int SpeedSlowMultiplier = 3;

    private static readonly string[] CriChallengeTypes =
    {
        "ceo_fraud", "mailbox", "multichoice", "password_quiz", "phishing_ai"
    };

    public RiskScoringService(AppDbContext db, ILogger<RiskScoringService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RiskScoreDto> ComputeScoreForUserAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        var attempts = await LoadAttemptsAsync(userId, tenantId, ct);
        var totalAttempts = attempts.Count;

        if (totalAttempts < MinAttemptsForScore)
        {
            return new RiskScoreDto(
                Score: null,
                Components: new RiskScoreComponentsDto(0, 0, 0, 0),
                ComputedAt: DateTime.UtcNow);
        }

        var successRate = ComputeSuccessRate(attempts);
        var diversityScore = ComputeDiversityScore(attempts);
        var regressionScore = ComputeRegressionScore(attempts);
        var globalMedian = await ComputeGlobalSpeedMedianAsync(tenantId, ct);
        var speedScore = ComputeSpeedScore(attempts, globalMedian);

        var weighted =
            successRate * 0.50 +
            speedScore * 0.15 +
            diversityScore * 0.20 +
            regressionScore * 0.15;

        var finalScore = (int)Math.Round(Math.Clamp(weighted, 0, 100));

        return new RiskScoreDto(
            Score: finalScore,
            Components: new RiskScoreComponentsDto(successRate, speedScore, diversityScore, regressionScore),
            ComputedAt: DateTime.UtcNow);
    }

    public async Task ComputeAndStoreScoresForAllActiveUsersAsync(CancellationToken ct)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogInformation("CRI batch start at {StartedAt:o}", startedAt);

        var activeUsers = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, u.TenantId })
            .ToListAsync(ct);

        var processed = 0;
        var errors = 0;
        foreach (var user in activeUsers)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var dto = await ComputeScoreForUserAsync(user.Id, user.TenantId, ct);
                _db.RiskScoreHistories.Add(new RiskScoreHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TenantId = user.TenantId,
                    Score = dto.Score,
                    Components = JsonSerializer.Serialize(dto.Components),
                    ComputedAt = dto.ComputedAt
                });
                processed++;
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex, "CRI compute failed for user {UserId}", user.Id);
            }
        }

        await _db.SaveChangesAsync(ct);

        var duration = DateTime.UtcNow - startedAt;
        _logger.LogInformation(
            "CRI batch end : {Processed} users processed, {Errors} errors, {Duration}",
            processed, errors, duration);
    }

    public async Task<RiskScoreDto?> GetLatestScoreAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        var latest = await _db.RiskScoreHistories.AsNoTracking()
            .Where(h => h.UserId == userId && h.TenantId == tenantId)
            .OrderByDescending(h => h.ComputedAt)
            .Select(h => new { h.Score, h.Components, h.ComputedAt })
            .FirstOrDefaultAsync(ct);

        if (latest is null) return null;

        var components = TryDeserializeComponents(latest.Components);
        return new RiskScoreDto(latest.Score, components, latest.ComputedAt);
    }

    public async Task<IReadOnlyList<RiskScoreHistoryPointDto>> GetHistoryAsync(
        Guid userId, Guid tenantId, int months, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        var rows = await _db.RiskScoreHistories.AsNoTracking()
            .Where(h => h.UserId == userId && h.TenantId == tenantId && h.ComputedAt >= cutoff)
            .OrderBy(h => h.ComputedAt)
            .Select(h => new RiskScoreHistoryPointDto(h.ComputedAt, h.Score))
            .ToListAsync(ct);

        return rows;
    }

    // ── Calcul par composante ────────────────────────────────────────────────

    private async Task<List<AttemptRow>> LoadAttemptsAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-LookbackDays);

        // Jointure sur Challenges pour récupérer le ContentType (les 5 sous-types
        // interactifs CRI). Filtre tenant principal sur cc.TenantId (l'enregistrement
        // de complétion porte le tenant de l'utilisateur). Le tenant du challenge
        // n'est PAS filtré ici : un utilisateur CyberMed peut compléter un challenge
        // Demo (parcours catalogue / découverte), ce qui est un cas légitime du
        // seed. La sécurité multi-tenant reste assurée par cc.TenantId == tenantId.
        return await (
            from cc in _db.ChallengeCompletions.AsNoTracking()
            join c in _db.Challenges.AsNoTracking() on cc.ChallengeId equals c.Id
            where cc.UserId == userId
                  && cc.TenantId == tenantId
                  && cc.CompletedAt >= since
                  && c.ContentType != null
                  && CriChallengeTypes.Contains(c.ContentType)
            orderby cc.CompletedAt
            select new AttemptRow(c.ContentType!, cc.ScorePercent, cc.CompletedAt)
        ).ToListAsync(ct);
    }

    private static double ComputeSuccessRate(IReadOnlyList<AttemptRow> attempts)
    {
        if (attempts.Count == 0) return 0;
        var successes = attempts.Count(a => a.ScorePercent >= SuccessThresholdPercent);
        return (double)successes / attempts.Count * 100.0;
    }

    private static double ComputeDiversityScore(IReadOnlyList<AttemptRow> attempts)
    {
        var masteredTypes = attempts
            .Where(a => a.ScorePercent >= SuccessThresholdPercent)
            .Select(a => a.Type)
            .Distinct()
            .Count();
        return (double)masteredTypes / TotalChallengeTypes * 100.0;
    }

    private static double ComputeRegressionScore(IReadOnlyList<AttemptRow> attempts)
    {
        var regressionCutoff = DateTime.UtcNow.AddDays(-RegressionWindowDays);
        var typeFirstSuccess = attempts
            .Where(a => a.ScorePercent >= SuccessThresholdPercent)
            .GroupBy(a => a.Type)
            .ToDictionary(g => g.Key, g => g.Min(a => a.CompletedAt));

        var regressions = attempts.Count(a =>
            a.CompletedAt >= regressionCutoff
            && a.ScorePercent < RegressionThresholdPercent
            && typeFirstSuccess.TryGetValue(a.Type, out var firstSuccess)
            && a.CompletedAt > firstSuccess);

        var raw = 100 - regressions * RegressionPenaltyPerEvent;
        return Math.Clamp(raw, 0, 100);
    }

    private async Task<double> ComputeGlobalSpeedMedianAsync(Guid tenantId, CancellationToken ct)
    {
        // Médiane globale tenant : intervalles entre challenges consécutifs sur 90 jours,
        // tous users confondus. Sert de référence pour scorer la vitesse individuelle.
        var since = DateTime.UtcNow.AddDays(-LookbackDays);
        var rows = await _db.ChallengeCompletions.AsNoTracking()
            .Where(cc => cc.TenantId == tenantId && cc.CompletedAt >= since)
            .OrderBy(cc => cc.UserId).ThenBy(cc => cc.CompletedAt)
            .Select(cc => new { cc.UserId, cc.CompletedAt })
            .ToListAsync(ct);

        var intervals = new List<double>();
        for (var i = 1; i < rows.Count; i++)
        {
            if (rows[i].UserId == rows[i - 1].UserId)
                intervals.Add((rows[i].CompletedAt - rows[i - 1].CompletedAt).TotalDays);
        }
        return Median(intervals);
    }

    private static double ComputeSpeedScore(IReadOnlyList<AttemptRow> attempts, double globalMedian)
    {
        // Médiane des intervalles user. Score 100 si user ≤ globale ; dégradation
        // linéaire jusqu'à 0 si ≥ 3× globale. Si globale = 0 (pas de référence),
        // on neutralise : score 100 par défaut (pas de pénalité injustifiée).
        if (globalMedian <= 0 || attempts.Count < 2) return 100;

        var sorted = attempts.OrderBy(a => a.CompletedAt).ToList();
        var userIntervals = new List<double>(sorted.Count - 1);
        for (var i = 1; i < sorted.Count; i++)
            userIntervals.Add((sorted[i].CompletedAt - sorted[i - 1].CompletedAt).TotalDays);

        var userMedian = Median(userIntervals);
        if (userMedian <= globalMedian) return 100;

        var slowThreshold = globalMedian * SpeedSlowMultiplier;
        if (userMedian >= slowThreshold) return 0;

        var ratio = (userMedian - globalMedian) / (slowThreshold - globalMedian);
        return Math.Clamp(100 - ratio * 100, 0, 100);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static double Median(List<double> values)
    {
        if (values.Count == 0) return 0;
        values.Sort();
        var mid = values.Count / 2;
        return values.Count % 2 == 0
            ? (values[mid - 1] + values[mid]) / 2.0
            : values[mid];
    }

    private static RiskScoreComponentsDto TryDeserializeComponents(string json)
    {
        try
        {
            var d = JsonSerializer.Deserialize<RiskScoreComponentsDto>(json);
            return d ?? new RiskScoreComponentsDto(0, 0, 0, 0);
        }
        catch
        {
            return new RiskScoreComponentsDto(0, 0, 0, 0);
        }
    }

    private sealed record AttemptRow(string Type, int ScorePercent, DateTime CompletedAt);
}
