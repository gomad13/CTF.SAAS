using CTF.Api.Contracts.Legal;
using CTF.Api.Services.Legal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTF.Api.Controllers;

/// <summary>
/// Endpoints publics (anonymes) d'accès aux documents légaux. Utilisés par
/// la page d'inscription pour récupérer la version active à présenter à
/// l'utilisateur, et par les pages publiques /legal/[slug].
/// </summary>
[ApiController]
[Route("api/legal")]
[AllowAnonymous]
public sealed class LegalController : ControllerBase
{
    private readonly ILegalDocumentService _legalDocs;

    public LegalController(ILegalDocumentService legalDocs)
    {
        _legalDocs = legalDocs;
    }

    /// <summary>Liste des documents actifs (sans contenu HTML, lite).</summary>
    [HttpGet("documents")]
    public async Task<ActionResult<List<LegalDocumentSummaryDto>>> GetActiveDocuments(CancellationToken ct)
    {
        var docs = await _legalDocs.GetActiveDocumentsAsync(ct);
        var dtos = docs
            .Select(d => new LegalDocumentSummaryDto(d.Slug, d.Title, d.Version, d.IsRequired, d.PublishedAt))
            .ToList();
        return Ok(dtos);
    }

    /// <summary>Document actif complet pour le slug donné.</summary>
    [HttpGet("documents/{slug}")]
    public async Task<ActionResult<LegalDocumentDto>> GetDocument([FromRoute] string slug, CancellationToken ct)
    {
        var doc = await _legalDocs.GetActiveBySlugAsync(slug, ct);
        if (doc is null) return NotFound(new { error = "Document introuvable." });
        return Ok(new LegalDocumentDto(
            doc.Slug, doc.Title, doc.Version, doc.ContentHtml,
            doc.IsRequired, doc.PublishedAt, doc.ChangeLog));
    }
}
