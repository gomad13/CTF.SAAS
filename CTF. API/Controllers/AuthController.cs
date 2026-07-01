using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CTF.Api.Contracts;
using CTF.Api.Contracts.Legal;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Models.Legal;
using CTF.Api.Security;
using CTF.Api.Services;
using CTF.Api.Services.Legal;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration      _config;
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache        _cache;
    private readonly IMailService        _mail;
    private readonly ILogger<AuthController> _logger;

    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

    // cache key prefix for password-reset tokens
    private const string ResetPrefix = "pwd_reset_";

    // M3 — cookie opaque (≠ cookie "jwt") portant l'état "en attente de code 2FA" au login.
    private const string TwoFactorCookie = "twofa_pending";

    private record ResetTokenData(Guid UserId, Guid TenantId, DateTime ExpiresAt);

    public AuthController(IConfiguration config, IWebHostEnvironment env, IMemoryCache cache,
        IMailService mail, ILogger<AuthController> logger)
    {
        _config = config;
        _env    = env;
        _cache  = cache;
        _mail   = mail;
        _logger = logger;
    }

    // ── Dev token (DEV only) ────────────────────────────────────────────────
    public record DevTokenRequest(Guid TenantId, Guid UserId, string Role);

#if DEBUG
    [HttpPost("dev-token")]
    public IActionResult DevToken([FromBody] DevTokenRequest req)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var tokenString = BuildJwt(req.TenantId, req.UserId, req.Role);
        SetAuthCookie(tokenString);
        return Ok(new { message = "Token émis." });
    }
