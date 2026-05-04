using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CTF.Api.Contracts.Scenarios;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Models.Scenarios;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CTF.Api.Tests;

/// <summary>
/// Tests d'intégration HTTP des contrôleurs Pilier 1 (catalogue admin,
/// inbox employé, tracking anonyme, consentement). Hangfire désactivé via
/// l'environnement "Testing".
/// </summary>
public class ScenarioEndpointTests : IClassFixture<ScenarioTestApiFactory>
{
    private readonly ScenarioTestApiFactory _factory;
    public ScenarioEndpointTests(ScenarioTestApiFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Catalog_Without_Jwt_Returns_401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var resp = await client.GetAsync("/api/admin/scenarios/catalog");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Catalog_As_Admin_Returns_Seeded_Templates()
    {
        var (client, _, tenantId) = _factory.CreateAuthenticatedClient(role: "admin");
        await SeedTemplate(_factory, "test-cat-1");
        var resp = await client.GetAsync("/api/admin/scenarios/catalog");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await resp.Content.ReadFromJsonAsync<List<ScenarioCatalogItemDto>>(JsonOpts);
        Assert.NotNull(items);
        Assert.Contains(items!, x => x.ExternalId == "test-cat-1");
        _ = tenantId;
    }

    [Fact]
    public async Task EligibleSenders_Returns_Only_Consenting_Users_Of_Tenant()
    {
        var (client, _, tenantId) = _factory.CreateAuthenticatedClient(role: "admin");
        var consenting = await SeedUser(_factory, tenantId, true, "consent");
        var notConsenting = await SeedUser(_factory, tenantId, false, "noconsent");
        // Autre tenant : ne doit jamais apparaître.
        var otherTenant = Guid.NewGuid();
        await SeedUser(_factory, otherTenant, true, "othertenant");

        var resp = await client.GetAsync("/api/admin/scenarios/eligible-senders");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var list = await resp.Content.ReadFromJsonAsync<List<EligibleSenderDto>>(JsonOpts);
        Assert.NotNull(list);
        Assert.Contains(list!, x => x.UserId == consenting);
        Assert.DoesNotContain(list!, x => x.UserId == notConsenting);
    }

    [Fact]
    public async Task SenderConsent_Update_And_Read_Roundtrip()
    {
        var (client, userId, tenantId) = _factory.CreateAuthenticatedClient(role: "user");
        // L'user doit exister en DB pour que l'endpoint /api/users/me/sender-consent puisse le retrouver.
        await UpsertUser(_factory, userId, tenantId, false, "self");

        var put = await client.PutAsJsonAsync("/api/users/me/sender-consent", new { consentsToBeFictionalSender = true });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await client.GetAsync("/api/users/me/sender-consent");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var json = await get.Content.ReadAsStringAsync();
        Assert.Contains("\"consentsToBeFictionalSender\":true", json);
    }

    [Fact]
    public async Task TrackingPixel_Is_Anonymous_And_Returns_Gif()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        // Pas d'auth, pas d'X-Requested-With (GET non mutant).
        var resp = await client.GetAsync("/api/scenario-tracking/open/inexistant-token-1234.gif");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("image/gif", resp.Content.Headers.ContentType?.MediaType);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.Equal(43, bytes.Length); // 1x1 gif standard, 43 octets
    }

