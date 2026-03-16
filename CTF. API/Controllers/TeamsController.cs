using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/teams")]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public TeamsController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = _tenant.TenantId!.Value;

        var items = await _db.Teams
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .Select(t => new { t.Id, t.Name, t.CreatedAt })
            .ToListAsync();

        return Ok(items);
    }

    public sealed record CreateTeamRequest(string Name);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;

        var team = new Team
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = req.Name,
            CreatedAt = DateTime.UtcNow
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = team.Id }, new { team.Id, team.Name, team.CreatedAt });
    }
}
