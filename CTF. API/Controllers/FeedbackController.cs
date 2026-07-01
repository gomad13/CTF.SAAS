using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CTF.Api.Controllers;

/// <summary>
/// Feedback utilisateur — endpoint public pour envoyer un message,
/// endpoints SuperAdmin pour consulter et répondre.
/// </summary>
[ApiController]
[Route("api")]
public class FeedbackController : ControllerBase
{
    private static readonly HashSet<string> AllowedSubjects = new(StringComparer.OrdinalIgnoreCase)
    {
        "bug", "amelioration", "question", "autre"
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "new", "read", "responded", "archived"
    };

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(AppDbContext db, IMemoryCache cache, ILogger<FeedbackController> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public record SubmitFeedbackRequest(
        string? Email,
        string? Name,
        string? Subject,
        string Message,
        string? Page);

    /// <summary>
    /// Public — envoi d'un feedback. Rate-limit applicatif : 3 messages / 10 min par IP.
    /// </summary>
    [HttpPost("feedback")]
    [AllowAnonymous]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "Body manquant." });

        var message = (req.Message ?? string.Empty).Trim();
        if (message.Length < 10)
            return BadRequest(new { error = "Le message doit contenir au moins 10 caractères." });
        if (message.Length > 5000)
            return BadRequest(new { error = "Le message ne peut pas dépasser 5000 caractères." });

        var subject = (req.Subject ?? "autre").Trim().ToLowerInvariant();
        if (!AllowedSubjects.Contains(subject))
            subject = "autre";

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateKey = $"feedback:rate:{ip}";
        var attempts = _cache.Get<int?>(rateKey) ?? 0;
        if (attempts >= 3)
            return StatusCode(429, new { error = "Trop de feedbacks récents. Réessayez dans 10 minutes." });
        _cache.Set(rateKey, attempts + 1, TimeSpan.FromMinutes(10));

        // Tentative de récupération du contexte authentifié (sans bloquer si non-auth).
        Guid? userId = null;
        Guid? tenantId = null;
        var emailFromContext = (string?)null;
        if (User?.Identity?.IsAuthenticated == true)
        {
            try { userId = User.GetUserId(); } catch { /* ignore */ }
            try { tenantId = User.GetTenantId(); } catch { /* ignore */ }
            emailFromContext = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                            ?? User.FindFirst("email")?.Value;
        }

        var emailFinal = (req.Email ?? emailFromContext ?? "").Trim();
        // Email optionnel (anonyme accepté) — mais si fourni, format minimal.
        if (!string.IsNullOrEmpty(emailFinal) && !emailFinal.Contains('@'))
            return BadRequest(new { error = "Adresse email invalide." });

        var entity = new FeedbackMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Email = emailFinal,
            Name = string.IsNullOrWhiteSpace(req.Name) ? null : req.Name.Trim(),
            Subject = subject,
            Message = message,
            Page = string.IsNullOrWhiteSpace(req.Page) ? null : req.Page.Trim(),
            UserAgent = Request.Headers["User-Agent"].ToString(),
            IpAddress = ip,
            SubmittedAt = DateTime.UtcNow,
            Status = "new",
        };

        _db.FeedbackMessages.Add(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Feedback received from {Email} subject={Subject} id={Id}",
            string.IsNullOrEmpty(emailFinal) ? "(anonymous)" : emailFinal, subject, entity.Id);

        // TODO V1 : envoyer un mail de confirmation au user via MailService (Phase 3 — Brevo)
        //           + une notif au SuperAdmin avec le contenu.
        return Ok(new { ok = true });
    }

    public record FeedbackListItemDto(
        Guid Id,
        string Email,
        string? Name,
        string Subject,
        string Message,
        string? Page,
        DateTime SubmittedAt,
        string Status,
        Guid? TenantId,
        Guid? UserId);

    /// <summary>
    /// SuperAdmin — listing paginé des feedbacks avec filtres.
    /// </summary>
    [HttpGet("superadmin/feedback")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var q = _db.FeedbackMessages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status))
            q = q.Where(f => f.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(f =>
                f.Email.ToLower().Contains(s) ||
                f.Message.ToLower().Contains(s) ||
                (f.Name != null && f.Name.ToLower().Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(f => f.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FeedbackListItemDto(
                f.Id, f.Email, f.Name, f.Subject, f.Message, f.Page, f.SubmittedAt,
                f.Status, f.TenantId, f.UserId))
            .ToListAsync(ct);

        return Ok(new PagedResult<FeedbackListItemDto>(items, page, pageSize, total));
    }

    public record UpdateStatusRequest(string Status, string? AdminNotes);

    /// <summary>
    /// SuperAdmin — change le statut d'un feedback (read / responded / archived).
    /// </summary>
    [HttpPatch("superadmin/feedback/{id:guid}/status")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "Body manquant." });
        if (!AllowedStatuses.Contains(req.Status))
            return BadRequest(new { error = "Statut invalide. Attendu : new | read | responded | archived." });

        var entity = await _db.FeedbackMessages.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entity is null) return NotFound();

        entity.Status = req.Status.ToLowerInvariant();
        if (req.AdminNotes is not null)
            entity.AdminNotes = req.AdminNotes;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }
}
