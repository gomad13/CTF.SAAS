using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Services.RiskScoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CTF.Api.Tests;

/// <summary>
/// Tests unitaires de la formule du Cyber Resilience Index.
///
/// Approche : EF Core InMemory provider — léger, rapide, suffisant pour
/// vérifier la logique applicative. Le provider InMemory ignore les contraintes
/// PostgreSQL (jsonb, FK cascade) qui ne sont pas le sujet de ces tests.
/// </summary>
public class RiskScoringServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RiskScoringService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private static readonly string[] AllTypes =
        { "ceo_fraud", "mailbox", "multichoice", "password_quiz", "phishing_ai" };

    public RiskScoringServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _service = new RiskScoringService(_db, NullLogger<RiskScoringService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Score_Is_Null_When_User_Has_No_Attempts()
    {
        var dto = await _service.ComputeScoreForUserAsync(_userId, _tenantId, CancellationToken.None);

        Assert.Null(dto.Score);
    }

    [Fact]
    public async Task Score_Is_Null_When_User_Has_Less_Than_3_Attempts()
    {
        await SeedAttempts(("ceo_fraud", 100, DaysAgo(5)), ("mailbox", 90, DaysAgo(4)));
        var dto = await _service.ComputeScoreForUserAsync(_userId, _tenantId, CancellationToken.None);

        Assert.Null(dto.Score);
    }

    [Fact]
    public async Task Score_Is_100_When_All_Types_Mastered_No_Regression()
    {
        // 5 réussites parfaites (1 par type), espacées de 1 jour. Pas de régression possible.
        var attempts = AllTypes
            .Select((t, i) => (t, 100, DaysAgo(10 - i)))
            .ToArray();
        await SeedAttempts(attempts);

        var dto = await _service.ComputeScoreForUserAsync(_userId, _tenantId, CancellationToken.None);

        Assert.NotNull(dto.Score);
        Assert.Equal(100, dto.Score);
    }

    [Fact]
    public async Task Score_Reflects_Zero_Success_Rate_With_Other_Components()
    {
        // 5 tentatives, toutes < 70% : succès=0, diversité=0, régression=100, vitesse=100.
        // Score pondéré = 0*0.5 + 100*0.15 + 0*0.20 + 100*0.15 = 30.
        // (Adaptation documentée : la formule pondérée donne 30, pas 0,
        //  car régression et vitesse n'ont rien à pénaliser.)
        var attempts = AllTypes
            .Select((t, i) => (t, 30, DaysAgo(10 - i)))
            .ToArray();
        await SeedAttempts(attempts);

        var dto = await _service.ComputeScoreForUserAsync(_userId, _tenantId, CancellationToken.None);

        Assert.NotNull(dto.Score);
        Assert.Equal(30, dto.Score);
        Assert.Equal(0.0, dto.Components.SuccessRate);
    }

    [Fact]
    public async Task Score_Aggregates_All_Components_With_Documented_Weights()
    {
        // Construction d'un cas mixte :
        // - 6 tentatives : 3 réussites (≥ 70%), 3 échecs (< 70%) → SuccessRate = 50.
        // - Réussites sur 3 types distincts : ceo_fraud, mailbox, multichoice → Diversity = 60.
        // - 1 régression : un échec < 50% sur ceo_fraud APRÈS le 1er succès du même type.
        // - Vitesse : intervalles utilisateur réguliers (1 jour) ≤ médiane globale → Speed = 100.
        // Score attendu = 50*0.5 + 100*0.15 + 60*0.20 + 80*0.15 = 25 + 15 + 12 + 12 = 64.
        await SeedAttempts(
            ("ceo_fraud",   100, DaysAgo(20)),  // succès
            ("mailbox",     100, DaysAgo(19)),  // succès
            ("multichoice", 100, DaysAgo(18)),  // succès
            ("ceo_fraud",    20, DaysAgo(10)),  // régression (échec < 50% après succès, dans les 30j)
            ("password_quiz", 0, DaysAgo(9)),
            ("phishing_ai",   0, DaysAgo(8)));

        var dto = await _service.ComputeScoreForUserAsync(_userId, _tenantId, CancellationToken.None);

        Assert.NotNull(dto.Score);
        Assert.Equal(64, dto.Score);
        Assert.Equal(50.0, dto.Components.SuccessRate);
        Assert.Equal(60.0, dto.Components.DiversityScore);
        Assert.Equal(80.0, dto.Components.RegressionScore);
    }

    [Fact]
    public async Task Multi_Tenant_Isolation_Other_Tenant_Data_Is_Ignored()
    {
        // User A dans tenant A → 5 succès parfaits.
        // Même UserId existe dans tenant B → 5 succès aussi (mais on ne doit pas les voir).
        var tenantB = Guid.NewGuid();
        var attemptsA = AllTypes.Select((t, i) => (t, 100, DaysAgo(10 - i))).ToArray();
        await SeedAttempts(attemptsA, tenantOverride: _tenantId);

        // On ajoute des challenges + completions sur tenant B avec le même UserId.
        // Ces données ne doivent pas affecter le score calculé pour tenant A.
        for (var i = 0; i < 5; i++)
        {
            var chId = Guid.NewGuid();
            _db.Challenges.Add(new Challenge
            {
                Id = chId, TenantId = tenantB, ModuleId = Guid.NewGuid(),
                Type = "interactive", ContentType = "ceo_fraud",
                Title = "B", Instructions = "B", Status = "published",
                Points = 10, CreatedAt = DateTime.UtcNow,
            });
            _db.ChallengeCompletions.Add(new ChallengeCompletion
            {
                Id = Guid.NewGuid(), UserId = _userId, TenantId = tenantB,
                ChallengeId = chId, ChallengeTitle = "B", PointsEarned = 0, ScorePercent = 0,
                CompletedAt = DaysAgo(5),
            });
        }
        await _db.SaveChangesAsync();

        // Score sur tenant A → 100 (les échecs tenant B sont ignorés).
        var dtoA = await _service.ComputeScoreForUserAsync(_userId, _tenantId, CancellationToken.None);
        Assert.Equal(100, dtoA.Score);

        // Score sur tenant B → tous les ScorePercent = 0 → SuccessRate=0, Diversity=0,
        // Regression=100, Speed=100 → 0.5*0 + 0.15*100 + 0.20*0 + 0.15*100 = 30.
        var dtoB = await _service.ComputeScoreForUserAsync(_userId, tenantB, CancellationToken.None);
        Assert.Equal(30, dtoB.Score);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DateTime DaysAgo(int days) => DateTime.UtcNow.AddDays(-days);

    private async Task SeedAttempts(params (string type, int score, DateTime at)[] attempts)
        => await SeedAttempts(attempts, tenantOverride: null);

    private async Task SeedAttempts(
        (string type, int score, DateTime at)[] attempts,
        Guid? tenantOverride)
    {
        var tenant = tenantOverride ?? _tenantId;
        foreach (var (type, score, at) in attempts)
        {
            var chId = Guid.NewGuid();
            _db.Challenges.Add(new Challenge
            {
                Id = chId, TenantId = tenant, ModuleId = Guid.NewGuid(),
                Type = "interactive", ContentType = type,
                Title = type, Instructions = "x", Status = "published",
                Points = 10, CreatedAt = at,
            });
            _db.ChallengeCompletions.Add(new ChallengeCompletion
            {
                Id = Guid.NewGuid(), UserId = _userId, TenantId = tenant,
                ChallengeId = chId, ChallengeTitle = type, PointsEarned = score / 10,
                ScorePercent = score, CompletedAt = at,
            });
        }
        await _db.SaveChangesAsync();
    }
}
