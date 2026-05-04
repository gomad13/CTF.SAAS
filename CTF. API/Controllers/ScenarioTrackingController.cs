using CTF.Api.Data;
using CTF.Api.Services.Scenarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Endpoints anonymes pour le tracking des emails simulés. Un GUID v4 dans
/// l'URL fait office d'identifiant unique non-énumérable. Aucun auth check.
/// Le TenantMiddleware bypass la résolution de tenant pour ce préfixe.
/// </summary>
[ApiController]
[Route("api/scenario-tracking")]
[AllowAnonymous]
public class ScenarioTrackingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IScenarioEngine _engine;
    private readonly IConfiguration _config;

    public ScenarioTrackingController(AppDbContext db, IScenarioEngine engine, IConfiguration config)
    {
        _db = db;
        _engine = engine;
        _config = config;
    }

    // GIF transparent 1×1 — bytes hardcodés, 43 octets, RFC GIF89a.
    private static readonly byte[] Pixel = new byte[]
    {
        0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00,
        0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00,
        0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02,
        0x44, 0x01, 0x00, 0x3B
    };

    [HttpGet("open/{token}.gif")]
    public async Task<IActionResult> Open(string token, CancellationToken ct)
    {
        // Renvoie toujours le pixel — on ne révèle jamais si le token est valide
        // ou non (anti-énumération). Si le token correspond, l'event est enregistré.
        var email = await _db.ScenarioEmails.AsNoTracking()
            .FirstOrDefaultAsync(e => e.TrackingToken == token, ct);
        if (email is not null)
        {
            await _engine.RecordOpenAsync(
                email.Id,
                Request.Headers.UserAgent.ToString(),
                Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                ct);
        }

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        return File(Pixel, "image/gif");
    }

    [HttpGet("click/{token}")]
    public async Task<IActionResult> Click(string token, CancellationToken ct)
    {
        var email = await _db.ScenarioEmails.AsNoTracking()
            .FirstOrDefaultAsync(e => e.TrackingToken == token, ct);

        // Pas de token connu → on redirige vers le frontend racine (pas d'oracle).
        var frontUrl = _config["FrontendUrl"] ?? "http://localhost:3000";
        if (email is null)
            return Redirect(frontUrl);

        await _engine.RecordClickAsync(
            email.Id,
            Request.Headers.UserAgent.ToString(),
            Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        // Redirige vers la landing post-clic du frontend, qui charge le coaching
        // (Pilier 4) + la mise à jour CRI (Pilier 5).
        return Redirect($"{frontUrl.TrimEnd('/')}/scenarios/landing/{token}");
    }
}
