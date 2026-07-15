using System.Text.RegularExpressions;
using CTF.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CTF.Api.Controllers;

/// <summary>
/// Formulaire de contact / support public. Envoie un message à l'adresse support
/// configurée (Mail:SupportEmail) via <see cref="IMailService"/>. Aucun open relay :
/// le destinataire est FIXE (config serveur), le reply-to = email fourni.
/// </summary>
[ApiController]
[Route("api/support")]
public class SupportController : ControllerBase
{
    private readonly IMailService _mail;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SupportController> _logger;

    // Email basique (validation format, pas de vérité métier).
    private static readonly Regex EmailRx =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public SupportController(IMailService mail, IMemoryCache cache, ILogger<SupportController> logger)
    {
        _mail = mail;
        _cache = cache;
        _logger = logger;
    }

    public record SupportRequest(string Email, string Subject, string Message);

    [HttpPost("contact")]
    public async Task<IActionResult> Contact([FromBody] SupportRequest req, CancellationToken ct)
    {
        var email   = req.Email?.Trim() ?? "";
        var subject = req.Subject?.Trim() ?? "";
        var message = req.Message?.Trim() ?? "";

        // Validation champs + longueurs (anti-abus).
        if (!EmailRx.IsMatch(email) || email.Length > 254)
            return BadRequest(new { error = "Adresse email invalide." });
        if (subject.Length < 3 || subject.Length > 150)
            return BadRequest(new { error = "Le sujet doit contenir entre 3 et 150 caractères." });
        if (message.Length < 10 || message.Length > 5000)
            return BadRequest(new { error = "Le message doit contenir entre 10 et 5000 caractères." });

        // Anti-injection d'en-têtes mail : refuser CR/LF dans email et sujet.
        if (HasHeaderInjection(email) || HasHeaderInjection(subject))
            return BadRequest(new { error = "Caractères non autorisés détectés." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (IsRateLimited($"support_ip_{ip}") || IsRateLimited($"support_mail_{email.ToLowerInvariant()}"))
            return StatusCode(429, new { error = "Trop de messages envoyés. Merci de patienter avant de réessayer." });

        // Le service échappe le contenu (HtmlEncode) — destinataire fixe côté serveur.
        await _mail.SendSupportMessageAsync(email, subject, message, ct);
        _logger.LogInformation("[SUPPORT] message reçu de {Email} (sujet: {Len} car.)", email, subject.Length);

        return Ok(new { message = "Votre message a bien été envoyé. Notre équipe vous répondra rapidement." });
    }

    private static bool HasHeaderInjection(string s) =>
        s.Contains('\r') || s.Contains('\n');

    // Max 5 messages / 15 min par clé (IP ou email).
    private bool IsRateLimited(string key)
    {
        var count = _cache.GetOrCreate(key, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            return 0;
        });
        if (count >= 5) return true;
        _cache.Set(key, count + 1, TimeSpan.FromMinutes(15));
        return false;
    }
}
