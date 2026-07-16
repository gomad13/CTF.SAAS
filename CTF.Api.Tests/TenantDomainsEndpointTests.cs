using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CTF.Api.Services;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using CTF.Api.Data;

namespace CTF.Api.Tests;

/// <summary>
/// Tests d'intégration HTTP de la vérification de domaine par tenant (PASSE 1).
/// La résolution DNS est remplacée par un fake pilotable (pas de vraie requête réseau) ;
/// la blacklist des domaines publics et la génération de token restent réelles.
/// </summary>
public class TenantDomainsEndpointTests : IClassFixture<DomainTestApiFactory>
{
    private readonly DomainTestApiFactory _factory;
    public TenantDomainsEndpointTests(DomainTestApiFactory factory) => _factory = factory;
    private static readonly JsonSerializerOptions J = new() { PropertyNameCaseInsensitive = true };

    // ── Blacklist ────────────────────────────────────────────────────────────
    [Fact]
    public async Task Declare_PublicDomain_Is_Refused()
    {
        var (client, _, _) = _factory.CreateAuthenticatedClient("admin");
        var resp = await client.PostAsJsonAsync("/api/tenant/domains", new { domain = "gmail.com" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Declare_InvalidFormat_Is_Refused()
    {
        var (client, _, _) = _factory.CreateAuthenticatedClient("admin");
        var resp = await client.PostAsJsonAsync("/api/tenant/domains", new { domain = "pas un domaine" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Déclaration + affichage du TXT ────────────────────────────────────────
    [Fact]
    public async Task Declare_Then_List_Shows_Pending_With_Txt_Record()
    {
        var (client, _, _) = _factory.CreateAuthenticatedClient("admin");
        var domain = $"clinique-{Guid.NewGuid():N}.fr";

        var create = await client.PostAsJsonAsync("/api/tenant/domains", new { domain });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var dto = await create.Content.ReadFromJsonAsync<JsonElement>(J);
        Assert.False(dto.GetProperty("isVerified").GetBoolean());
        Assert.Equal("pending", dto.GetProperty("status").GetString());
        Assert.Equal($"_sentys-verification.{domain}", dto.GetProperty("dnsRecordName").GetString());
        Assert.StartsWith("sentys-verify=", dto.GetProperty("dnsRecordValue").GetString()!);

        var list = await (await client.GetAsync("/api/tenant/domains")).Content.ReadFromJsonAsync<List<JsonElement>>(J);
        Assert.Contains(list!, d => d.GetProperty("domain").GetString() == domain);
    }

    // ── Unicité : domaine déjà pris par un AUTRE tenant ───────────────────────
    [Fact]
    public async Task Declare_Domain_Taken_By_Another_Tenant_Is_Refused()
    {
        var domain = $"hopital-{Guid.NewGuid():N}.fr";
        var (clientA, _, _) = _factory.CreateAuthenticatedClient("admin");
        var (clientB, _, _) = _factory.CreateAuthenticatedClient("admin"); // autre tenant (nouveau JWT)

        var a = await clientA.PostAsJsonAsync("/api/tenant/domains", new { domain });
        Assert.Equal(HttpStatusCode.Created, a.StatusCode);

        var b = await clientB.PostAsJsonAsync("/api/tenant/domains", new { domain });
        Assert.Equal(HttpStatusCode.Conflict, b.StatusCode); // 409 : appartient à une autre organisation
    }

    // ── Vérification DNS (fake piloté) ────────────────────────────────────────
    [Fact]
    public async Task Verify_Without_Txt_Fails_Cleanly()
    {
        _factory.Fake.NextResult = DomainVerificationResult.RecordNotFound;
        var (client, id) = await DeclareOne();

        var resp = await client.PostAsync($"/api/tenant/domains/{id}/verify", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var res = await resp.Content.ReadFromJsonAsync<JsonElement>(J);
        Assert.False(res.GetProperty("isVerified").GetBoolean());
        Assert.Equal("record_not_found", res.GetProperty("result").GetString());
    }

    [Fact]
    public async Task Verify_Wrong_Token_Fails()
    {
        _factory.Fake.NextResult = DomainVerificationResult.TokenMismatch;
        var (client, id) = await DeclareOne();

        var res = await (await client.PostAsync($"/api/tenant/domains/{id}/verify", null)).Content.ReadFromJsonAsync<JsonElement>(J);
        Assert.False(res.GetProperty("isVerified").GetBoolean());
        Assert.Equal("token_mismatch", res.GetProperty("result").GetString());
    }

    [Fact]
    public async Task Verify_Dns_Unavailable_Is_Not_Treated_As_Invalid()
    {
        _factory.Fake.NextResult = DomainVerificationResult.DnsUnavailable;
        var (client, id) = await DeclareOne();

        var res = await (await client.PostAsync($"/api/tenant/domains/{id}/verify", null)).Content.ReadFromJsonAsync<JsonElement>(J);
        Assert.False(res.GetProperty("isVerified").GetBoolean());
        Assert.Equal("dns_unavailable", res.GetProperty("result").GetString()); // reste en attente, réessayable
    }

    [Fact]
    public async Task Verify_Success_Marks_Domain_Verified()
    {
        _factory.Fake.NextResult = DomainVerificationResult.Verified;
        var (client, id) = await DeclareOne();

        var res = await (await client.PostAsync($"/api/tenant/domains/{id}/verify", null)).Content.ReadFromJsonAsync<JsonElement>(J);
        Assert.True(res.GetProperty("isVerified").GetBoolean());
        Assert.Equal("verified", res.GetProperty("result").GetString());

        // La liste reflète l'état vérifié, sans exposer de TXT à poser.
        var list = await (await client.GetAsync("/api/tenant/domains")).Content.ReadFromJsonAsync<List<JsonElement>>(J);
        var d = list!.First(x => x.GetProperty("id").GetGuid() == id);
        Assert.Equal("verified", d.GetProperty("status").GetString());
        Assert.Equal("", d.GetProperty("dnsRecordValue").GetString());
    }

    // ── Autorisation serveur : membre non-admin refusé ────────────────────────
    [Fact]
    public async Task Member_NonAdmin_Is_Forbidden_On_All_Operations()
    {
        var (member, _, _) = _factory.CreateAuthenticatedClient("user");

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/tenant/domains")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await member.PostAsJsonAsync("/api/tenant/domains", new { domain = "exemple-test.fr" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await member.PostAsync($"/api/tenant/domains/{Guid.NewGuid()}/verify", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await member.DeleteAsync($"/api/tenant/domains/{Guid.NewGuid()}")).StatusCode);
    }

    // ── Rate limiting sur verify ──────────────────────────────────────────────
    [Fact]
    public async Task Verify_Is_Rate_Limited_After_Five_Attempts()
    {
        _factory.Fake.NextResult = DomainVerificationResult.RecordNotFound;
        var (client, id) = await DeclareOne();

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/tenant/domains/{id}/verify", null)).StatusCode);

        var sixth = await client.PostAsync($"/api/tenant/domains/{id}/verify", null);
        Assert.Equal((HttpStatusCode)429, sixth.StatusCode); // TooManyRequests
    }

    // ── Retrait ───────────────────────────────────────────────────────────────
    [Fact]
    public async Task Remove_Deletes_Domain()
    {
        var (client, id) = await DeclareOne();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/tenant/domains/{id}")).StatusCode);
        var list = await (await client.GetAsync("/api/tenant/domains")).Content.ReadFromJsonAsync<List<JsonElement>>(J);
        Assert.DoesNotContain(list!, d => d.GetProperty("id").GetGuid() == id);
    }

    private async Task<(HttpClient client, Guid id)> DeclareOne()
    {
        var (client, _, _) = _factory.CreateAuthenticatedClient("admin");
        var domain = $"org-{Guid.NewGuid():N}.fr";
        var create = await client.PostAsJsonAsync("/api/tenant/domains", new { domain });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var dto = await create.Content.ReadFromJsonAsync<JsonElement>(J);
        return (client, dto.GetProperty("id").GetGuid());
    }
}

/// <summary>Fabrique de test dédiée : BDD InMemory isolée + fake DNS pilotable.</summary>
public class DomainTestApiFactory : WebApplicationFactory<Program>
{
    private const string SigningKey = "test-key-must-be-at-least-32-characters-long-please";
    private const string Issuer = "ctf-api-test";
    private const string Audience = "ctf-web-test";

    public FakeDomainVerificationService Fake { get; } = new();

    static DomainTestApiFactory() => Environment.SetEnvironmentVariable("JWT_KEY", SigningKey);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer, ["Jwt:Audience"] = Audience, ["Jwt:Key"] = SigningKey,
                ["ConnectionStrings:DefaultConnection"] = "Host=unused;Database=unused;Username=unused;Password=unused",
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["FrontendUrl"] = "http://localhost:3000",
            });
        });
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("DomainVerificationTests"));

            // Remplace la résolution DNS réelle par le fake pilotable.
            var dns = services.SingleOrDefault(d => d.ServiceType == typeof(IDomainVerificationService));
            if (dns is not null) services.Remove(dns);
            services.AddSingleton<IDomainVerificationService>(Fake);

            services.AddSingleton<IBackgroundJobClient, NoOpBackgroundJobClient>();
        });
    }

    public (HttpClient client, Guid userId, Guid tenantId) CreateAuthenticatedClient(string role)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience,
            new[]
            {
                new Claim("user_id", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim(ClaimTypes.Role, role),
            },
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(15), creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", $"jwt={jwt}");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        return (client, userId, tenantId);
    }

    private sealed class NoOpBackgroundJobClient : IBackgroundJobClient
    {
        public string Create(Job job, IState state) => Guid.NewGuid().ToString();
        public bool ChangeState(string jobId, IState state, string? expectedState) => true;
        public bool Delete(string jobId) => true;
        public bool Requeue(string jobId) => true;
    }
}

/// <summary>Fake : résultat DNS pilotable, mais blacklist/format/token RÉELS.</summary>
public sealed class FakeDomainVerificationService : IDomainVerificationService
{
    private readonly DomainVerificationService _real =
        new(new ConfigurationBuilder().Build(), NullLogger<DomainVerificationService>.Instance);

    public DomainVerificationResult NextResult { get; set; } = DomainVerificationResult.RecordNotFound;

    public bool IsPublicDomain(string domain) => _real.IsPublicDomain(domain);
    public bool IsValidDomainFormat(string domain) => _real.IsValidDomainFormat(domain);
    public string GenerateToken() => _real.GenerateToken();
    public string RecordName(string domain) => _real.RecordName(domain);
    public string RecordValue(string token) => _real.RecordValue(token);
    public Task<DomainVerificationResult> VerifyTxtAsync(string domain, string expectedToken, CancellationToken ct = default)
        => Task.FromResult(NextResult);
}
