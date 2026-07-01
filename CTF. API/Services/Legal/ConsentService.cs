using CTF.Api.Contracts.Legal;
using CTF.Api.Data;
using CTF.Api.Models.Legal;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services.Legal;

public interface IConsentService
{
    /// <summary>Valide qu'un payload de consentements couvre tous les documents requis dans leur version active. Pour l'inscription : tous les docs requis doivent être présents et acceptés.</summary>
    Task<string?> ValidateRequiredConsentsAsync(IReadOnlyList<ConsentItem> consents, CancellationToken ct = default);

    /// <summary>Valide un payload de re-acceptation : chaque item doit cibler la version active du slug et être marqué accepté. Pas d'exigence de couvrir tous les docs requis (l'utilisateur ne re-accepte que ce qui a changé).</summary>
    Task<string?> ValidateReAcceptanceAsync(IReadOnlyList<ConsentItem> consents, CancellationToken ct = default);

    /// <summary>Insère N enregistrements UserConsent dans le DbContext courant (sans SaveChanges).</summary>
    Task RecordConsentsAsync(
        Guid userId,
        Guid tenantId,
        IReadOnlyList<ConsentItem> consents,
        string source,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    /// <summary>État de consentement d'un user vis-à-vis des documents actifs requis.</summary>
    Task<ConsentStatusDto> GetUserStatusAsync(Guid userId, CancellationToken ct = default);
}

public sealed class ConsentService : IConsentService
{
    private readonly AppDbContext _db;
    private readonly ILegalDocumentService _legalDocs;

    public ConsentService(AppDbContext db, ILegalDocumentService legalDocs)
    {
        _db = db;
        _legalDocs = legalDocs;
    }

    public async Task<string?> ValidateRequiredConsentsAsync(IReadOnlyList<ConsentItem> consents, CancellationToken ct = default)
    {
        if (consents is null) return "Le bloc 'consents' est obligatoire.";

        var activeDocs = await _legalDocs.GetActiveDocumentsAsync(ct);
        var requiredDocs = activeDocs.Where(d => d.IsRequired).ToList();

        foreach (var doc in requiredDocs)
        {
            var docSlug = LegalNormalization.NormalizeSlug(doc.Slug);
            var docVersion = LegalNormalization.NormalizeVersion(doc.Version);
            var match = consents.FirstOrDefault(c => LegalNormalization.NormalizeSlug(c.DocumentSlug) == docSlug);
            if (match is null)
                return $"Le consentement '{docSlug}' est obligatoire et doit être accepté pour la version {docVersion}.";

            var receivedVersion = LegalNormalization.NormalizeVersion(match.DocumentVersion);
            if (!string.Equals(receivedVersion, docVersion, StringComparison.Ordinal))
                return $"Le consentement '{docSlug}' doit porter sur la version active {docVersion} (reçu : {receivedVersion}).";

            if (!match.Accepted)
                return $"Le consentement '{docSlug}' est obligatoire et doit être accepté pour la version {docVersion}.";
        }

        return null;
    }

    public async Task<string?> ValidateReAcceptanceAsync(IReadOnlyList<ConsentItem> consents, CancellationToken ct = default)
    {
        if (consents is null || consents.Count == 0)
            return "Le bloc 'consents' est obligatoire et doit contenir au moins un consentement.";

        var activeDocs = await _legalDocs.GetActiveDocumentsAsync(ct);
        var activeBySlug = activeDocs.ToDictionary(d => LegalNormalization.NormalizeSlug(d.Slug), d => d);

        foreach (var c in consents)
        {
            var slug = LegalNormalization.NormalizeSlug(c.DocumentSlug);
            if (!activeBySlug.TryGetValue(slug, out var doc))
                return $"Document '{slug}' inconnu ou inactif.";

            var docVersion = LegalNormalization.NormalizeVersion(doc.Version);
            var receivedVersion = LegalNormalization.NormalizeVersion(c.DocumentVersion);
            if (!string.Equals(receivedVersion, docVersion, StringComparison.Ordinal))
                return $"La version reçue pour '{slug}' n'est pas la version active ({docVersion}).";
            if (doc.IsRequired && !c.Accepted)
                return $"Le consentement '{slug}' est obligatoire — l'option 'refuser' déclenchera une procédure de retrait dédiée, non implémentée ici.";
        }

        return null;
    }

    public async Task RecordConsentsAsync(
        Guid userId,
        Guid tenantId,
        IReadOnlyList<ConsentItem> consents,
        string source,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var activeDocs = await _legalDocs.GetActiveDocumentsAsync(ct);
        var docsBySlug = activeDocs.ToDictionary(d => LegalNormalization.NormalizeSlug(d.Slug), d => d);
        var now = DateTime.UtcNow;

        foreach (var c in consents)
        {
            var slug = LegalNormalization.NormalizeSlug(c.DocumentSlug);
            if (!docsBySlug.TryGetValue(slug, out var doc)) continue;

            var docVersion = LegalNormalization.NormalizeVersion(doc.Version);
            var receivedVersion = LegalNormalization.NormalizeVersion(c.DocumentVersion);
            if (!string.Equals(receivedVersion, docVersion, StringComparison.Ordinal)) continue;

            _db.UserConsents.Add(new UserConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = tenantId,
                LegalDocumentId = doc.Id,
                DocumentSlug = slug,         // slug normalisé (sans BOM/NBSP/ZWS/whitespace)
                DocumentVersion = docVersion, // version normalisée
                Accepted = c.Accepted,
                AcceptedAt = now,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Source = source,
            });
        }
    }

    public async Task<ConsentStatusDto> GetUserStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var activeDocs = await _legalDocs.GetActiveDocumentsAsync(ct);
        var requiredDocs = activeDocs.Where(d => d.IsRequired).ToList();
        if (requiredDocs.Count == 0)
            return new ConsentStatusDto(true, new List<MissingConsentDto>());

        var requiredSlugs = requiredDocs.Select(d => d.Slug).ToList();

        var lastConsents = await _db.UserConsents
            .AsNoTracking()
            .Where(c => c.UserId == userId && requiredSlugs.Contains(c.DocumentSlug))
            .GroupBy(c => c.DocumentSlug)
            .Select(g => g.OrderByDescending(x => x.AcceptedAt).First())
            .ToListAsync(ct);

        var lastBySlug = lastConsents.ToDictionary(c => c.DocumentSlug, c => c);

        var missing = new List<MissingConsentDto>();
        foreach (var doc in requiredDocs)
        {
            lastBySlug.TryGetValue(doc.Slug, out var last);
            var lastIsCurrentAccepted = last is not null
                                        && last.Accepted
                                        && string.Equals(last.DocumentVersion, doc.Version, StringComparison.Ordinal);
            if (!lastIsCurrentAccepted)
            {
                missing.Add(new MissingConsentDto(
                    DocumentSlug: doc.Slug,
                    DocumentTitle: doc.Title,
                    CurrentVersion: doc.Version,
                    LastAcceptedVersion: last?.Accepted == true ? last.DocumentVersion : null,
                    ChangeLog: doc.ChangeLog));
            }
        }

        return new ConsentStatusDto(missing.Count == 0, missing);
    }
}

/// <summary>Helpers pour extraire IP réelle (X-Forwarded-For derrière proxy) et UA.</summary>
public static class ConsentRequestContext
{
    public static string? GetClientIp(HttpContext ctx)
    {
        var fwd = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
        {
            var first = fwd.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first)) return first;
        }
        return ctx.Connection.RemoteIpAddress?.ToString();
    }

    public static string? GetUserAgent(HttpContext ctx)
    {
        var ua = ctx.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : ua;
    }
}
