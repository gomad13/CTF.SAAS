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
/// V4 — Invitations sécurisées pour rejoindre un tenant via QR code / lien.
/// Endpoints admin (création / liste / révocation) + endpoint utilisateur (redeem).
/// Toute validation est faite côté serveur ; le token n'est jamais stocké ni logué en clair.
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

    // ── Admin : créer une invitation ─────────────────────────────────────────
    [HttpPost("admin/invites")]
    [Authorize(Roles = "admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateInviteRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tenantId = User.GetTenantId();
        var userId = User.GetUserId();

        var token = GenerateToken();
        var invite = new TenantInvite
        {
            TenantId = tenantId,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddHours(req.ExpiresInHours),
            MaxUses = req.MaxUses,
            UsedCount = 0,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
        };

        _db.TenantInvites.Add(invite);
        await _db.SaveChangesAsync();

        var joinUrl = $"{FrontendBaseUrl()}/join?token={token}";
        return Ok(new CreatedInviteDto(invite.Id, token, joinUrl, invite.ExpiresAt, invite.MaxUses));
    }

    // ── Admin : lister les invitations du tenant ─────────────────────────────
    [HttpGet("admin/invites")]
    [Authorize(Roles = "admin,SuperAdmin")]
    public async Task<IActionResult> List()
    {
        var tenantId = User.GetTenantId();
        var now = DateTime.UtcNow;

        var items = await _db.TenantInvites
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InviteDto(
                i.Id, i.ExpiresAt, i.MaxUses, i.UsedCount,
                i.IsRevoked, i.ExpiresAt < now, i.CreatedAt))
            .ToListAsync();

        return Ok(items);
    }

    // ── Admin : révoquer une invitation ──────────────────────────────────────
    [HttpDelete("admin/invites/{id:guid}")]
    [Authorize(Roles = "admin,SuperAdmin")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var tenantId = User.GetTenantId();

        var invite = await _db.TenantInvites
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);
        if (invite is null) return NotFound();

        invite.IsRevoked = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Utilisateur : rejoindre un tenant via token ──────────────────────────
    [HttpPost("invites/redeem")]
    [Authorize]
    public async Task<IActionResult> Redeem([FromBody] RedeemInviteRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return BadRequest(new { error = "Token manquant." });

        var invite = await _db.TenantInvites
            .FirstOrDefaultAsync(i => i.TokenHash == HashToken(req.Token));

        if (!IsRedeemable(invite))
            return BadRequest(new { error = "Invitation invalide, expirée ou épuisée." });

        var userId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized(new { error = "Utilisateur introuvable." });

        if (user.TenantId == invite!.TenantId)
            return BadRequest(new { error = "Vous faites déjà partie de cette entreprise." });

        await AttachUserToTenantAsync(user, invite);

        var tenantName = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == invite.TenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync() ?? "Inconnu";

        return Ok(new RedeemResultDto(invite.TenantId, tenantName));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static bool IsRedeemable(TenantInvite? invite) =>
        invite is not null
        && !invite.IsRevoked
        && invite.ExpiresAt > DateTime.UtcNow
        && invite.UsedCount < invite.MaxUses;

    private async Task AttachUserToTenantAsync(User user, TenantInvite invite)
    {
        // Rattachement : on rejoint TOUJOURS comme simple membre (on ne devient pas
        // admin d'une entreprise en scannant un QR). TeamId appartient à l'ancien tenant.
        user.TenantId = invite.TenantId;
        user.Role = "user";
        user.TeamId = null;
        user.UpdatedAt = DateTime.UtcNow;
        invite.UsedCount += 1;
        await _db.SaveChangesAsync();
        // Le JWT contient encore l'ancien tenant_id : le front appelle /api/auth/refresh
        // qui reconstruit le token depuis user.TenantId (nouveau tenant).
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
