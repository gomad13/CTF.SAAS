using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Édition admin des consignes pédagogiques d'un challenge (V1 light).
/// Multi-tenant strict : un admin n'édite que les challenges de son tenant.
/// </summary>
[ApiController]
[Route("api/admin/challenges")]
[Authorize(Roles = "admin,SuperAdmin")]
public class AdminChallengeInstructionsController : ControllerBase
{
    private const int MaxTitleLength = 200;
    private const int MaxReminderLength = 300;

    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public AdminChallengeInstructionsController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // ── PUT /api/admin/challenges/{id}/instructions ───────────────────────────
    [HttpPut("{id:guid}/instructions")]
    public async Task<IActionResult> UpdateInstructions(
        Guid id,
        [FromBody] UpdateInstructionsRequest request,
        CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        // Validation des longueurs (le corps reste libre).
        var title = Normalize(request.InstructionTitle);
        var reminder = Normalize(request.InstructionShortReminder);
        var body = Normalize(request.InstructionBody);

        if (title is { Length: > MaxTitleLength })
            return BadRequest(new { error = $"InstructionTitle dépasse {MaxTitleLength} caractères." });
        if (reminder is { Length: > MaxReminderLength })
            return BadRequest(new { error = $"InstructionShortReminder dépasse {MaxReminderLength} caractères." });

        // Multi-tenant strict : on ne touche qu'aux challenges du tenant courant.
        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

        if (challenge is null) return NotFound();

        challenge.InstructionTitle = title;
        challenge.InstructionBody = body;
        challenge.InstructionShortReminder = reminder;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            challenge.Id,
            challenge.InstructionTitle,
            challenge.InstructionBody,
            challenge.InstructionShortReminder
        });
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
