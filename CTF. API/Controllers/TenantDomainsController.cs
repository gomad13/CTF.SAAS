using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using CTF.Api.Services;

namespace CTF.Api.Controllers;

/// <summary>
/// Vérification de domaine par tenant (PASSE 1). Un admin déclare un domaine et prouve
/// sa possession via un enregistrement DNS TXT. Réservé aux admins du tenant.
/// </summary>
[ApiController]
[Route("api/tenant/domains")]
[Authorize(Roles = "admin,SuperAdmin")]
public class TenantDomainsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDomainVerificationService _dns;
    private readonly IMemoryCache _cache;
    private readonly AuditService _audit;

    public TenantDomainsController(AppDbContext db, IDomainVerificationService dns,
        IMemoryCache cache, AuditService audit)
    {
        _db = db; _dns = dns; _cache = cache; _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var domains = await _db.TenantEmailDomains.AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Domain)
            .ToListAsync(ct);
        return Ok(domains.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Declare([FromBody] DeclareDomainRequest req, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId   = User.GetUserId();
        var domain   = (req.Domain ?? string.Empty).Trim().ToLowerInvariant();

        if (!_dns.IsValidDomainFormat(domain))
            return BadRequest(new { error = "Format de domaine invalide (ex. clinique-saint-marc.fr)." });
        if (_dns.IsPublicDomain(domain))
            return BadRequest(new { error = "Les domaines de messagerie publics (gmail.com, outlook.com, orange.fr…) ne peuvent pas être déclarés." });

        var existing = await _db.TenantEmailDomains.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Domain == domain, ct);
        if (existing is not null)
            return Conflict(new { error = existing.TenantId == tenantId
                ? "Ce domaine est déjà déclaré pour votre organisation."
                : "Ce domaine est déjà rattaché à une autre organisation. Contactez le support si c'est une erreur." });

        var entity = new TenantEmailDomain
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Domain = domain,
            IsAutoProvisioningEnabled = false,          // PASSE 1 : aucun effet automatique
            VerificationToken = _dns.GenerateToken(),
            IsVerified = false, CreatedAt = DateTime.UtcNow, CreatedBy = userId,
        };
        _db.TenantEmailDomains.Add(entity);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { error = "Ce domaine est déjà rattaché à une autre organisation." }); }

        await _audit.LogAsync(tenantId, userId, "domain.declare", "TenantEmailDomain", entity.Id, domain);
        return StatusCode(StatusCodes.Status201Created, ToDto(entity));
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId   = User.GetUserId();

        if (IsRateLimited($"domverify_{tenantId}_{id}", 5))
            return StatusCode(429, new { error = "Trop de tentatives de vérification. Réessayez dans quelques minutes." });

        var entity = await _db.TenantEmailDomains.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);
        if (entity is null) return NotFound(new { error = "Domaine introuvable." });
        if (entity.IsVerified) return Ok(new VerifyDomainResultDto("verified", true, "Domaine déjà vérifié."));
        if (string.IsNullOrEmpty(entity.VerificationToken))
            return BadRequest(new { error = "Aucun token de vérification. Retirez puis re-déclarez le domaine." });

        entity.LastCheckedAt = DateTime.UtcNow;
        var result = await _dns.VerifyTxtAsync(entity.Domain, entity.VerificationToken, ct);
        return result == DomainVerificationResult.Verified
            ? await MarkVerifiedAsync(entity, tenantId, userId, ct)
            : await RecordFailureAsync(entity, tenantId, userId, result, ct);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId   = User.GetUserId();
        var entity = await _db.TenantEmailDomains.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);
        if (entity is null) return NotFound(new { error = "Domaine introuvable." });

        var domain = entity.Domain;
        _db.TenantEmailDomains.Remove(entity);          // token invalidé (ligne supprimée)
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(tenantId, userId, "domain.remove", "TenantEmailDomain", id, domain);
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private async Task<IActionResult> MarkVerifiedAsync(TenantEmailDomain e, Guid tenantId, Guid userId, CancellationToken ct)
    {
        e.IsVerified = true; e.VerifiedAt = DateTime.UtcNow; e.VerifiedBy = userId;
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { error = "Ce domaine vient d'être vérifié par une autre organisation." }); }
        await _audit.LogAsync(tenantId, userId, "domain.verify.success", "TenantEmailDomain", e.Id, e.Domain);
        return Ok(new VerifyDomainResultDto("verified", true, "Domaine vérifié avec succès."));
    }

    private async Task<IActionResult> RecordFailureAsync(TenantEmailDomain e, Guid tenantId, Guid userId,
        DomainVerificationResult result, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);   // persiste LastCheckedAt
        await _audit.LogAsync(tenantId, userId, "domain.verify.failed", "TenantEmailDomain", e.Id, $"{e.Domain}:{result}");
        var (code, msg) = result switch
        {
            DomainVerificationResult.RecordNotFound => ("record_not_found", "Enregistrement TXT introuvable. Vérifiez qu'il est bien posé (la propagation DNS peut prendre jusqu'à 48 h)."),
            DomainVerificationResult.TokenMismatch  => ("token_mismatch", "Un enregistrement TXT existe mais sa valeur ne correspond pas. Vérifiez que vous avez copié la bonne valeur."),
            _                                        => ("dns_unavailable", "Le service DNS n'a pas pu être interrogé pour le moment. Réessayez plus tard."),
        };
        return Ok(new VerifyDomainResultDto(code, false, msg));
    }

    private TenantDomainDto ToDto(TenantEmailDomain d) => new(
        d.Id, d.Domain, d.IsVerified, d.IsVerified ? "verified" : "pending",
        d.VerifiedAt, d.LastCheckedAt, d.CreatedAt,
        _dns.RecordName(d.Domain),
        d.IsVerified || d.VerificationToken is null ? string.Empty : _dns.RecordValue(d.VerificationToken));

    private bool IsRateLimited(string key, int max)
    {
        var count = _cache.GetOrCreate(key, e => { e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15); return 0; });
        if (count >= max) return true;
        _cache.Set(key, count + 1, TimeSpan.FromMinutes(15));
        return false;
    }
}
