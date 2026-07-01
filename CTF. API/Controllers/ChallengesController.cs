using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/challenges")]
public class ChallengesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public ChallengesController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id)
    {
        var tenantId = _tenant.TenantId!.Value;
        var demoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

        var challenge = await _db.Challenges
            .AsNoTracking()
            .Where(c => (c.TenantId == tenantId || c.TenantId == demoTenantId) && c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.ModuleId,
                c.Type,
                c.Title,
                c.Instructions,
                c.Difficulty,
                c.Points,
                c.Status,
                c.InstructionTitle,
                c.InstructionBody,
                c.InstructionShortReminder
            })
            .SingleOrDefaultAsync();

        if (challenge is null) return NotFound();
        return Ok(challenge);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid moduleId)
    {
        var tenantId = _tenant.TenantId!.Value;

        var items = await _db.Challenges
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.ModuleId == moduleId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ChallengeDto(c.Id, c.ModuleId, c.Type, c.Title, c.Difficulty, c.Points, c.Status, c.CreatedAt, c.PublishedAt, c.InstructionTitle, c.InstructionBody, c.InstructionShortReminder))
            .ToListAsync();

        return Ok(items);
    }

    public sealed record CreateChallengeRequest(
        Guid ModuleId,
        string Type,
        string Title,
        string Instructions,
        int? Difficulty,
        int Points,
        string Status,
        string? CorrectAnswer
    );

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChallengeRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;

        // Garde précoce : pré-valide les chaînes obligatoires pour que le compilateur
        // sache qu'après ce point Type/Title/Instructions/Status sont non-null
        // (évite les warnings CS8601 sur les assignations à l'entité Challenge).
        if (string.IsNullOrWhiteSpace(req.Type)
            || string.IsNullOrWhiteSpace(req.Title)
            || string.IsNullOrWhiteSpace(req.Instructions)
            || string.IsNullOrWhiteSpace(req.Status))
            return BadRequest(new { error = "Type, Title, Instructions and Status are required." });

        var moduleOk = await _db.Modules.AnyAsync(m => m.Id == req.ModuleId && m.TenantId == tenantId);
        if (!moduleOk) return BadRequest(new { error = "ModuleId not found for this tenant" });

        if (req.Points <= 0)
            return BadRequest(new { error = "Points must be greater than 0." });

        if (req.Difficulty.HasValue && (req.Difficulty < 1 || req.Difficulty > 5))
            return BadRequest(new { error = "Difficulty must be between 1 and 5." });

        string[] allowedTypes = { "quiz", "flag", "scenario", "code" };
        if (!allowedTypes.Contains(req.Type.ToLowerInvariant()))
            return BadRequest(new { error = $"Invalid type. Allowed: {string.Join(", ", allowedTypes)}" });

        string[] allowedStatuses = { "draft", "published", "archived" };
        if (!allowedStatuses.Contains(req.Status.ToLowerInvariant()))
            return BadRequest(new { error = "Invalid status. Allowed: draft, published, archived" });

        var createdBy = User.GetUserId();

        // ✅ CORRECTION : Challenge au singulier
        var challenge = new Challenge
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ModuleId = req.ModuleId,
            Type = req.Type,
            Title = req.Title,
            Instructions = req.Instructions,
            Difficulty = req.Difficulty,
            Points = req.Points <= 0 ? 10 : req.Points,
            Status = req.Status,
            CorrectAnswer = req.CorrectAnswer,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.Challenges.Add(challenge);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { moduleId = req.ModuleId }, new { challenge.Id });
    }
}