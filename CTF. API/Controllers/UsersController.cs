using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

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

        var items = await _db.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Select(u => new { u.Id, u.Email, u.DisplayName, u.Role, u.IsActive, u.TeamId, u.CreatedAt, u.LastLoginAt })
            .ToListAsync();

        return Ok(items);
    }

    public sealed record CreateUserRequest(
        string Email,
        string Role,
        string? DisplayName,
        Guid? TeamId
    );

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;

        // (optionnel) petite validation
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "Email is required" });

        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(new { error = "Role is required" });

        // Si un TeamId est fourni, on vérifie qu'il appartient au même tenant
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
            DisplayName = req.DisplayName,
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
            // unique email
            return Conflict(new { error = "Email already exists" });
        }

        return CreatedAtAction(nameof(GetAll), new { id = user.Id }, new { user.Id, user.Email, user.Role });
    }
}
