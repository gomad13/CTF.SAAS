using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration      _config;
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache        _cache;

    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

    // cache key prefix for password-reset tokens
    private const string ResetPrefix = "pwd_reset_";

    private record ResetTokenData(Guid UserId, Guid TenantId, DateTime ExpiresAt);

    public AuthController(IConfiguration config, IWebHostEnvironment env, IMemoryCache cache)
    {
        _config = config;
        _env    = env;
        _cache  = cache;
    }

    // ── Dev token (DEV only) ────────────────────────────────────────────────
    public record DevTokenRequest(Guid TenantId, Guid UserId, string Role);

    [HttpPost("dev-token")]
    public IActionResult DevToken([FromBody] DevTokenRequest req)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var tokenString = BuildJwt(req.TenantId, req.UserId, req.Role);
        SetAuthCookie(tokenString);
        return Ok(new { message = "Token émis." });
    }

    // ── Login ───────────────────────────────────────────────────────────────
    public record LoginRequest(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest req,
        [FromServices] AppDbContext db)
    {
        // ── Rate limiting : 5 tentatives / IP / minute ──────────────────────
        var ip       = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"login_attempt_{ip}";
        var attempts = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });

        if (attempts >= 5)
            return StatusCode(429, new { error = "Trop de tentatives, attendez 1 minute." });

        _cache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(1));

        // ── Validation de base ──────────────────────────────────────────────
        var normalizedEmail = req.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalizedEmail))
            return BadRequest(new { error = "Email requis." });

        // ── Recherche par email uniquement (le rôle vient de la base) ───────
        var user = await db.Users
            .Where(u => u.Email == normalizedEmail)
            .FirstOrDefaultAsync();

        // Anti-timing : même délai si email introuvable ou mauvais mot de passe
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            await Task.Delay(300);
            return Unauthorized(new { error = "Email ou mot de passe incorrect." });
        }

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            await Task.Delay(300);
            return Unauthorized(new { error = "Email ou mot de passe incorrect." });
        }

        if (!user.IsActive)
        {
            await Task.Delay(300);
            return Unauthorized(new { error = "Votre compte a été désactivé." });
        }

        // ── Succès : reset du compteur, mise à jour du lastLogin ────────────
        _cache.Remove(cacheKey);
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // ── Détection SuperAdmin (table dédiée) ─────────────────────────────
        var isSuperAdmin = await db.SuperAdmins
            .AnyAsync(sa => sa.IsActive && sa.Email.ToLower() == user.Email.ToLower());

        var effectiveRole = isSuperAdmin ? "SuperAdmin" : user.Role;

        // ── Émettre JWT court + refresh token + cookie rôle ────────────────
        var token = BuildJwt(user.TenantId, user.Id, effectiveRole);
        SetAuthCookie(token);
        SetRoleCookie(effectiveRole);

        var ipAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var refresh = await IssueRefreshTokenAsync(db, user.Id, ipAddr);
        SetRefreshCookie(refresh.Token);

        // ── Redirection selon le rôle effectif ──────────────────────────────
        var redirectTo = effectiveRole switch
        {
            "SuperAdmin"                                                      => "/superadmin",
            var r when string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase) => "/admin/dashboard",
            _                                                                 => "/dashboard",
        };

        return Ok(new { role = effectiveRole, redirectTo });
    }

    // ── Register ────────────────────────────────────────────────────────────
    public record RegisterRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        Guid? TenantId
    );

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest req,
        [FromServices] AppDbContext db,
        [FromServices] IConfiguration config)
    {
        // Bêta DSI : inscription publique fermée par défaut.
        // Réactivable via appsettings : "Beta:RegistrationOpen": true ou env BETA__REGISTRATIONOPEN=true.
        var registrationOpen = config.GetValue<bool>("Beta:RegistrationOpen", false);
        if (!registrationOpen)
        {
            return StatusCode(403, new
            {
                error = "L'inscription publique est actuellement fermée. " +
                        "Si votre organisation est partenaire de Viper, contactez votre administrateur. " +
                        "Pour rejoindre la bêta, écrivez à contact@viper.fr."
            });
        }

        if (!IsPasswordStrong(req.Password))
            return BadRequest(new
            {
                error = "Le mot de passe doit contenir au moins 8 caractères, " +
                        "une majuscule, une minuscule, un chiffre et un caractère spécial."
            });

        var tenantId = req.TenantId ?? DemoTenantId;

        var tenantExists = await db.Tenants.AnyAsync(t => t.Id == tenantId);
        if (!tenantExists)
            return BadRequest(new { error = "Tenant introuvable. Vérifiez votre TenantId." });

        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var emailTaken = await db.Users.AnyAsync(u =>
            u.TenantId == tenantId && u.Email == normalizedEmail);
        if (emailTaken)
            return Conflict(new { error = "Cet email est déjà utilisé dans cette organisation." });

        var user = new User
        {
            Id          = Guid.NewGuid(),
            TenantId    = tenantId,
            FirstName   = req.FirstName.Trim(),
            LastName    = req.LastName.Trim(),
            Email       = normalizedEmail,
            DisplayName = $"{req.FirstName.Trim()} {req.LastName.Trim()}",
            Role        = "user",
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Auto-assign demo paths to all new users (regardless of tenant)
        {
            var demoPaths = new[]
            {
                Guid.Parse("00000000-0000-0000-0000-000000000001"), // Demo
                Guid.Parse("20000000-0000-0000-0000-000000000000"), // Medical
            };
            foreach (var pathId in demoPaths)
            {
                if (!await db.Paths.AnyAsync(p => p.Id == pathId)) continue;
                if (await db.Assignments.AnyAsync(a => a.UserId == user.Id && a.PathId == pathId)) continue;

                db.Assignments.Add(new Assignment
                {
                    Id          = Guid.NewGuid(),
                    TenantId    = tenantId,
                    UserId      = user.Id,
                    PathId      = pathId,
                    Status      = Assignment.Statuses.Assigned,
                    AssignedBy  = user.Id,
                    AssignedAt  = DateTime.UtcNow,
                    UpdatedAt   = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        var tokenString = BuildJwt(tenantId, user.Id, user.Role);
        SetAuthCookie(tokenString);

        return Ok(new { message = "Compte créé avec succès." });
    }

    // ── Forgot password ─────────────────────────────────────────────────────
    public record ForgotPasswordRequest(string Email, Guid? TenantId);

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest req,
        [FromServices] AppDbContext db)
    {
        // Réponse identique quelle que soit l'issue (sécurité anti-énumération)
        const string safeMsg = "Si cet email existe, un lien de réinitialisation a été envoyé.";

        var normalizedEmail = req.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalizedEmail))
            return Ok(new { message = safeMsg });

        var tenantId = req.TenantId ?? DemoTenantId;

        var user = await db.Users
            .Where(u => u.Email == normalizedEmail && u.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (user is not null)
        {
            var token     = Guid.NewGuid().ToString("N");   // 32 hex chars
            var expiresAt = DateTime.UtcNow.AddHours(1);

            _cache.Set(
                ResetPrefix + token,
                new ResetTokenData(user.Id, user.TenantId, expiresAt),
                absoluteExpiration: new DateTimeOffset(expiresAt)
            );

            // En DEV : lien dans les logs — remplacer par SMTP en production
            var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?token={token}";
            Console.WriteLine($"[DEV] Lien de réinitialisation pour {normalizedEmail} : {resetLink}");
        }

        return Ok(new { message = safeMsg });
    }

    // ── Reset password ──────────────────────────────────────────────────────
    public record ResetPasswordRequest(string Token, string NewPassword);

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest req,
        [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return BadRequest(new { error = "Token manquant." });

        if (!_cache.TryGetValue(ResetPrefix + req.Token, out ResetTokenData? data) || data is null)
            return BadRequest(new { error = "Lien invalide ou expiré. Demandez un nouveau lien." });

        if (data.ExpiresAt < DateTime.UtcNow)
        {
            _cache.Remove(ResetPrefix + req.Token);
            return BadRequest(new { error = "Lien expiré. Demandez un nouveau lien." });
        }

        if (!IsPasswordStrong(req.NewPassword))
            return BadRequest(new
            {
                error = "Le mot de passe doit contenir au moins 8 caractères, " +
                        "une majuscule, une minuscule, un chiffre et un caractère spécial."
            });

        var user = await db.Users.FindAsync(data.UserId);
        if (user is null)
            return BadRequest(new { error = "Utilisateur introuvable." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();

        _cache.Remove(ResetPrefix + req.Token);

        return Ok(new { message = "Mot de passe modifié avec succès." });
    }

    // ── Me ──────────────────────────────────────────────────────────────────
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Me([FromServices] AppDbContext db)
    {
        var userId   = User.GetUserId();
        var tenantId = User.GetTenantId();

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.Role, u.TenantId })
            .SingleOrDefaultAsync();

        if (user is null)
            return Unauthorized(new { error = "Utilisateur introuvable." });

        var tenant = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.Name })
            .SingleOrDefaultAsync();

        // Lire le rôle depuis le JWT (peut être "SuperAdmin" alors que user.Role en BDD = "user")
        var roleFromJwt = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? user.Role;

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            Role       = roleFromJwt,
            tenantId   = user.TenantId,
            tenantName = tenant?.Name ?? "Inconnu"
        });
    }

    // ── Change password ─────────────────────────────────────────────────────
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [HttpPut("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest req,
        [FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized(new { error = "Utilisateur introuvable." });

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { error = "Mot de passe actuel incorrect." });

        if (!IsPasswordStrong(req.NewPassword))
            return BadRequest(new { error = "Le mot de passe doit contenir au moins 8 caractères, une majuscule, une minuscule, un chiffre et un caractère spécial." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ── Refresh ────────────────────────────────────────────────────────────
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromServices] AppDbContext db)
    {
        var refreshTokenValue = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshTokenValue))
            return Unauthorized(new { error = "Refresh token manquant." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var rt = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshTokenValue);

        if (rt is null || rt.IsRevoked || rt.ExpiresAt <= DateTime.UtcNow)
            return Unauthorized(new { error = "Refresh token invalide ou expiré." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == rt.UserId);
        if (user is null || !user.IsActive)
            return Unauthorized(new { error = "Compte introuvable ou désactivé." });

        // Recalculer le rôle effectif (SuperAdmin)
        var isSuperAdmin = await db.SuperAdmins
            .AnyAsync(sa => sa.IsActive && sa.Email.ToLower() == user.Email.ToLower());
        var effectiveRole = isSuperAdmin ? "SuperAdmin" : user.Role;

        // Nouveaux tokens (rotation)
        var newJwt = BuildJwt(user.TenantId, user.Id, effectiveRole);
        SetAuthCookie(newJwt);
        SetRoleCookie(effectiveRole);

        var newRefresh = await IssueRefreshTokenAsync(db, user.Id, ip);
        SetRefreshCookie(newRefresh.Token);

        return Ok(new { success = true });
    }

    // ── Logout all (placeholder) ────────────────────────────────────────────
    [HttpPost("logout-all")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult LogoutAll()
    {
        return Ok(new { success = true });
    }

    // ── Logout ──────────────────────────────────────────────────────────────
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Logout([FromServices] AppDbContext db)
    {
        // Révoquer le refresh token actif
        var refreshTokenValue = Request.Cookies["refresh_token"];
        if (!string.IsNullOrEmpty(refreshTokenValue))
        {
            var rt = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshTokenValue && !t.IsRevoked);
            if (rt != null)
            {
                rt.IsRevoked = true;
                rt.RevokedAt = DateTime.UtcNow;
                rt.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                await db.SaveChangesAsync();
            }
        }

        var baseOpts = new CookieOptions
        {
            HttpOnly = true,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path     = "/"
        };
        Response.Cookies.Delete("jwt", baseOpts);
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path     = "/",
        });
        Response.Cookies.Delete("user_role", new CookieOptions
        {
            HttpOnly = false,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path     = "/",
        });

        return Ok(new { success = true });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private static bool IsPasswordStrong(string password) =>
        !string.IsNullOrEmpty(password)
        && password.Length >= 8
        && System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]")
        && System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]")
        && System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]")
        && System.Text.RegularExpressions.Regex.IsMatch(password, @"[^A-Za-z0-9]");

    private string BuildJwt(Guid tenantId, Guid userId, string role)
    {
        var section  = _config.GetSection("Jwt");
        var key      = Environment.GetEnvironmentVariable("JWT_KEY") ?? section["Key"]!;
        var issuer   = section["Issuer"]!;
        var audience = section["Audience"]!;
        // JWT court (15 min) — refresh token prend le relais
        var minutes  = 15;

        var claims = new List<Claim>
        {
            new("tenant_id", tenantId.ToString()),
            new("user_id",   userId.ToString()),
            new(ClaimTypes.Role, role)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds      = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetAuthCookie(string tokenString)
    {
        // JWT 15 min — refresh token prend le relais
        Response.Cookies.Append("jwt", tokenString, new CookieOptions
        {
            HttpOnly = true,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddMinutes(15),
            Path     = "/"
        });
    }

    private void SetRoleCookie(string role)
    {
        // Cookie rôle (NON HttpOnly — lu par middleware Next.js)
        Response.Cookies.Append("user_role", role, new CookieOptions
        {
            HttpOnly = false,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddDays(7),
            Path     = "/"
        });
    }

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddDays(7),
            Path     = "/"
        });
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private async Task<RefreshToken> IssueRefreshTokenAsync(AppDbContext db, Guid userId, string ip)
    {
        // Révoquer les anciens refresh tokens actifs de cet user
        var oldTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var old in oldTokens)
        {
            old.IsRevoked = true;
            old.RevokedAt = DateTime.UtcNow;
            old.RevokedByIp = ip;
        }

        var token = new RefreshToken
        {
            UserId      = userId,
            Token       = GenerateSecureToken(),
            CreatedByIp = ip,
            ExpiresAt   = DateTime.UtcNow.AddDays(7),
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
        return token;
    }
}