    [Fact]
    public async Task TrackingClick_Redirects_To_Frontend_Even_On_Unknown_Token()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/api/scenario-tracking/click/unknowntoken");
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        // Pas d'oracle : un token inconnu doit rediriger vers la racine du front, pas 404.
    }

    [Fact]
    public async Task Inbox_Me_Returns_Only_Own_Emails()
    {
        var (client, userId, tenantId) = _factory.CreateAuthenticatedClient(role: "user");
        await UpsertUser(_factory, userId, tenantId, false, "myinbox");
        await SeedScenarioEmail(_factory, tenantId, userId, "Mine", false);
        await SeedScenarioEmail(_factory, tenantId, Guid.NewGuid(), "Not mine", false);

        var resp = await client.GetAsync("/api/inbox/me");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var list = await resp.Content.ReadFromJsonAsync<List<InboxEmailListItemDto>>(JsonOpts);
        Assert.NotNull(list);
        Assert.Single(list!, x => x.Subject == "Mine");
        Assert.DoesNotContain(list!, x => x.Subject == "Not mine");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static async Task<Guid> SeedTemplate(ScenarioTestApiFactory factory, string externalId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var t = new ScenarioTemplate
        {
            Id = Guid.NewGuid(), ExternalId = externalId, Version = "1.0.0",
            Name = "Test", Description = "Test", Category = "ceo_fraud",
            Difficulty = "easy", DurationDays = 1, RawJson = "{\"timeline\":[],\"outcomes\":{},\"characters\":[]}",
        };
        // Idempotence : on n'ajoute que si externalId+version pas déjà là.
        if (!await db.ScenarioTemplates.AnyAsync(x => x.ExternalId == externalId && x.Version == "1.0.0"))
        {
            db.ScenarioTemplates.Add(t);
            await db.SaveChangesAsync();
        }
        return t.Id;
    }

    private static async Task<Guid> SeedUser(ScenarioTestApiFactory factory, Guid tenantId, bool consent, string label)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = id, TenantId = tenantId,
            Email = $"{label}-{Guid.NewGuid():N}@test.local",
            FirstName = label, LastName = "Test",
            Role = "user", IsActive = true,
            ConsentsToBeFictionalSender = consent,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task UpsertUser(ScenarioTestApiFactory factory, Guid userId, Guid tenantId, bool consent, string label)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (existing is null)
        {
            db.Users.Add(new User
            {
                Id = userId, TenantId = tenantId,
                Email = $"{label}-{Guid.NewGuid():N}@test.local",
                FirstName = label, LastName = "Test",
                Role = "user", IsActive = true,
                ConsentsToBeFictionalSender = consent,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.ConsentsToBeFictionalSender = consent;
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedScenarioEmail(ScenarioTestApiFactory factory, Guid tenantId, Guid recipientId, string subject, bool isAttack)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ScenarioEmails.Add(new ScenarioEmail
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientUserId = recipientId,
            FromName = "Test", FromEmail = "test@local",
            Subject = subject,
            BodyHtml = "<p>x</p>",
            TrackingToken = Guid.NewGuid().ToString("N"),
            IsAttackStep = isAttack,
            SentAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}

public class ScenarioTestApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "ctf-api-test";
    public const string Audience = "ctf-web-test";
    public const string SigningKey = "test-key-must-be-at-least-32-characters-long-please";

    static ScenarioTestApiFactory()
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
                ["FrontendUrl"] = "http://localhost:3000",
                ["ApiBaseUrl"] = "http://localhost:5202",
            });
        });
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("ScenarioEndpointTests"));

            // En env "Testing", Hangfire est désactivé donc IBackgroundJobClient
            // n'est pas enregistré → ScenarioEngine ne peut pas s'instancier.
            // On injecte un stub no-op qui capture les appels sans rien planifier.
            services.AddSingleton<IBackgroundJobClient, NoOpBackgroundJobClient>();
        });
    }

    private sealed class NoOpBackgroundJobClient : IBackgroundJobClient
    {
        public string Create(Job job, IState state) => Guid.NewGuid().ToString();
        public bool ChangeState(string jobId, IState state, string? expectedState) => true;
        public bool Delete(string jobId) => true;
        public bool Requeue(string jobId) => true;
    }

    public (HttpClient client, Guid userId, Guid tenantId) CreateAuthenticatedClient(string role)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = GenerateJwt(userId, tenantId, role);
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
            issuer: Issuer, audience: Audience,
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
}
