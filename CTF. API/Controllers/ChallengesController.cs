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

    // GET /api/challenges?moduleId=...
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid moduleId)
    {
        var tenantId = _tenant.TenantId!.Value;

        var items = await _db.Challenges
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.ModuleId == moduleId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.ModuleId,
                c.Type,
                c.Title,
                c.Difficulty,
                c.Points,
                c.Status,
                c.CreatedAt,
                c.PublishedAt
            })
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
        string Status
    );

    // POST /api/challenges  ✅ ADMIN ONLY
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChallengeRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;

        // Vérifie que le module existe (et appartient au tenant)
        var moduleOk = await _db.Modules.AnyAsync(m => m.Id == req.ModuleId && m.TenantId == tenantId);
        if (!moduleOk) return BadRequest(new { error = "ModuleId not found for this tenant" });

        // ✅ user connecté via JWT
        var createdBy = User.GetUserId();

        var challenge = new Challenges
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

            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.Challenges.Add(challenge);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { moduleId = req.ModuleId }, new { challenge.Id });
    }
}
