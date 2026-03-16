using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/modules")]
public class ModulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public ModulesController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // GET /api/modules?pathId=...
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid pathId)
    {
        var tenantId = _tenant.TenantId!.Value;

        var items = await _db.Modules
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.PathId == pathId)
            .OrderBy(m => m.SortOrder)
            .Select(m => new { m.Id, m.PathId, m.Title, m.SortOrder, m.CreatedAt })
            .ToListAsync();

        return Ok(items);
    }

    public sealed record CreateModuleRequest(Guid PathId, string Title, int SortOrder);

    // POST /api/modules  ✅ ADMIN ONLY
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModuleRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;

        // Vérifie que le path existe (et appartient au tenant)
        var pathOk = await _db.Paths.AnyAsync(p => p.Id == req.PathId && p.TenantId == tenantId);
        if (!pathOk) return BadRequest(new { error = "PathId not found for this tenant" });

        var module = new Module
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PathId = req.PathId,
            Title = req.Title,
            SortOrder = req.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.Modules.Add(module);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { pathId = req.PathId }, new { module.Id });
    }
}
