using CTF.Api.Contracts.Scenarios;
using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services.Scenarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Inbox interne Viper — boîte de réception simulée pour les scénarios narratifs.
/// Chaque employé authentifié voit uniquement ses propres emails (filtre user + tenant).
/// </summary>
[ApiController]
[Route("api/inbox")]
[Authorize(Roles = "user,admin,SuperAdmin")]
public class InboxController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IScenarioEngine _engine;

    public InboxController(AppDbContext db, IScenarioEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<InboxEmailListItemDto>>> ListMine(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        var rows = await _db.ScenarioEmails.AsNoTracking()
            .Where(e => e.RecipientUserId == userId && e.TenantId == tenantId)
            .OrderByDescending(e => e.SentAt)
            .Take(200)
            .Select(e => new InboxEmailListItemDto(
                e.Id, e.Subject, e.FromName, e.FromEmail, e.SentAt,
                e.FirstReadAt != null, e.ReportedAt != null, e.IsSystemNotification))
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("me/{id:guid}")]
    public async Task<ActionResult<InboxEmailDetailDto>> GetMine(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        var email = await _db.ScenarioEmails
            .FirstOrDefaultAsync(e => e.Id == id && e.RecipientUserId == userId && e.TenantId == tenantId, ct);
        if (email is null) return NotFound();

        // Marquer comme lu (premier accès uniquement). On NE déclenche PAS
        // l'event "opened" via l'engine ici — l'open-pixel API est la source
        // canonique : un employé qui consulte l'email sans charger d'image
        // (mode dev/Postman) ne déclenche que ce mark-as-read côté inbox,
        // pas l'event de tracking complet (qui exigerait un user-agent + IP).
        if (email.FirstReadAt is null)
        {
            email.FirstReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new InboxEmailDetailDto(
            email.Id, email.Subject, email.FromName, email.FromEmail,
            email.BodyHtml, email.SentAt,
            email.FirstReadAt != null, email.ReportedAt != null, email.IsSystemNotification));
    }

    [HttpPost("me/{id:guid}/report")]
    public async Task<ActionResult<ReportPhishingResponse>> ReportMine(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();
        var resp = await _engine.RecordReportAsync(id, userId, tenantId, ct);
        if (!resp.Success) return NotFound(resp);
        return Ok(resp);
    }
}
