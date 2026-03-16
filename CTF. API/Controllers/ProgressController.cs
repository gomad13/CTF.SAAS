using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/progress")]
public class ProgressController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public ProgressController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // GET /api/progress?pathId=...
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid pathId)
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId = User.GetUserId();

        var item = await _db.Progresses
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.UserId == userId && p.PathId == pathId)
            .Select(p => new
            {
                p.Id,
                p.UserId,
                p.PathId,
                p.Status,
                p.Percent,
                p.StartedAt,
                p.CompletedAt,
                p.UpdatedAt
            })
            .SingleOrDefaultAsync();

        return item is null ? NotFound() : Ok(item);
    }

    // ✅ On ne prend plus UserId depuis le client
    public sealed record UpsertProgressRequest(Guid PathId, string Status, int Percent);

    // POST /api/progress (upsert simple)
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertProgressRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId = User.GetUserId();

        var now = DateTime.UtcNow;

        var entity = await _db.Progresses
            .Where(p => p.TenantId == tenantId && p.UserId == userId && p.PathId == req.PathId)
            .SingleOrDefaultAsync();

        if (entity is null)
        {
            entity = new Progress
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                PathId = req.PathId,
                Status = req.Status,
                Percent = Math.Clamp(req.Percent, 0, 100),
                StartedAt = req.Status == "not_started" ? null : now,
                CompletedAt = req.Status == "completed" ? now : null,
                UpdatedAt = now
            };

            _db.Progresses.Add(entity);
        }
        else
        {
            entity.Status = req.Status;
            entity.Percent = Math.Clamp(req.Percent, 0, 100);
            entity.StartedAt ??= (req.Status == "not_started" ? null : now);
            entity.CompletedAt = req.Status == "completed" ? (entity.CompletedAt ?? now) : null;
            entity.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            entity.Id,
            entity.UserId,
            entity.PathId,
            entity.Status,
            entity.Percent,
            entity.StartedAt,
            entity.CompletedAt,
            entity.UpdatedAt
        });
    }
}
