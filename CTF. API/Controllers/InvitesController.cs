using System.Security.Cryptography;
using System.Text;
using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Invitations QR sécurisées en 3 types (voir QR_3_TYPES) :
///  - Type 1 (app) : invitation application vers l'inscription générale (SuperAdmin, sans tenant).
///  - Type 2 (enterprise_signup) : nouveau compte -> rattaché à l'entreprise scellée.
///  - Type 3 (enterprise_join) : compte existant -> rejoint l'entreprise scellée.
/// Le tenant d'une invitation entreprise est TOUJOURS dérivé du token stocké côté serveur
/// (jamais du body/query) : un QR de l'entreprise A ne rejoint que A. Token jamais en clair.
/// </summary>
[ApiController]
[Route("api")]
public class InvitesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public InvitesController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ── Admin : créer une invitation (typée) ─────────────────────────────────
    [HttpPost("admin/invites")]
    [Authorize(Roles = "admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateInviteRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var type = (req.Type ?? InviteTypes.EnterpriseJoin).Trim();
        if (!InviteTypes.IsValid(type))
            return BadRequest(new { error = "Type d'invitation invalide." });

        var userId = User.GetUserId();
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var token = GenerateToken();

        // Type 1 (App) : réservé SuperAdmin, aucun tenant, cible = inscription générale.
        if (type == InviteTypes.App)
        {
            if (!isSuperAdmin)
                return StatusCode(403, new { error = "Seul un SuperAdmin peut créer une invitation application." });

            var appInvite = NewInvite(null, type, token, req, userId);
            _db.TenantInvites.Add(appInvite);
            await _db.SaveChangesAsync();
            var registerUrl = $"{FrontendBaseUrl()}/register";
            return Ok(new CreatedInviteDto(appInvite.Id, token, registerUrl, appInvite.ExpiresAt, appInvite.MaxUses, type));
        }

        // Types 2 & 3 (Entreprise) : scellés au tenant actif de l'admin.
        var tenantId = User.GetTenantId();
        if (!isSuperAdmin && !await ManagesTenantAsync(userId, tenantId))
            return StatusCode(403, new { error = "Vous ne gérez pas cette société." });

        var invite = NewInvite(tenantId, type, token, req, userId);
        _db.TenantInvites.Add(invite);
        await _db.SaveChangesAsync();

        var joinUrl = $"{FrontendBaseUrl()}/join?token={token}";
        return Ok(new CreatedInviteDto(invite.Id, token, joinUrl, invite.ExpiresAt, invite.MaxUses, type));
    }

    // ── Admin : lister les invitations (tenant courant + app si SuperAdmin) ───
    [HttpGet("admin/invites")]
    [Authorize(Roles = "admin,SuperAdmin")]
    public async Task<IActionResult> List()
    {
        var tenantId = User.GetTenantId();
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var now = DateTime.UtcNow;

        var raw = await _db.TenantInvites
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId || (isSuperAdmin && i.TenantId == null))
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new { i.Id, i.ExpiresAt, i.MaxUses, i.UsedCount, i.IsRevoked, i.CreatedAt, i.InviteType, i.TenantId })
            .ToListAsync();

        var ids = raw.Where(x => x.TenantId != null).Select(x => x.TenantId!.Value).Distinct().ToList();
        var names = await _db.Tenants.AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        var items = raw.Select(i => new InviteDto(
            i.Id, i.ExpiresAt, i.MaxUses, i.UsedCount, i.IsRevoked, i.ExpiresAt < now, i.CreatedAt,
            i.InviteType, i.TenantId,
            i.TenantId != null && names.TryGetValue(i.TenantId.Value, out var n) ? n : null)).ToList();

        return Ok(items);
    }

    // ── Admin : révoquer une invitation ──────────────────────────────────────
    [HttpDelete("admin/invites/{id:guid}")]
    [Authorize(Roles = "admin,SuperAdmin")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var tenantId = User.GetTenantId();
        var isSuperAdmin = User.IsInRole("SuperAdmin");

        // Isolation : un admin ne révoque que les invitations de SA société ;
        // un SuperAdmin peut révoquer n'importe quelle invitation (y compris « app »).
        var invite = await _db.TenantInvites
            .FirstOrDefaultAsync(i => i.Id == id && (isSuperAdmin || i.TenantId == tenantId));
        if (invite is null) return NotFound();

        invite.IsRevoked = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Utilisateur : rejoindre un tenant via token (Types 2 & 3) ─────────────
    [HttpPost("invites/redeem")]
    [Authorize]
    public async Task<IActionResult> Redeem([FromBody] RedeemInviteRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return BadRequest(new { error = "Token manquant." });

        var tokenHash = HashToken(req.Token);
        var invite = await _db.TenantInvites
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);

        if (invite is null)
            return BadRequest(new { error = "Invitation invalide, expirée ou épuisée." });

        // Type 1 (App) : ne rattache à aucune entreprise (mène à l'inscription générale).
        if (invite.InviteType == InviteTypes.App || invite.TenantId is null)
            return BadRequest(new { error = "Ce QR mène à l'inscription générale, pas à une entreprise." });

        var inviteTenantId = invite.TenantId.Value;
        var userId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized(new { error = "Utilisateur introuvable." });

        // [MULTI-SOCIETES] déjà membre de cette société ?
        var alreadyMember = await _db.UserTenants
            .AnyAsync(ut => ut.UserId == userId && ut.TenantId == inviteTenantId);
        if (alreadyMember)
            return BadRequest(new { error = "Vous faites déjà partie de cette entreprise." });

        // [PENTEST] incrément atomique anti-TOCTOU : un seul UPDATE conditionnel
        // (Id + UsedCount<MaxUses + !IsRevoked + ExpiresAt>now) ; si 0 ligne affectée,
        // l'invitation est épuisée/expirée/révoquée et on refuse.
        var now = DateTime.UtcNow;
        var affected = await _db.TenantInvites
            .Where(i => i.Id == invite.Id
                        && i.UsedCount < i.MaxUses
                        && !i.IsRevoked
                        && i.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UsedCount, i => i.UsedCount + 1));

        if (affected != 1)
            return BadRequest(new { error = "Invitation invalide, expirée ou épuisée." });

        await AttachUserToTenantAsync(user.Id, inviteTenantId);

        var tenantName = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == inviteTenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync() ?? "Inconnu";

        return Ok(new RedeemResultDto(inviteTenantId, tenantName));
    }

    // ── Public : aperçu d'une invitation (type + société) sans la consommer ────
    // Sert à pré-remplir/verrouiller la société côté inscription (flux QR Type 2).
    [HttpGet("/api/auth/invite-preview")]
    [AllowAnonymous]
    public async Task<IActionResult> InvitePreview([FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "Token manquant." });

        var tokenHash = HashToken(token);
        var now = DateTime.UtcNow;
        var invite = await _db.TenantInvites.AsNoTracking()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);

        if (invite is null || invite.IsRevoked || invite.ExpiresAt <= now || invite.UsedCount >= invite.MaxUses)
            return Ok(new { valid = false, type = (string?)null, tenantId = (Guid?)null, tenantName = (string?)null });

        var tenantName = invite.TenantId is null ? null : await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == invite.TenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync();
        return Ok(new { valid = true, type = invite.InviteType, tenantId = invite.TenantId, tenantName });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static TenantInvite NewInvite(Guid? tenantId, string type, string token, CreateInviteRequest req, Guid userId) => new()
    {
        TenantId = tenantId,
        InviteType = type,
        TokenHash = HashToken(token),
        ExpiresAt = DateTime.UtcNow.AddHours(req.ExpiresInHours),
        MaxUses = req.MaxUses,
        UsedCount = 0,
        CreatedByUserId = userId,
        CreatedAt = DateTime.UtcNow,
        IsRevoked = false,
    };

    private async Task<bool> ManagesTenantAsync(Guid userId, Guid tenantId) =>
        await _db.UserTenants.AnyAsync(ut =>
            ut.UserId == userId && ut.TenantId == tenantId &&
            (ut.Role == "admin" || ut.Role == "owner"));

    private async Task AttachUserToTenantAsync(Guid userId, Guid tenantId)
    {
        // [MULTI-SOCIETES] Rejoindre = AJOUTER une appartenance (UserTenant) comme simple
        // membre (role User), sans écraser le tenant d'origine ni les sociétés déjà rejointes.
        var exists = await _db.UserTenants.AnyAsync(ut => ut.UserId == userId && ut.TenantId == tenantId);
        if (!exists)
        {
            _db.UserTenants.Add(new UserTenant
            {
                UserId    = userId,
                TenantId  = tenantId,
                Role      = "user",
                IsDefault = false,
                JoinedAt  = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
        }
        // [PENTEST] UsedCount déjà incrémenté atomiquement dans Redeem (anti-TOCTOU).
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private string FrontendBaseUrl() =>
        (_config["FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
}
