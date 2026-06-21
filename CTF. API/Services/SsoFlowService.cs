using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CTF.Api.Data;
using CTF.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CTF.Api.Services;

/// <summary>
/// Logique partagée SSO : résolution tenant → upsert user → JWT + refresh token + cookies → redirection.
/// Appelée par SsoController (callbacks Google/MS) et TestOAuthController (endpoint Dev).
/// </summary>
public class SsoFlowService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly TenantResolutionService _resolver;
    private readonly ILogger<SsoFlowService> _logger;

    public SsoFlowService(AppDbContext db, IConfiguration config, IWebHostEnvironment env,
        TenantResolutionService resolver, ILogger<SsoFlowService> logger)
    {
        _db = db; _config = config; _env = env; _resolver = resolver; _logger = logger;
    }

    public async Task<IActionResult> ProcessAsync(
        HttpContext httpContext,
        string provider, string email, string firstName, string lastName,
        string? subjectId, string? avatarUrl, string returnUrl)
    {
        email = (email ?? "").Trim().ToLowerInvariant();

        TenantResolutionService.Resolution resolution;
        try { resolution = await _resolver.ResolveForEmailAsync(email); }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[SSO] TenantResolution failed for {Email}", MaskEmail(email));
            return new ObjectResult(new { error = "demo_tenant_missing", message = "Demo tenant missing — contact admin" })
            { StatusCode = 500 };
        }
        catch (ArgumentException)
        {
            return new RedirectResult($"{FrontendUrl()}/login?error=invalid_email");
        }

        if (resolution.ProvisioningDisabled)
        {
            _logger.LogWarning("[SSO] Provisioning disabled for domain={Domain} — blocked", resolution.Domain);
            return new RedirectResult($"{FrontendUrl()}/login?error=provisioning_disabled");
        }

        var providerKey = provider.ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == email
            || (providerKey == "google" && u.GoogleSubjectId != null && u.GoogleSubjectId == subjectId)
            || (providerKey == "microsoft" && u.MicrosoftSubjectId != null && u.MicrosoftSubjectId == subjectId));

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = $"{firstName} {lastName}".Trim(),
                Role = "user",
                TenantId = resolution.TenantId,
                IsActive = true,
                PasswordHash = null,
                AvatarUrl = avatarUrl,
                AuthProvider = providerKey,
                GoogleSubjectId = providerKey == "google" ? subjectId : null,
                MicrosoftSubjectId = providerKey == "microsoft" ? subjectId : null,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };
            _db.Users.Add(user);
            _logger.LogInformation("[SSO] New user via {Provider}: {Email} → tenant={TenantId} (source={Source})",
                provider, MaskEmail(email), resolution.TenantId, resolution.Source);
        }
        else
        {
            if (!user.IsActive)
                return new RedirectResult($"{FrontendUrl()}/login?error=account_disabled");

            user.LastLoginAt = DateTime.UtcNow;
            user.LastActivityAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(firstName)) user.FirstName = firstName;
            if (string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(lastName)) user.LastName = lastName;
            if (!string.IsNullOrEmpty(avatarUrl)) user.AvatarUrl = avatarUrl;

            if (providerKey == "google" && !string.IsNullOrEmpty(subjectId) && string.IsNullOrEmpty(user.GoogleSubjectId))
                user.GoogleSubjectId = subjectId;
            if (providerKey == "microsoft" && !string.IsNullOrEmpty(subjectId) && string.IsNullOrEmpty(user.MicrosoftSubjectId))
                user.MicrosoftSubjectId = subjectId;

            var providers = new List<string>();
            if (!string.IsNullOrEmpty(user.GoogleSubjectId)) providers.Add("google");
            if (!string.IsNullOrEmpty(user.MicrosoftSubjectId)) providers.Add("microsoft");
            if (!string.IsNullOrEmpty(user.PasswordHash)) providers.Add("password");
            user.AuthProvider = providers.Count > 1 ? "multi" : providers.FirstOrDefault() ?? providerKey;
        }

        // Enforcement par tenant : si l'entreprise a désactivé ce provider SSO, on refuse la connexion
        // (rend effectif le toggle « Paramètres entreprise → SSO »). Un nouvel utilisateur non encore
        // persisté n'est donc pas créé.
        var ssoAllowed = await _db.Tenants
            .Where(t => t.Id == user.TenantId)
            .Select(t => providerKey == "google" ? t.GoogleSsoEnabled : t.MicrosoftSsoEnabled)
            .FirstOrDefaultAsync();
        if (!ssoAllowed)
        {
            _logger.LogWarning("[SSO] {Provider} désactivé pour tenant={TenantId} — refusé", provider, user.TenantId);
            return new RedirectResult($"{FrontendUrl()}/login?error=sso_disabled_for_tenant");
        }

        await _db.SaveChangesAsync();

        var isSuperAdmin = await _db.SuperAdmins.AnyAsync(sa => sa.IsActive && sa.Email.ToLower() == email);
        var effectiveRole = isSuperAdmin ? "SuperAdmin" : user.Role;

        var jwt = BuildJwt(user.TenantId, user.Id, effectiveRole);

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var oldTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var old in oldTokens) { old.IsRevoked = true; old.RevokedAt = DateTime.UtcNow; }

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
            CreatedByIp = ip,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        var isDev = _env.IsDevelopment();
        httpContext.Response.Cookies.Append("jwt", jwt, new CookieOptions
        {
            HttpOnly = true, Secure = !isDev, SameSite = SameSiteMode.Lax, Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(15),
        });
        httpContext.Response.Cookies.Append("refresh_token", refreshToken.Token, new CookieOptions
        {
            HttpOnly = true, Secure = !isDev, SameSite = SameSiteMode.Lax, Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        });
        httpContext.Response.Cookies.Append("user_role", effectiveRole, new CookieOptions
        {
            HttpOnly = false, Secure = !isDev, SameSite = SameSiteMode.Lax, Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        });
        httpContext.Response.Cookies.Delete("ext_auth");

        // Bannière Demo côté front : cookie simple lu au dashboard
        if (resolution.IsDemoFallback)
        {
            httpContext.Response.Cookies.Append("sso_demo_fallback", "1", new CookieOptions
            {
                HttpOnly = false, Secure = !isDev, SameSite = SameSiteMode.Lax, Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(30),
            });
        }

        _logger.LogInformation("[SSO] {Provider} login OK: userId={UserId} role={Role} demo_fallback={Demo}",
            provider, user.Id, effectiveRole, resolution.IsDemoFallback);

        var destination = effectiveRole switch
        {
            "SuperAdmin" => "/superadmin",
            var r when string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase) => "/admin/dashboard",
            _ => returnUrl,
        };
        return new RedirectResult($"{FrontendUrl()}{destination}");
    }

    private string FrontendUrl() => _config["FrontendUrl"] ?? "http://localhost:3000";

    private string BuildJwt(Guid tenantId, Guid userId, string role)
    {
        var section = _config.GetSection("Jwt");
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? section["Key"]!;
        var claims = new List<Claim>
        {
            new("tenant_id", tenantId.ToString()),
            new("user_id", userId.ToString()),
            new(ClaimTypes.Role, role),
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: section["Issuer"]!,
            audience: section["Audience"]!,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "***";
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var domain = email[(at + 1)..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***@{domain}";
    }
}
