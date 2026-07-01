using System.Text;
using CTF.Api.Data;
using CTF.Api.Models.Legal;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services.Legal;

public interface ILegalDocumentSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}

/// <summary>
/// Seed des 4 documents légaux Sentys (politique, CGU, DPA, mentions légales)
/// au démarrage. Idempotent : ne crée un document que si la combinaison
/// (Slug, Version) n'existe pas déjà. Le contenu HTML est lu depuis les
/// fichiers Resources/Legal/*.html copiés à côté du binaire.
/// </summary>
public sealed class LegalDocumentSeeder : ILegalDocumentSeeder
{
    private const string SeedVersion = "1.0.0";
    private static readonly DateTime SeedPublishedAt = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly AppDbContext _db;
    private readonly ILogger<LegalDocumentSeeder> _logger;

    private static readonly (string Slug, string Title, string FileName, bool IsRequired)[] Documents =
    {
        (LegalDocumentSlugs.PolitiqueConfidentialite, "Politique de Confidentialité", "politique-confidentialite.html", true),
        (LegalDocumentSlugs.Cgu, "Conditions Générales d'Utilisation", "cgu.html", true),
        (LegalDocumentSlugs.Dpa, "Accord de Traitement des Données", "dpa.html", true),
        (LegalDocumentSlugs.MentionsLegales, "Mentions Légales", "mentions-legales.html", false),
    };

    public LegalDocumentSeeder(AppDbContext db, ILogger<LegalDocumentSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Legal");
        if (!Directory.Exists(baseDir))
        {
            _logger.LogWarning("Resources/Legal introuvable ({Dir}). Seed ignoré.", baseDir);
            return;
        }

        var seededCount = 0;
        foreach (var doc in Documents)
        {
            var exists = await _db.LegalDocuments
                .AsNoTracking()
                .AnyAsync(d => d.Slug == doc.Slug && d.Version == SeedVersion, ct);

            if (exists) continue;

            var path = Path.Combine(baseDir, doc.FileName);
            if (!File.Exists(path))
            {
                _logger.LogWarning("Fichier {File} introuvable, document {Slug} ignoré.", path, doc.Slug);
                continue;
            }

            // Encoding UTF-8 explicite : la valeur par défaut de File.ReadAllText
            // varie selon la plateforme et la présence de BOM. On force UTF-8
            // pour garantir que les caractères accentués (« é », « à », « œ ») se
            // retrouvent correctement en BDD, peu importe l'environnement de build.
            var html = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);

            _db.LegalDocuments.Add(new LegalDocument
            {
                Id = Guid.NewGuid(),
                Slug = LegalNormalization.NormalizeSlug(doc.Slug),
                Title = doc.Title,
                Version = LegalNormalization.NormalizeVersion(SeedVersion),
                ContentHtml = html,
                ContentMarkdown = "",
                IsRequired = doc.IsRequired,
                IsActive = true,
                PublishedAt = SeedPublishedAt,
                ChangeLog = "Version initiale.",
            });
            seededCount++;
        }

        if (seededCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("{Count} document(s) légaux seedés en version {Version}.", seededCount, SeedVersion);
        }

        await CleanupDuplicateActivesAsync(ct);
    }

    /// <summary>
    /// Nettoyage défensif : si un slug a plusieurs versions IsActive=true (état
    /// dégradé suite à un INSERT manuel sans désactivation), garde uniquement
    /// la plus récente par PublishedAt active. Trim aussi les Slug et Version
    /// pour retirer un BOM/NBSP/whitespace glissé par un INSERT manuel
    /// (cause documentée du bug "version reçue n'est pas la version active"
    /// quand <c>length(Version)</c> en BDD vaut 6 au lieu de 5 pour "1.0.0").
    /// Idempotent : si la BDD est saine, ne touche rien.
    /// </summary>
    private async Task CleanupDuplicateActivesAsync(CancellationToken ct)
    {
        if (!_db.Database.IsRelational()) return;

        // 1) Sanitiser les Slug et Version pollués (BOM, NBSP, whitespace).
        //    On charge en tracking et on compare valeur normalisée vs valeur brute.
        var allDocs = await _db.LegalDocuments.ToListAsync(ct);
        var sanitizedCount = 0;
        foreach (var d in allDocs)
        {
            var cleanSlug = LegalNormalization.NormalizeSlug(d.Slug);
            var cleanVersion = LegalNormalization.NormalizeVersion(d.Version);
            if (cleanSlug != d.Slug) { d.Slug = cleanSlug; sanitizedCount++; }
            if (cleanVersion != d.Version) { d.Version = cleanVersion; sanitizedCount++; }
        }
        if (sanitizedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning(
                "{Count} valeur(s) Slug/Version contenaient des caractères invisibles (BOM/NBSP/whitespace) — sanitisées au démarrage.",
                sanitizedCount);
        }

        // 2) Si plusieurs IsActive=true par slug, garder la plus récente seule.
        var rows = await _db.LegalDocuments
            .Where(d => d.IsActive)
            .OrderBy(d => d.Slug)
            .ThenByDescending(d => d.PublishedAt)
            .ToListAsync(ct);

        var toDeactivate = rows
            .GroupBy(d => d.Slug)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        if (toDeactivate.Count == 0) return;

        foreach (var d in toDeactivate) d.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning(
            "{Count} version(s) légale(s) doublement actives ont été désactivées au démarrage (cleanup défensif).",
            toDeactivate.Count);
    }
}
