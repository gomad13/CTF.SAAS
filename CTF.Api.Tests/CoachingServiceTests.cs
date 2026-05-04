using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Services.Coaching;
using CTF.Api.Services.LLM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CTF.Api.Tests;

/// <summary>
/// Tests unitaires de CoachingService.
/// Provider LLM mocké via FakeLlm pour rester déterministe et rapide
/// (pas d'appel Ollama réel). Le test E2E avec le vrai modèle est fait à
/// part lors de la vérification fonctionnelle.
/// </summary>
public class CoachingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public CoachingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Coaching:Ollama:PrimaryModel"] = "mistral:7b",
                ["Coaching:Ollama:FallbackModel"] = "llama3.2:3b",
                ["Coaching:Ollama:TimeoutSeconds"] = "60",
                ["Coaching:Generation:MaxTokens"] = "500",
                ["Coaching:Generation:Temperature"] = "0.7",
                ["Coaching:Generation:MaxContentLength"] = "2000",
                ["Coaching:Cache:TtlHours"] = "24",
                ["Coaching:RateLimit:MaxGenerationsPerUserPerMinute"] = "5",
            })
            .Build();
    }

    public void Dispose()
    {
        _db.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public async Task Generate_With_Working_Llm_Returns_Generated_Status()
    {
        var attemptId = await SeedAttempt("ceo_fraud", scorePercent: 30);
        var llm = new FakeLlm(available: true, modelAvailable: true,
            response: new LLMResponse("Coaching généré pour test.", 100, 50, TimeSpan.FromSeconds(2), "mistral:7b"));

        var svc = NewService(llm);
        var dto = await svc.GenerateForAttemptAsync(attemptId, _userId, _tenantId, CancellationToken.None);

        Assert.Equal("Generated", dto.Status);
        Assert.Equal("ceo_fraud", dto.ChallengeType);
        Assert.Contains("test", dto.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Generate_When_Provider_Throws_Returns_Fallback()
    {
        var attemptId = await SeedAttempt("phishing_ai", scorePercent: 10);
        // Provider qui throw → traduit en null (catch dans GenerateAsync) → fallback.
        var llm = new FakeLlm(available: true, modelAvailable: true, throwOnGenerate: true);

        var svc = NewService(llm);
        var dto = await svc.GenerateForAttemptAsync(attemptId, _userId, _tenantId, CancellationToken.None);

        Assert.Equal("Fallback", dto.Status);
        Assert.False(string.IsNullOrWhiteSpace(dto.Content));
    }

    [Fact]
    public async Task Generate_When_Provider_Unavailable_Returns_Fallback()
    {
        var attemptId = await SeedAttempt("multichoice", scorePercent: 0);
        var llm = new FakeLlm(available: false, modelAvailable: false);

        var svc = NewService(llm);
        var dto = await svc.GenerateForAttemptAsync(attemptId, _userId, _tenantId, CancellationToken.None);

        Assert.Equal("Fallback", dto.Status);
    }

    [Fact]
    public async Task Generate_Twice_For_Same_Attempt_Returns_Cached_Existing()
    {
        var attemptId = await SeedAttempt("password_quiz", scorePercent: 40);
        var llm = new FakeLlm(available: true, modelAvailable: true,
            response: new LLMResponse("Premier coaching.", 80, 40, TimeSpan.FromSeconds(1), "mistral:7b"));

        var svc = NewService(llm);
        var first = await svc.GenerateForAttemptAsync(attemptId, _userId, _tenantId, CancellationToken.None);
        var second = await svc.GenerateForAttemptAsync(attemptId, _userId, _tenantId, CancellationToken.None);

        // Idempotent : même DTO renvoyé, et le provider n'a été appelé qu'une fois.
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, llm.GenerateCallCount);
    }

    [Fact]
    public async Task Multi_Tenant_Isolation_Other_Tenant_Cannot_Access()
    {
        var tenantA = _tenantId;
        var tenantB = Guid.NewGuid();
        var attemptIdA = await SeedAttempt("mailbox", scorePercent: 50, tenant: tenantA);

        var svc = NewService(new FakeLlm(available: true, modelAvailable: true,
            response: new LLMResponse("ok", 1, 1, TimeSpan.Zero, "mistral:7b")));

        // Génère le coaching dans le bon tenant.
        await svc.GenerateForAttemptAsync(attemptIdA, _userId, tenantA, CancellationToken.None);

        // Demander le même attempt depuis tenant B → 404 (KeyNotFound).
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.GenerateForAttemptAsync(attemptIdA, _userId, tenantB, CancellationToken.None));

        // L'historique de tenant B est vide.
        var hist = await svc.GetHistoryAsync(_userId, tenantB, 1, 10, CancellationToken.None);
        Assert.Empty(hist.Items);
    }

    [Fact]
    public async Task Generate_With_Attempt_From_Other_User_Returns_NotFound()
    {
        var otherUser = Guid.NewGuid();
        var attemptId = await SeedAttempt("ceo_fraud", scorePercent: 20, userOverride: otherUser);
        var svc = NewService(new FakeLlm(available: true, modelAvailable: true,
            response: new LLMResponse("ok", 1, 1, TimeSpan.Zero, "mistral:7b")));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.GenerateForAttemptAsync(attemptId, _userId, _tenantId, CancellationToken.None));
    }

    [Fact]
    public async Task Generate_Truncates_Content_Above_MaxLength()
    {
        var attemptId = await SeedAttempt("phishing_ai", scorePercent: 0);
        // Contenu de 5000 caractères, dépasse MaxContentLength=2000.
        var huge = string.Concat(Enumerable.Repeat("Ceci est une longue phrase de coaching. ", 200));
        var llm = new FakeLlm(available: true, modelAvailable: true,
            response: new LLMResponse(huge, 200, 1500, TimeSpan.FromSeconds(3), "mistral:7b"));

        var svc = NewService(llm);
        var dto = await svc.GenerateForAttemptAsync(attemptId, _userId, _tenantId, CancellationToken.None);

        Assert.Equal("Generated", dto.Status);
        Assert.True(dto.Content.Length <= 2000);
    }

    [Fact]
    public async Task GetHistory_Pagination_Respects_PageSize()
    {
        // Crée 7 coachings, 1 par attempt distinct (à cause de l'idempotence).
        for (var i = 0; i < 7; i++)
        {
            var aid = await SeedAttempt("multichoice", scorePercent: 30);
            var svc1 = NewService(new FakeLlm(available: true, modelAvailable: true,
                response: new LLMResponse($"Coaching #{i}", 1, 1, TimeSpan.Zero, "mistral:7b")));
            await svc1.GenerateForAttemptAsync(aid, _userId, _tenantId, CancellationToken.None);
        }

        var svc = NewService(new FakeLlm(available: true, modelAvailable: true));
        var page1 = await svc.GetHistoryAsync(_userId, _tenantId, page: 1, pageSize: 3, CancellationToken.None);

        Assert.Equal(7, page1.TotalCount);
        Assert.Equal(3, page1.Items.Count);
        Assert.Equal(1, page1.Page);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private CoachingService NewService(IOllamaLLMProvider llm)
        => new CoachingService(_db, llm, _cache, _config, NullLogger<CoachingService>.Instance);

    private async Task<Guid> SeedAttempt(string contentType, int scorePercent, Guid? tenant = null, Guid? userOverride = null)
    {
        var t = tenant ?? _tenantId;
        var u = userOverride ?? _userId;
        var chId = Guid.NewGuid();
        _db.Challenges.Add(new Challenge
        {
            Id = chId, TenantId = t, ModuleId = Guid.NewGuid(),
            Type = "interactive", ContentType = contentType,
            Title = $"Test {contentType}", Instructions = "Description.",
            Status = "published", Points = 10, CreatedAt = DateTime.UtcNow,
        });
        var ccId = Guid.NewGuid();
        _db.ChallengeCompletions.Add(new ChallengeCompletion
        {
            Id = ccId, UserId = u, TenantId = t, ChallengeId = chId,
            ChallengeTitle = contentType, ScorePercent = scorePercent,
            PointsEarned = scorePercent / 10, CompletedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
        return ccId;
    }

    /// <summary>Stub IOllamaLLMProvider pour les tests unitaires.</summary>
    private sealed class FakeLlm : IOllamaLLMProvider
    {
        private readonly bool _available;
        private readonly bool _modelAvailable;
        private readonly LLMResponse? _response;
        private readonly bool _throwOnGenerate;

        public int GenerateCallCount { get; private set; }

        public FakeLlm(bool available = true, bool modelAvailable = true,
            LLMResponse? response = null, bool throwOnGenerate = false)
        {
            _available = available;
            _modelAvailable = modelAvailable;
            _response = response;
            _throwOnGenerate = throwOnGenerate;
        }

        public Task<bool> IsAvailableAsync(CancellationToken ct) => Task.FromResult(_available);
        public Task<bool> IsModelAvailableAsync(string modelName, CancellationToken ct) => Task.FromResult(_modelAvailable);
        public Task<bool> EnsureModelDownloadedAsync(string modelName, CancellationToken ct) => Task.FromResult(_modelAvailable);

        public Task<LLMResponse?> GenerateAsync(LLMRequest request, CancellationToken ct)
        {
            GenerateCallCount++;
            if (_throwOnGenerate)
                return Task.FromResult<LLMResponse?>(null);
            return Task.FromResult(_response);
        }
    }
}
