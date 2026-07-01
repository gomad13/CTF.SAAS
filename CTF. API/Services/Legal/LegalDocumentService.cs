using CTF.Api.Data;
using CTF.Api.Models.Legal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CTF.Api.Services.Legal;

public interface ILegalDocumentService
{
    /// <summary>Liste des documents actifs, dédoublonnée : un seul item par slug, le plus récent par PublishedAt.</summary>
    Task<List<LegalDocument>> GetActiveDocumentsAsync(CancellationToken ct = default);

    /// <summary>Document actif pour le slug donné. Si la BDD contient (par erreur) plusieurs lignes IsActive=true pour ce slug, retourne celle dont PublishedAt est le plus récent.</summary>
    Task<LegalDocument?> GetActiveBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Publie une nouvelle version. Désactive en BDD toutes les versions actives existantes du même slug dans une transaction atomique, puis insère la nouvelle. Invalide le cache.</summary>
    Task<LegalDocument> PublishNewVersionAsync(LegalDocument newVersion, CancellationToken ct = default);

    /// <summary>Invalide le cache mémoire (à appeler après toute mutation directe en BDD hors PublishNewVersion).</summary>
    void Invalidate();
}

/// <summary>
/// Cache mémoire des documents légaux actifs (TTL 5 min). Les documents
/// changent rarement et sont lus à chaque requête authentifiée par le
/// middleware de consentement, donc le cache est crucial pour la perf.
///
/// Robustesse : si la BDD se retrouve dans un état dégradé avec plusieurs
/// versions IsActive=true pour un même slug (oubli de désactivation, INSERT
/// manuel, race condition), la query DB et le filtrage en mémoire dédoublonnent
/// par (Slug, PublishedAt DESC) → on retourne toujours la plus récente.
/// </summary>
public sealed class LegalDocumentService : ILegalDocumentService
{
    private const string ActiveDocsCacheKey = "legal:active-docs";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public LegalDocumentService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<LegalDocument>> GetActiveDocumentsAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(ActiveDocsCacheKey, out List<LegalDocument>? cached) && cached is not null)
            return cached;

        // Tri (Slug ASC, PublishedAt DESC) : si plusieurs lignes IsActive=true
        // existent pour le même slug, le GroupBy en mémoire pioche la plus
        // récente. C'est défensif — la situation normale est 1 active par slug.
        var rows = await _db.LegalDocuments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Slug)
            .ThenByDescending(d => d.PublishedAt)
            .ToListAsync(ct);

        var docs = rows
            .GroupBy(d => d.Slug)
            .Select(g => g.First())
            .ToList();

        _cache.Set(ActiveDocsCacheKey, docs, CacheTtl);
        return docs;
    }

    public async Task<LegalDocument?> GetActiveBySlugAsync(string slug, CancellationToken ct = default)
    {
        // Lecture directe DB (pas via cache liste) pour bénéficier d'un index
        // (Slug, IsActive, PublishedAt) et ne pas charger toute la liste si
        // le cache est froid. Le tri DESC est OBLIGATOIRE pour gérer la
        // situation dégradée multi-actives.
        return await _db.LegalDocuments
            .AsNoTracking()
            .Where(d => d.Slug == slug && d.IsActive)
            .OrderByDescending(d => d.PublishedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<LegalDocument> PublishNewVersionAsync(LegalDocument newVersion, CancellationToken ct = default)
    {
        if (newVersion is null) throw new ArgumentNullException(nameof(newVersion));
        if (string.IsNullOrWhiteSpace(newVersion.Slug)) throw new ArgumentException("Slug requis.", nameof(newVersion));
        if (string.IsNullOrWhiteSpace(newVersion.Version)) throw new ArgumentException("Version requise.", nameof(newVersion));

        // Normalisation Slug + Version : retire whitespace/BOM/NBSP/ZWS susceptibles
        // d'être glissés par un copier-coller depuis un éditeur web ou un fichier
        // SQL avec BOM. Sans ça, la comparaison Ordinal côté validation casse.
        newVersion.Slug = LegalNormalization.NormalizeSlug(newVersion.Slug);
        newVersion.Version = LegalNormalization.NormalizeVersion(newVersion.Version);

        // Le provider InMemory utilisé en tests ne supporte pas les transactions ;
        // on les utilise uniquement quand le provider est relationnel.
        var supportsTx = _db.Database.IsRelational();
        var tx = supportsTx ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            // Désactiver toutes les versions actuellement actives du même slug.
            // ExecuteUpdateAsync : idéal en relationnel, pas dispo en InMemory →
            // fallback sur le tracking classique pour conserver la testabilité.
            if (supportsTx)
            {
                await _db.LegalDocuments
                    .Where(d => d.Slug == newVersion.Slug && d.IsActive)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsActive, false), ct);
            }
            else
            {
                var actives = await _db.LegalDocuments
                    .Where(d => d.Slug == newVersion.Slug && d.IsActive)
                    .ToListAsync(ct);
                foreach (var a in actives) a.IsActive = false;
            }

            if (newVersion.Id == Guid.Empty) newVersion.Id = Guid.NewGuid();
            if (newVersion.PublishedAt == default) newVersion.PublishedAt = DateTime.UtcNow;
            newVersion.IsActive = true;

            _db.LegalDocuments.Add(newVersion);
            await _db.SaveChangesAsync(ct);

            if (tx is not null) await tx.CommitAsync(ct);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }

        Invalidate();
        return newVersion;
    }

    public void Invalidate() => _cache.Remove(ActiveDocsCacheKey);
}
