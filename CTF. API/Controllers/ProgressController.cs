using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using CTF.Api.Services;
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
    private readonly ProgressCalculationService _progressCalc;

    public ProgressController(AppDbContext db, TenantContext tenant, ProgressCalculationService progressCalc)
    {
        _db = db;
        _tenant = tenant;
        _progressCalc = progressCalc;
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

    // Note : Status/Percent du client ne sont plus utilisés ; la progression est calculée serveur.
    public sealed record UpsertProgressRequest(Guid PathId, string? Status = null, int Percent = 0);

    // POST /api/progress (upsert : progression recalculée côté serveur)
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertProgressRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId = User.GetUserId();

        // [PENTEST] progression calculee serveur, valeurs client ignorees
        // On ne fait jamais confiance à req.Percent / req.Status : la progression est
        // (re)calculée et persistée à partir des challenges réellement complétés par ce user.
        await _progressCalc.RecalculateAndPersistAsync(userId, req.PathId, tenantId);

        var entity = await _db.Progresses
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.UserId == userId && p.PathId == req.PathId)
            .SingleOrDefaultAsync();

        if (entity is null)
            return NotFound();

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
