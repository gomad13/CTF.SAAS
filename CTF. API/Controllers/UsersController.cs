using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

// ✅ CORRECTION : [Authorize] ajouté sur tout le controller
[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public UsersController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = _tenant.TenantId!.Value;
        var isAdmin = User.IsInRole("admin") || User.IsInRole("SuperAdmin");

        var query = _db.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId);

        if (!isAdmin)
        {
            var userId = User.GetUserId();
            query = query.Where(u => u.Id == userId);
        }

        var items = await query
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                u.FirstName,
                u.LastName,
                u.Role,
                u.IsActive,
                u.TeamId,
                u.CreatedAt,
                u.LastLoginAt
            })
            .ToListAsync();

        return Ok(items);
    }

    // ✅ CORRECTION : FirstName et LastName ajoutés
    public sealed record CreateUserRequest(
        string Email,
        string Role,
        string FirstName,
        string LastName,
        string? DisplayName,
        Guid? TeamId
    );

    // ✅ CORRECTION : Admin only pour créer un user
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;

        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "Email is required" });

        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(new { error = "Role is required" });

        if (string.IsNullOrWhiteSpace(req.FirstName))
            return BadRequest(new { error = "FirstName is required" });

        if (string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest(new { error = "LastName is required" });

        if (req.TeamId.HasValue)
        {
            var teamOk = await _db.Teams.AnyAsync(t => t.Id == req.TeamId.Value && t.TenantId == tenantId);
            if (!teamOk) return BadRequest(new { error = "TeamId not found for this tenant" });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TeamId = req.TeamId,
            Email = req.Email.Trim().ToLowerInvariant(),
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            DisplayName = req.DisplayName ?? $"{req.FirstName.Trim()} {req.LastName.Trim()}",
            Role = req.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "Email already exists" });
        }

        return CreatedAtAction(nameof(GetAll), new { id = user.Id }, new { user.Id, user.Email, user.Role });
    }

    // ── Pilier 1 — Consentement « expéditeur fictif » ───────────────────────
    // L'employé connecté gère SON consentement uniquement. Aucun admin ne peut
    // toggle ce flag pour quelqu'un d'autre via cet endpoint (RGPD).
    [HttpGet("me/sender-consent")]
    public async Task<ActionResult<object>> GetMySenderConsent()
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();
        var consent = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .Select(u => (bool?)u.ConsentsToBeFictionalSender)
            .FirstOrDefaultAsync();
        if (consent is null) return NotFound();
        return Ok(new { consentsToBeFictionalSender = consent.Value });
    }

    public sealed record UpdateSenderConsentRequest(bool ConsentsToBeFictionalSender);

    [HttpPut("me/sender-consent")]
    public async Task<IActionResult> UpdateMySenderConsent([FromBody] UpdateSenderConsentRequest req)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (user is null) return NotFound();
        user.ConsentsToBeFictionalSender = req.ConsentsToBeFictionalSender;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { consentsToBeFictionalSender = user.ConsentsToBeFictionalSender });
    }
}