#endif

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

        // ── Succès credentials : reset du compteur ──────────────────────────
        _cache.Remove(cacheKey);

        // ── M3 — Si 2FA email activée : NE PAS délivrer la session ───────────
        // On crée un code, on pose un cookie opaque "pending", on envoie le code,
        // et on renvoie requiresTwoFactor. La session est délivrée par /2fa/verify.
        if (user.TwoFactorEnabled)
        {
            await StartTwoFactorChallengeAsync(db, user);
            return Ok(new { requiresTwoFactor = true });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var (role, redirectTo) = await IssueSessionAsync(db, user);
        return Ok(new { role, redirectTo });
    }

    /// <summary>
    /// Délivre la session (JWT + refresh + cookie rôle) et renvoie (rôle effectif, redirection).
    /// Partagé par le login classique et la vérification 2FA.
    /// </summary>
    private async Task<(string role, string redirectTo)> IssueSessionAsync(AppDbContext db, User user)
    {
        var isSuperAdmin = await db.SuperAdmins
            .AnyAsync(sa => sa.IsActive && sa.Email.ToLower() == user.Email.ToLower());

        // [MULTI-SOCIETES] Société active par défaut + rôle dans cette société.
        var (activeTenantId, effectiveRole) = await ResolveActiveAsync(db, user, isSuperAdmin, requestedTenantId: null);

        SetAuthCookie(BuildJwt(activeTenantId, user.Id, effectiveRole));
        SetRoleCookie(effectiveRole);

        var ipAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var refresh = await IssueRefreshTokenAsync(db, user.Id, ipAddr, activeTenantId);
        SetRefreshCookie(refresh.Token);

        var redirectTo = effectiveRole switch
        {
            "SuperAdmin" => "/superadmin",
            var r when string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase) => "/admin/dashboard",
            _ => "/dashboard",
        };
        return (effectiveRole, redirectTo);
    }

    // ── Register ────────────────────────────────────────────────────────────
    // Le payload inclut désormais une liste de consentements (RGPD article 7).
    // Tous les documents légaux marqués IsRequired doivent être présents et
    // acceptés sur leur version active, sinon l'inscription est refusée.
    public record RegisterRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        Guid? TenantId,
        List<ConsentItem>? Consents
    );

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest req,
        [FromServices] AppDbContext db,
        [FromServices] IConfiguration config,
        [FromServices] IConsentService consentService,
        CancellationToken ct)
    {
        // Bêta DSI : inscription publique fermée par défaut.
        // Réactivable via appsettings : "Beta:RegistrationOpen": true ou env BETA__REGISTRATIONOPEN=true.
        var registrationOpen = config.GetValue<bool>("Beta:RegistrationOpen", false);
        if (!registrationOpen)
        {
            return StatusCode(403, new
            {
                error = "L'inscription publique est actuellement fermée. " +
                        "Si votre organisation est partenaire de Sentys, contactez votre administrateur. " +
                        "Pour rejoindre la bêta, écrivez à contact@sentys.fr."
            });
        }

        if (!IsPasswordStrong(req.Password))
            return BadRequest(new
            {
                error = "Le mot de passe doit contenir au moins 8 caractères, " +
                        "une majuscule, une minuscule, un chiffre et un caractère spécial."
            });

        // Validation stricte des consentements RGPD avant tout I/O DB lourd.
        var consents = req.Consents ?? new List<ConsentItem>();
        var consentError = await consentService.ValidateRequiredConsentsAsync(consents, ct);
        if (consentError is not null)
            return BadRequest(new { error = consentError });

        // [PENTEST] tenant non issu du body — inscription publique self-service :
        // on ignore req.TenantId pour éviter qu'un client se rattache à un tenant arbitraire.
        var tenantId = DemoTenantId;

        var tenantExists = await db.Tenants.AnyAsync(t => t.Id == tenantId, ct);
        if (!tenantExists)
            return BadRequest(new { error = "Tenant introuvable. Vérifiez votre TenantId." });

        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var emailTaken = await db.Users.AnyAsync(u =>
            u.TenantId == tenantId && u.Email == normalizedEmail, ct);
        if (emailTaken)
            return Conflict(new { error = "Cet email est déjà utilisé dans cette organisation." });

        // Atomicité user + consentements : transaction explicite pour qu'un
        // échec d'insertion des consentements rollback aussi la création du user.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        // [MULTI-SOCIETES] Appartenance par défaut du nouvel utilisateur à sa société.
        db.UserTenants.Add(new UserTenant
        {
            UserId    = user.Id,
            TenantId  = tenantId,
            Role      = user.Role,
            IsDefault = true,
            JoinedAt  = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        await consentService.RecordConsentsAsync(
            userId: user.Id,
            tenantId: tenantId,
            consents: consents,
            source: ConsentSources.Registration,
            ipAddress: ConsentRequestContext.GetClientIp(HttpContext),
            userAgent: ConsentRequestContext.GetUserAgent(HttpContext),
            ct: ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Auto-assign demo paths to all new users (regardless of tenant)
        {
            var demoPaths = new[]
            {
                Guid.Parse("00000000-0000-0000-0000-000000000001"), // Demo
                Guid.Parse("20000000-0000-0000-0000-000000000000"), // Medical
            };
            foreach (var pathId in demoPaths)
            {
                if (!await db.Paths.AnyAsync(p => p.Id == pathId, ct)) continue;
                if (await db.Assignments.AnyAsync(a => a.UserId == user.Id && a.PathId == pathId, ct)) continue;

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
            await db.SaveChangesAsync(ct);
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

            // [PENTEST] Ne JAMAIS logguer le token/lien en clair hors Development.
            var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?token={token}";
            if (_env.IsDevelopment())
            {
                Console.WriteLine($"[DEV] Lien de réinitialisation pour {normalizedEmail} : {resetLink}");
            }
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

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 12);
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

        // Source de vérité : on re-dérive le statut SuperAdmin depuis la table SuperAdmins
        // (par email) à CHAQUE appel, au lieu de se fier au rôle figé dans le JWT. Sinon un
        // token court rafraîchi/ancien peut renvoyer "admin" et faire disparaître le menu
        // SuperAdmin alors que le compte est bien SuperAdmin actif en base.
        // Même requête qu'au login (cf. plus haut) ; non-SuperAdmin → rôle BDD inchangé.
        var isSuperAdmin = await db.SuperAdmins
            .AnyAsync(sa => sa.IsActive && sa.Email.ToLower() == user.Email.ToLower());
        var effectiveRole = isSuperAdmin ? "SuperAdmin" : user.Role;

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            Role       = effectiveRole,
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

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 12);
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

        // [MULTI-SOCIETES] Restaurer la société active mémorisée dans le refresh token
        // (re-vérifie l'appartenance ; retombe sur la société par défaut si plus membre).
        var (activeTenantId, effectiveRole) = await ResolveActiveAsync(db, user, isSuperAdmin, rt.ActiveTenantId);

        // Nouveaux tokens (rotation)
        var newJwt = BuildJwt(activeTenantId, user.Id, effectiveRole);
        SetAuthCookie(newJwt);
        SetRoleCookie(effectiveRole);

        var newRefresh = await IssueRefreshTokenAsync(db, user.Id, ip, activeTenantId);
        SetRefreshCookie(newRefresh.Token);

        return Ok(new { success = true });
    }

    // ── Logout all ───────────────────────────────────────────────────────────
    [HttpPost("logout-all")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> LogoutAll([FromServices] AppDbContext db, CancellationToken ct)
    {
        var userId = User.GetUserId();

        // Révoquer TOUS les refresh tokens actifs de l'utilisateur courant
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);
        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        await db.SaveChangesAsync(ct);

        // Supprimer les cookies de session comme dans Logout
        Response.Cookies.Delete("jwt", new CookieOptions
        {
            HttpOnly = true,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path     = "/",
        });
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path     = "/",
        });
        Response.Cookies.Delete("user_role", new CookieOptions
        {
            HttpOnly = false,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path     = "/",
        });

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
            SameSite = SameSiteMode.Lax,
            Path     = "/"
        };
        Response.Cookies.Delete("jwt", baseOpts);
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path     = "/",
        });
        Response.Cookies.Delete("user_role", new CookieOptions
        {
            HttpOnly = false,
            Secure   = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path     = "/",
        });

        return Ok(new { success = true });
    }

    // ════════════════════════════════════════════════════════════════════════
    // M3 — Double authentification par email (2FA), optionnelle par utilisateur
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet("2fa/status")]
    [Authorize]
    public async Task<IActionResult> TwoFactorStatus([FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        var enabled = await db.Users.Where(u => u.Id == userId)
            .Select(u => u.TwoFactorEnabled).FirstOrDefaultAsync();
        return Ok(new TwoFactorStatusDto(enabled));
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> TwoFactorEnable([FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized(new { error = "Utilisateur introuvable." });
        if (user.TwoFactorEnabled) return BadRequest(new { error = "La 2FA est déjà activée." });

        var code = await CreateCodeAsync(db, user.Id, pendingTokenHash: null);
        await _mail.SendTwoFactorCodeAsync(user.Email, code);
        return Ok(new { message = "Code de confirmation envoyé." });
    }

    [HttpPost("2fa/confirm")]
    [Authorize]
    public async Task<IActionResult> TwoFactorConfirm([FromBody] TwoFactorCodeRequest req, [FromServices] AppDbContext db)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Code à 6 chiffres requis." });
        var userId = User.GetUserId();
        var entry = await db.TwoFactorCodes
            .Where(c => c.UserId == userId && c.PendingTokenHash == null && !c.IsUsed)
            .OrderByDescending(c => c.CreatedAt).FirstOrDefaultAsync();

        var (ok, err) = await ValidateCodeAsync(db, entry, req.Code);
        if (!ok) return BadRequest(new { error = err });

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.TwoFactorEnabled = true;
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> TwoFactorDisable([FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized(new { error = "Utilisateur introuvable." });

        user.TwoFactorEnabled = false;
        await InvalidateUserCodesAsync(db, userId);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> TwoFactorVerify([FromBody] TwoFactorCodeRequest req, [FromServices] AppDbContext db)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Code à 6 chiffres requis." });
        var pending = Request.Cookies[TwoFactorCookie];
        if (string.IsNullOrEmpty(pending))
            return Unauthorized(new { error = "Session 2FA expirée. Reconnectez-vous." });

        var entry = await db.TwoFactorCodes
            .FirstOrDefaultAsync(c => c.PendingTokenHash == Hash(pending) && !c.IsUsed);
        var (ok, err) = await ValidateCodeAsync(db, entry, req.Code);
        if (!ok) return BadRequest(new { error = err });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == entry!.UserId);
        if (user is null || !user.IsActive)
            return Unauthorized(new { error = "Compte indisponible." });

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Response.Cookies.Delete(TwoFactorCookie, PendingCookieOptions());

        var (role, redirectTo) = await IssueSessionAsync(db, user);
        return Ok(new { role, redirectTo });
    }

    [HttpPost("2fa/resend")]
    [AllowAnonymous]
    public async Task<IActionResult> TwoFactorResend([FromServices] AppDbContext db)
    {
        var pending = Request.Cookies[TwoFactorCookie];
        if (string.IsNullOrEmpty(pending))
            return Unauthorized(new { error = "Session 2FA expirée. Reconnectez-vous." });

        var entry = await db.TwoFactorCodes
            .FirstOrDefaultAsync(c => c.PendingTokenHash == Hash(pending) && !c.IsUsed);
        if (entry is null) return BadRequest(new { error = "Session 2FA invalide." });
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == entry.UserId);
        if (user is null) return BadRequest(new { error = "Compte introuvable." });

        var code = GenerateNumericCode();
        entry.CodeHash = Hash(code);
        entry.Attempts = 0;
        entry.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
        await db.SaveChangesAsync();
        await _mail.SendTwoFactorCodeAsync(user.Email, code);
        return Ok(new { message = "Nouveau code envoyé." });
    }

    // ── 2FA helpers ───────────────────────────────────────────────────────────
    private async Task StartTwoFactorChallengeAsync(AppDbContext db, User user)
    {
        var pendingToken = GenerateSecureToken();           // opaque, ≠ cookie "jwt"
        var code = await CreateCodeAsync(db, user.Id, Hash(pendingToken));
        SetTwoFactorPendingCookie(pendingToken);
        await _mail.SendTwoFactorCodeAsync(user.Email, code);
        _logger.LogInformation("[2FA] challenge envoyé user={UserId}", user.Id);
    }

    /// <summary>Crée un code 2FA (un seul actif à la fois), le stocke hashé, renvoie le clair.</summary>
    private async Task<string> CreateCodeAsync(AppDbContext db, Guid userId, string? pendingTokenHash)
    {
        await InvalidateUserCodesAsync(db, userId);
        var code = GenerateNumericCode();
        db.TwoFactorCodes.Add(new TwoFactorCode
        {
            UserId = userId,
            CodeHash = Hash(code),
            PendingTokenHash = pendingTokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return code;
    }

    /// <summary>Valide un code (existe / non expiré / tentatives &lt; max / correspond) ; persiste l'état.</summary>
    private async Task<(bool ok, string? err)> ValidateCodeAsync(AppDbContext db, TwoFactorCode? entry, string code)
    {
        if (entry is null) return (false, "Code invalide ou expiré.");
        if (entry.ExpiresAt < DateTime.UtcNow)
        { entry.IsUsed = true; await db.SaveChangesAsync(); return (false, "Code expiré. Demandez-en un nouveau."); }
        if (entry.Attempts >= entry.MaxAttempts)
        { entry.IsUsed = true; await db.SaveChangesAsync(); return (false, "Trop de tentatives. Demandez un nouveau code."); }
        if (entry.CodeHash != Hash(code))
        { entry.Attempts += 1; await db.SaveChangesAsync(); return (false, "Code incorrect."); }
        entry.IsUsed = true;
        await db.SaveChangesAsync();
        return (true, null);
    }

    private static async Task InvalidateUserCodesAsync(AppDbContext db, Guid userId)
    {
        var actives = await db.TwoFactorCodes.Where(c => c.UserId == userId && !c.IsUsed).ToListAsync();
        foreach (var c in actives) c.IsUsed = true;
    }

    private static string GenerateNumericCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string Hash(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private void SetTwoFactorPendingCookie(string token) =>
        Response.Cookies.Append(TwoFactorCookie, token, PendingCookieOptions());

    private CookieOptions PendingCookieOptions() => new()
    {
        HttpOnly = true,
        Secure   = !_env.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Expires  = DateTimeOffset.UtcNow.AddMinutes(10),
        Path     = "/",
    };

    // ── Helpers ─────────────────────────────────────────────────────────────
    private static bool IsPasswordStrong(string password) =>
        !string.IsNullOrEmpty(password)
        && password.Length >= 8
        && System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]")
        && System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]")
        && System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]")
        && System.Text.RegularExpressions.Regex.IsMatch(password, @"[^A-Za-z0-9]");

    // ── [MULTI-SOCIETES] Résolution de la société active + endpoints /api/me ──

    /// <summary>
    /// Détermine la société active et le rôle effectif d'un utilisateur.
    /// requestedTenantId : société demandée (switch/refresh) ; null = société par défaut.
    /// SÉCURITÉ : un non-SuperAdmin ne peut activer qu'une société dont il est MEMBRE (UserTenant).
    /// Le claim tenant_id émis pilote directement l'isolation (TenantContext + filtres .Where).
    /// </summary>
    private async Task<(Guid tenantId, string effectiveRole)> ResolveActiveAsync(
        AppDbContext db, User user, bool isSuperAdmin, Guid? requestedTenantId)
    {
        var memberships = await db.UserTenants
            .Where(ut => ut.UserId == user.Id)
            .ToListAsync();

        UserTenant? chosen = null;
        if (requestedTenantId.HasValue)
            chosen = memberships.FirstOrDefault(m => m.TenantId == requestedTenantId.Value);

        Guid tenantId;
        string roleInTenant;
        if (chosen != null)
        {
            tenantId = chosen.TenantId;
            roleInTenant = chosen.Role;
        }
        else if (requestedTenantId.HasValue && isSuperAdmin)
        {
            // SuperAdmin : accès cross-société autorisé même sans appartenance explicite.
            tenantId = requestedTenantId.Value;
            roleInTenant = "admin";
        }
        else
        {
            var def = memberships.FirstOrDefault(m => m.IsDefault) ?? memberships.FirstOrDefault();
            if (def != null) { tenantId = def.TenantId; roleInTenant = def.Role; }
            else { tenantId = user.TenantId; roleInTenant = user.Role; } // fallback (aucune appartenance)
        }

        var effectiveRole = isSuperAdmin
            ? "SuperAdmin"
            : (string.Equals(roleInTenant, "owner", StringComparison.OrdinalIgnoreCase) ? "admin" : roleInTenant);
        return (tenantId, effectiveRole);
    }

    /// <summary>Liste des sociétés de l'utilisateur connecté (pour le sélecteur de société).</summary>
    [HttpGet("/api/me/tenants")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> MyTenants([FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        Guid activeTenantId = Guid.Empty;
        var tc = User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
        if (tc != null) Guid.TryParse(tc, out activeTenantId);

        var memberships = await db.UserTenants
            .Where(ut => ut.UserId == userId)
            .ToListAsync();
        var tenantIds = memberships.Select(m => m.TenantId).ToList();
        var tenants = await db.Tenants
            .Where(t => tenantIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name, t.IsActive })
            .ToListAsync();

        var list = memberships
            .Select(m =>
            {
                var t = tenants.FirstOrDefault(x => x.Id == m.TenantId);
                return new
                {
                    tenantId = m.TenantId,
                    name = t?.Name ?? "(société)",
                    role = m.Role,
                    isActive = m.TenantId == activeTenantId,
                    isDefault = m.IsDefault,
                    enabled = t?.IsActive ?? true,
                };
            })
            .OrderByDescending(x => x.isActive)
            .ThenBy(x => x.name)
            .ToList();

        return Ok(new { activeTenantId, tenants = list });
    }

    public record SetActiveTenantRequest(Guid TenantId);

    /// <summary>Change la société active : ré-émet le JWT/cookies pour la nouvelle société.</summary>
    [HttpPost("/api/me/active-tenant")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> SetActiveTenant([FromBody] SetActiveTenantRequest req, [FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null || !user.IsActive)
            return Unauthorized(new { error = "Compte introuvable ou désactivé." });

        var isSuperAdmin = await db.SuperAdmins
            .AnyAsync(sa => sa.IsActive && sa.Email.ToLower() == user.Email.ToLower());

        // SÉCURITÉ CRITIQUE : appartenance obligatoire (sauf SuperAdmin) avant d'émettre
        // un JWT portant cette société active — sinon fuite cross-société.
        var member = await db.UserTenants
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TenantId == req.TenantId);
        if (member is null && !isSuperAdmin)
            return StatusCode(403, new { error = "Vous n'êtes pas membre de cette société." });

        var (activeTenantId, effectiveRole) = await ResolveActiveAsync(db, user, isSuperAdmin, req.TenantId);

        SetAuthCookie(BuildJwt(activeTenantId, user.Id, effectiveRole));
        SetRoleCookie(effectiveRole);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var refresh = await IssueRefreshTokenAsync(db, user.Id, ip, activeTenantId);
        SetRefreshCookie(refresh.Token);

        var redirectTo = effectiveRole switch
        {
            "SuperAdmin" => "/superadmin",
            var r when string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase) => "/admin/dashboard",
            _ => "/dashboard",
        };
        return Ok(new { success = true, tenantId = activeTenantId, role = effectiveRole, redirectTo });
    }

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
            SameSite = SameSiteMode.Lax,
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
            SameSite = SameSiteMode.Lax,
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
            SameSite = SameSiteMode.Lax,
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

    private async Task<RefreshToken> IssueRefreshTokenAsync(AppDbContext db, Guid userId, string ip, Guid? activeTenantId = null)
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
            ActiveTenantId = activeTenantId,   // [MULTI-SOCIETES]
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
        return token;
    }
}
