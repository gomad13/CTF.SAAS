using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CTF.Api.Data;
using CTF.Api.Models;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CTF.Api.Tests;

/// <summary>
/// M2 (mélange des options) + M3 (5 variantes + tirage aléatoire) : tests d'intégration
/// HTTP du contrôleur interactif. Vérifie le tirage de variante, le mélange des choix,
/// et que le scoring valide la bonne réponse de la variante effectivement affichée.
/// </summary>
public class InteractiveVariantsTests : IClassFixture<InteractiveTestApiFactory>
{
    private readonly InteractiveTestApiFactory _factory;
    public InteractiveVariantsTests(InteractiveTestApiFactory factory) => _factory = factory;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private const string BaseContent =
        "{\"question\":\"BASE\",\"choices\":[{\"id\":\"x\",\"label\":\"x\",\"is_correct\":true,\"explanation\":\"x\"}]}";

    // Variante i : question "Q{i}", 4 choix, le choix correct a l'id "v{i}_ok".
    private static string BuildVariants(int n)
    {
        var variants = new List<string>();
        for (var i = 0; i < n; i++)
        {
            variants.Add(
                "{\"question\":\"Q" + i + "\",\"choices\":[" +
                "{\"id\":\"v" + i + "_ok\",\"label\":\"Bonne\",\"is_correct\":true,\"explanation\":\"ok\"}," +
                "{\"id\":\"v" + i + "_b\",\"label\":\"b\",\"is_correct\":false,\"explanation\":\"non\"}," +
                "{\"id\":\"v" + i + "_c\",\"label\":\"c\",\"is_correct\":false,\"explanation\":\"non\"}," +
                "{\"id\":\"v" + i + "_d\",\"label\":\"d\",\"is_correct\":false,\"explanation\":\"non\"}" +
                "]}");
        }
        return "[" + string.Join(",", variants) + "]";
    }

    private async Task<Guid> SeedMultichoice(Guid tenantId, string? variantsJson, string contentJson)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.NewGuid();
        db.Challenges.Add(new Challenge
        {
            Id = id, TenantId = tenantId, ModuleId = Guid.NewGuid(),
            Type = "interactive", ContentType = "multichoice",
            Title = "Test QCM", Instructions = "x", Status = "published", Points = 100,
            ContentJson = contentJson, VariantsJson = variantsJson, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task Content_Draws_A_Variant_In_Range_And_Matching_Question()
    {
        var (client, _, tenantId) = _factory.CreateAuthenticatedClient("user");
        var id = await SeedMultichoice(tenantId, BuildVariants(5), BaseContent);

        var seen = new HashSet<int>();
        for (var k = 0; k < 25; k++)
        {
            var doc = await GetContent(client, id);
            var vi = doc.RootElement.GetProperty("variantIndex").GetInt32();
            Assert.InRange(vi, 0, 4);
            var q = doc.RootElement.GetProperty("content").GetProperty("question").GetString();
            Assert.Equal($"Q{vi}", q); // le contenu servi correspond bien à la variante tirée
            seen.Add(vi);
        }
        // Tirage aléatoire : sur 25 appels on doit voir plusieurs variantes différentes.
        Assert.True(seen.Count > 1, $"variantes vues = {seen.Count}");
    }

    [Fact]
    public async Task Content_Hides_IsCorrect_And_Shuffles_Choices()
    {
        var (client, _, tenantId) = _factory.CreateAuthenticatedClient("user");
        // Pas de variantes : variante figée → on isole l'effet du mélange (M2).
        var content =
            "{\"question\":\"Q\",\"choices\":[" +
            "{\"id\":\"a\",\"label\":\"A\",\"is_correct\":true,\"explanation\":\"e\"}," +
            "{\"id\":\"b\",\"label\":\"B\",\"is_correct\":false,\"explanation\":\"e\"}," +
            "{\"id\":\"c\",\"label\":\"C\",\"is_correct\":false,\"explanation\":\"e\"}," +
            "{\"id\":\"d\",\"label\":\"D\",\"is_correct\":false,\"explanation\":\"e\"}," +
            "{\"id\":\"e\",\"label\":\"E\",\"is_correct\":false,\"explanation\":\"e\"}]}";
        var id = await SeedMultichoice(tenantId, null, content);

        var orders = new HashSet<string>();
        for (var k = 0; k < 15; k++)
        {
            var doc = await GetContent(client, id);
            var choices = doc.RootElement.GetProperty("content").GetProperty("choices");
            // is_correct ne doit jamais fuiter vers le client
            foreach (var c in choices.EnumerateArray())
                Assert.False(c.TryGetProperty("is_correct", out _), "is_correct ne doit pas être exposé");
            var order = string.Join(",", choices.EnumerateArray().Select(c => c.GetProperty("id").GetString()));
            orders.Add(order);
        }
        Assert.True(orders.Count > 1, "les choix doivent être mélangés à l'affichage (ordres distincts)");
    }

    [Fact]
    public async Task Submit_Validates_The_Displayed_Variant()
    {
        var (client, _, tenantId) = _factory.CreateAuthenticatedClient("user");
        var id = await SeedMultichoice(tenantId, BuildVariants(5), BaseContent);

        var doc = await GetContent(client, id);
        var vi = doc.RootElement.GetProperty("variantIndex").GetInt32();

        // Bonne réponse de la variante affichée → 100 %
        var good = await client.PostAsJsonAsync($"/api/challenges/interactive/{id}/submit-multichoice",
            new { selectedChoices = new[] { $"v{vi}_ok" }, variantIndex = vi });
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        var goodRes = await good.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(100, goodRes.GetProperty("scorePercent").GetInt32());

        // Mauvaise réponse (id d'une autre variante) → score < 100
        var wrongId = vi == 0 ? "v1_ok" : "v0_ok";
        var bad = await client.PostAsJsonAsync($"/api/challenges/interactive/{id}/submit-multichoice",
            new { selectedChoices = new[] { wrongId }, variantIndex = vi });
        var badRes = await bad.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(badRes.GetProperty("scorePercent").GetInt32() < 100);
    }

    private static async Task<JsonDocument> GetContent(HttpClient client, Guid id)
    {
        var resp = await client.GetAsync($"/api/challenges/interactive/{id}/content");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    }
}

public class InteractiveTestApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-key-must-be-at-least-32-characters-long-please";
    public const string Issuer = "ctf-api-test";
    public const string Audience = "ctf-web-test";

    // Base InMemory ISOLÉE par instance de factory : IClassFixture crée une instance par classe
    // de test, et les BDD InMemory sont indexées par nom au niveau process. Un nom fixe faisait
    // partager la même base entre classes → contamination croisée sous exécution parallèle xUnit.
    private readonly string _dbName = "interactive-" + Guid.NewGuid().ToString("N");

    static InteractiveTestApiFactory() => Environment.SetEnvironmentVariable("JWT_KEY", SigningKey);

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
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
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
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
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
