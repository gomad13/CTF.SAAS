using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CTF.Api.Contracts.RiskScore;
using CTF.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CTF.Api.Tests;

/// <summary>
/// Tests d'intégration HTTP du contrôleur <c>RiskScoreController</c>.
/// Couvrent les 3 cas demandés par le prompt :
/// 1. JWT valide → 200 + payload conforme au DTO ;
/// 2. Sans JWT → 401 ;
/// 3. months hors range → 400.
/// </summary>
public class RiskScoreEndpointTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public RiskScoreEndpointTests(TestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMyScore_WithValidJwt_Returns200_AndDto()
    {
        var (client, _, _) = _factory.CreateAuthenticatedClient();

        var resp = await client.GetAsync("/api/risk-score/me");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<RiskScoreDto>();
        Assert.NotNull(dto);
        // Aucune complétion seedée → score null (données insuffisantes), DTO valide.
        Assert.Null(dto!.Score);
        Assert.NotNull(dto.Components);
    }

    [Fact]
    public async Task GetMyScore_WithoutJwt_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        // Pas de cookie jwt → l'endpoint [Authorize] doit refuser.
        var resp = await client.GetAsync("/api/risk-score/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetMyHistory_WithMonthsOutOfRange_Returns400()
    {
        var (client, _, _) = _factory.CreateAuthenticatedClient();

        var resp = await client.GetAsync("/api/risk-score/me/history?months=99");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}

/// <summary>
/// Factory de test : InMemory DB, Hangfire désactivé via env "Testing",
/// JWT signing configurés pour pouvoir générer des tokens de test.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "ctf-api-test";
    public const string Audience = "ctf-web-test";
    public const string SigningKey = "test-key-must-be-at-least-32-characters-long-please";

    static TestApiFactory()
    {
        // Program.cs lit JWT_KEY directement via Environment.GetEnvironmentVariable
        // avant l'init du builder.Configuration → on doit la fixer en amont du host.
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
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remplace AppDbContext par InMemory DB pour ne pas dépendre de PostgreSQL.
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("RiskScoreEndpointTests"));
        });
    }

    public (HttpClient client, Guid userId, Guid tenantId) CreateAuthenticatedClient()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = GenerateJwt(userId, tenantId, role: "user");

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        // Le backend lit le JWT depuis le cookie HttpOnly nommé "jwt" (cf. Program.cs).
        client.DefaultRequestHeaders.Add("Cookie", $"jwt={token}");
        // Le middleware CSRF exige X-Requested-With sur les mutations ; on l'ajoute par défaut.
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
}
