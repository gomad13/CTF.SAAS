using CTF.Api.Contracts.Legal;
using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services.Legal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CTF.Api.Controllers;

/// <summary>
/// Consultation et re-acceptation par l'utilisateur connecté de ses propres
/// consentements. Sert à la page /account/consents et à la modal de re-
/// acceptation déclenchée par le middleware <see cref="Middleware.RequireUpToDateConsentMiddleware"/>.
/// </summary>
[ApiController]
[Route("api/me/consents")]
[Authorize]
public sealed class MyConsentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConsentService _consents;
    private readonly ILegalDocumentService _legalDocs;
    private readonly IMemoryCache _cache;

    public MyConsentsController(AppDbContext db, IConsentService consents, ILegalDocumentService legalDocs, IMemoryCache cache)
    {
        _db = db;
        _consents = consents;
        _legalDocs = legalDocs;
        _cache = cache;
    }

    /// <summary>Historique complet des consentements de l'utilisateur connecté.</summary>
    [HttpGet]
    public async Task<ActionResult<List<UserConsentDto>>> GetMyConsents(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();
        var activeDocs = await _legalDocs.GetActiveDocumentsAsync(ct);
        var activeBySlug = activeDocs.ToDictionary(d => d.Slug, d => d.Version);

        var titlesById = await _db.LegalDocuments
            .AsNoTracking()
            .Select(d => new { d.Id, d.Title })
            .ToDictionaryAsync(x => x.Id, x => x.Title, ct);

        var consents = await _db.UserConsents
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.TenantId == tenantId) // [PENTEST] filtre tenant
            .OrderByDescending(c => c.AcceptedAt)
            .ToListAsync(ct);

        var dtos = consents.Select(c => new UserConsentDto(
            Id: c.Id,
            DocumentSlug: c.DocumentSlug,
            DocumentVersion: c.DocumentVersion,
            DocumentTitle: titlesById.TryGetValue(c.LegalDocumentId, out var t) ? t : c.DocumentSlug,
            Accepted: c.Accepted,
            AcceptedAt: c.AcceptedAt,
            IpAddress: c.IpAddress,
            UserAgent: c.UserAgent,
            Source: c.Source,
            IsCurrentVersion: activeBySlug.TryGetValue(c.DocumentSlug, out var v)
                              && string.Equals(v, c.DocumentVersion, StringComparison.Ordinal)
        )).ToList();

        return Ok(dtos);
    }

    /// <summary>État de complétude vis-à-vis des documents requis actifs.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<ConsentStatusDto>> GetStatus(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var status = await _consents.GetUserStatusAsync(userId, ct);
        return Ok(status);
    }

    /// <summary>Re-acceptation d'une ou plusieurs nouvelles versions.</summary>
    [HttpPost("re-accept")]
    public async Task<ActionResult> ReAccept([FromBody] ReAcceptRequest req, CancellationToken ct)
    {
        if (req.Consents is null || req.Consents.Count == 0)
            return BadRequest(new { error = "Le bloc 'consents' est obligatoire." });

        var error = await _consents.ValidateReAcceptanceAsync(req.Consents, ct);
        if (error is not null)
            return BadRequest(new { error });

        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        await _consents.RecordConsentsAsync(
            userId, tenantId, req.Consents,
            Models.Legal.ConsentSources.ReAcceptance,
            ConsentRequestContext.GetClientIp(HttpContext),
            ConsentRequestContext.GetUserAgent(HttpContext),
            ct);

        await _db.SaveChangesAsync(ct);
        // Invalide la mise en cache "consent à jour" du middleware pour ce user
        _cache.Remove($"consent:up_to_date:{userId}");
        return Ok(new { success = true });
    }
}
