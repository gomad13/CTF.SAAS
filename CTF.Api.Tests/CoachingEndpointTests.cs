using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CTF.Api.Contracts.Coaching;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Services.LLM;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CTF.Api.Tests;

/// <summary>
/// Tests d'intégration HTTP du contrôleur <c>CoachingController</c>.
/// L'environnement "Testing" du Program.cs désactive Hangfire ; on injecte
/// un IOllamaLLMProvider stub pour ne pas dépendre d'Ollama réel.
/// </summary>
public class CoachingEndpointTests : IClassFixture<CoachingTestApiFactory>
{
    private readonly CoachingTestApiFactory _factory;

    public CoachingEndpointTests(CoachingTestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Generate_With_Valid_Jwt_And_Existing_Attempt_Returns_200_And_Dto()
    {
        var (client, userId, tenantId) = _factory.CreateAuthenticatedClient();
        var attemptId = await SeedAttempt(_factory, userId, tenantId, "ceo_fraud", scorePercent: 25);

        var resp = await client.PostAsJsonAsync("/api/coaching/generate", new { attemptId });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<CoachingFeedbackDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal("ceo_fraud", dto!.ChallengeType);
        Assert.False(string.IsNullOrWhiteSpace(dto.Content));
    }

    [Fact]
    public async Task Generate_Without_Jwt_Returns_401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        var resp = await client.PostAsJsonAsync("/api/coaching/generate", new { attemptId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Generate_With_Attempt_Of_Other_User_Returns_404()
    {
        var (client, userId, tenantId) = _factory.CreateAuthenticatedClient();
        var otherUserId = Guid.NewGuid();
        // Attempt seedé pour un AUTRE user dans le même tenant.
        var attemptId = await SeedAttempt(_factory, otherUserId, tenantId, "phishing_ai", scorePercent: 0);

        var resp = await client.PostAsJsonAsync("/api/coaching/generate", new { attemptId });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetHistory_With_PageSize_Out_Of_Range_Returns_400()
    {
        var (client, _, _) = _factory.CreateAuthenticatedClient();

        var resp = await client.GetAsync("/api/coaching/me/history?page=1&pageSize=999");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static async Task<Guid> SeedAttempt(
        CoachingTestApiFactory factory, Guid userId, Guid tenantId, string contentType, int scorePercent)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var chId = Guid.NewGuid();
        db.Challenges.Add(new Challenge
        {
            Id = chId, TenantId = tenantId, ModuleId = Guid.NewGuid(),
            Type = "interactive", ContentType = contentType,
            Title = $"Test {contentType}", Instructions = "Description.", Status = "published",
            Points = 10, CreatedAt = DateTime.UtcNow,
        });
        var ccId = Guid.NewGuid();
        db.ChallengeCompletions.Add(new ChallengeCompletion
        {
            Id = ccId, UserId = userId, TenantId = tenantId, ChallengeId = chId,
            ChallengeTitle = contentType, ScorePercent = scorePercent,
            PointsEarned = scorePercent / 10, CompletedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return ccId;
    }
}

public class CoachingTestApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "ctf-api-test";
    public const string Audience = "ctf-web-test";
    public const string SigningKey = "test-key-must-be-at-least-32-characters-long-please";

    static CoachingTestApiFactory()
    {
        Environment.SetEnvironmentVariable("JWT_KEY", SigningKey);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:Key"] = SigningKey,
                ["ConnectionStrings:DefaultConnection"] = "Host=unused;Database=unused;Username=unused;Password=unused",
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["Coaching:Ollama:PrimaryModel"] = "mistral:7b",
                ["Coaching:Ollama:FallbackModel"] = "llama3.2:3b",
                ["Coaching:Ollama:TimeoutSeconds"] = "60",
                ["Coaching:Generation:MaxTokens"] = "500",
                ["Coaching:Generation:Temperature"] = "0.7",
                ["Coaching:Generation:MaxContentLength"] = "2000",
                ["Coaching:RateLimit:MaxGenerationsPerUserPerMinute"] = "5",
            });
        });

        builder.ConfigureServices(services =>
        {
            // InMemory DB dédiée à ces tests.
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("CoachingEndpointTests"));

            // Stub IOllamaLLMProvider : retourne toujours une réponse Ollama "fake"
            // — pas de dépendance réseau. On vérifie seulement le contrat HTTP.
            var llmDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOllamaLLMProvider));
            if (llmDescriptor is not null) services.Remove(llmDescriptor);
            services.AddScoped<IOllamaLLMProvider, StubLlmForEndpoints>();
        });
    }

    public (HttpClient client, Guid userId, Guid tenantId) CreateAuthenticatedClient()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = GenerateJwt(userId, tenantId, role: "user");

        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", $"jwt={token}");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        return (client, userId, tenantId);
    }

    private static string GenerateJwt(Guid userId, Guid tenantId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[]
            {
                new Claim("user_id", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim(ClaimTypes.Role, role),
            },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Provider stub : Ollama répond toujours OK avec un coaching fictif.</summary>
    private sealed class StubLlmForEndpoints : IOllamaLLMProvider
    {
        public Task<bool> IsAvailableAsync(CancellationToken ct) => Task.FromResult(true);
        public Task<bool> IsModelAvailableAsync(string modelName, CancellationToken ct) => Task.FromResult(true);
        public Task<bool> EnsureModelDownloadedAsync(string modelName, CancellationToken ct) => Task.FromResult(true);
        public Task<LLMResponse?> GenerateAsync(LLMRequest request, CancellationToken ct)
            => Task.FromResult<LLMResponse?>(new LLMResponse(
                "Coaching de test généré par stub. Tu progresses, continue.",
                100, 50, TimeSpan.FromMilliseconds(50), request.Model));
    }
}
