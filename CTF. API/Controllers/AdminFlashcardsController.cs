using System.Text.Json;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Admin : ajouter un module « Flashcards » (mode ÉPREUVE) à un parcours du tenant.
/// Réutilise le ContentType existant `flash_cards` + le scoring `submit-flash-cards`.
/// Noté = Points > 0 ; Non noté = Points = 0 (complète l'étape sans impacter le barème).
/// </summary>
[ApiController]
[Route("api/admin/paths")]
[Authorize(Roles = "admin,SuperAdmin")]
public class AdminFlashcardsController : ControllerBase
{
    private readonly AppDbContext _db;
    private const int NotePoints = 10; // barème standard d'un exercice noté

    public AdminFlashcardsController(AppDbContext db) => _db = db;

    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // ── GET /api/admin/paths/editable — parcours du tenant (non-catalogue) + modules ──
    [HttpGet("editable")]
    public async Task<ActionResult<List<EditablePathDto>>> GetEditablePaths(CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        if (tenantId == Guid.Empty) return Unauthorized();

        var paths = await _db.Paths.AsNoTracking()
            .Where(p => p.TenantId == tenantId && !p.IsCatalog && p.Status != "archived")
            .OrderBy(p => p.Title)
            .Select(p => new { p.Id, p.Title })
            .ToListAsync(ct);
        var pathIds = paths.Select(p => p.Id).ToList();

        var modules = await _db.Modules.AsNoTracking()
            .Where(m => pathIds.Contains(m.PathId))
            .Select(m => new { m.Id, m.PathId, m.Title, m.SortOrder })
            .ToListAsync(ct);
        var moduleIds = modules.Select(m => m.Id).ToList();

        var counts = await _db.Challenges.AsNoTracking()
            .Where(c => moduleIds.Contains(c.ModuleId))
            .GroupBy(c => c.ModuleId)
            .Select(g => new { ModuleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ModuleId, x => x.Count, ct);

        var result = paths.Select(p => new EditablePathDto(
            p.Id, p.Title,
            modules.Where(m => m.PathId == p.Id).OrderBy(m => m.SortOrder)
                .Select(m => new EditablePathModuleDto(m.Id, m.Title, m.SortOrder, counts.GetValueOrDefault(m.Id, 0)))
                .ToList()
        )).ToList();

        return Ok(result);
    }

    // ── POST /api/admin/paths/modules/{moduleId}/flashcards-epreuve — créer le module flashcards ──
    [HttpPost("modules/{moduleId:guid}/flashcards-epreuve")]
    public async Task<ActionResult<CreatedFlashcardsDto>> CreateFlashcardsEpreuve(
        Guid moduleId, [FromBody] CreateFlashcardsEpreuveRequest req, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId = User.GetUserId();
        if (tenantId == Guid.Empty) return Unauthorized();

        // Whitelist / validation (jamais confiance au client)
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest(new { error = "Titre requis." });
        if (req.Cards is null || req.Cards.Count == 0) return BadRequest(new { error = "Au moins une carte requise." });
        foreach (var c in req.Cards)
        {
            if (string.IsNullOrWhiteSpace(c.Front) || c.Choices is null || c.Choices.Count < 2)
                return BadRequest(new { error = "Chaque carte : une question et au moins 2 choix." });
            if (c.CorrectIndex < 0 || c.CorrectIndex >= c.Choices.Count)
                return BadRequest(new { error = "Index de bonne réponse invalide." });
        }

        var module = await _db.Modules.AsNoTracking().FirstOrDefaultAsync(m => m.Id == moduleId, ct);
        if (module is null) return NotFound(new { error = "Module introuvable." });

        // Le parcours doit appartenir au tenant (pas de catalogue partagé, pas d'autre tenant)
        var path = await _db.Paths.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == module.PathId && p.TenantId == tenantId && !p.IsCatalog, ct);
        if (path is null) return Forbid();

        var contentJson = JsonSerializer.Serialize(new
        {
            subtype = "epreuve",
            note = req.Note,
            instructions = "Répondez au QCM. Feedback immédiat juste / faux, chrono et score en fin d'épreuve.",
            cards = req.Cards.Select(c => new
            {
                id = string.IsNullOrWhiteSpace(c.Id) ? Guid.NewGuid().ToString("N")[..8] : c.Id,
                category = c.Category ?? "",
                front = c.Front,
                back = c.Back ?? "",
                choices = c.Choices,
                correctIndex = c.CorrectIndex
            }).ToList()
        }, CamelCase);

        var nextSort = await _db.Challenges.AsNoTracking()
            .Where(c => c.ModuleId == moduleId).Select(c => (int?)c.SortOrder).MaxAsync(ct) ?? 0;

        var challenge = new Challenge
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ModuleId = moduleId,
            Type = "interactive",
            ContentType = "flash_cards",
            ContentJson = contentJson,
            Title = req.Title.Trim(),
            Instructions = "Épreuve flashcards.",
            Points = req.Note ? NotePoints : 0,   // Non noté = 0 point → aucun impact barème
            SortOrder = nextSort + 1,
            Status = "published",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow
        };
        _db.Challenges.Add(challenge);
        await _db.SaveChangesAsync(ct);

        return Ok(new CreatedFlashcardsDto(challenge.Id, challenge.Title, req.Note, req.Cards.Count));
    }
}

public record FlashcardEpreuveCardDto(string? Id, string? Category, string Front, string? Back, List<string> Choices, int CorrectIndex);
public record CreateFlashcardsEpreuveRequest(string Title, bool Note, List<FlashcardEpreuveCardDto> Cards);
public record CreatedFlashcardsDto(Guid ChallengeId, string Title, bool Note, int CardCount);
public record EditablePathModuleDto(Guid Id, string Title, int SortOrder, int ChallengeCount);
public record EditablePathDto(Guid Id, string Title, List<EditablePathModuleDto> Modules);
