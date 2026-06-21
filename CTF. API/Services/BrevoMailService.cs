using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CTF.Api.Data;
using CTF.Api.Models;

namespace CTF.Api.Services;

/// <summary>
/// V3 — Envoi d'emails transactionnels via l'API HTTP Brevo (https://www.brevo.com).
/// Activée seulement quand <c>Mail:Provider = "Brevo"</c> ET <c>Mail:BrevoApiKey</c> est présente
/// (cf. Program.cs) ; sinon on reste sur <see cref="LogOnlyMailService"/>.
///
/// Sécurité : la clé API n'est JAMAIS loguée ni renvoyée. Elle est lue depuis la config
/// (idéalement la variable d'environnement <c>Mail__BrevoApiKey</c>, jamais commitée).
/// Chaque envoi est tracé dans <see cref="MailLog"/> (status "sent" / "failed:*"), sans la clé.
/// </summary>
public class BrevoMailService : IMailService
{
    private const string ApiUrl = "https://api.brevo.com/v3/smtp/email";

    private readonly HttpClient _http;
    private readonly ILogger<BrevoMailService> _logger;
    private readonly AppDbContext _db;
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public BrevoMailService(HttpClient http, IConfiguration config, ILogger<BrevoMailService> logger, AppDbContext db)
    {
        _http = http;
        _logger = logger;
        _db = db;
        _apiKey = config["Mail:BrevoApiKey"] ?? "";
        _senderEmail = config["Mail:SenderEmail"] ?? "noreply@sentys.fr";
        _senderName = config["Mail:SenderName"] ?? "Sentys";
    }

    public Task SendInvitationAsync(string toEmail, string firstName, string tempPassword, string organizationName, CancellationToken ct = default)
        => SendAsync(toEmail, "invitation",
            $"Votre accès {organizationName} sur Sentys",
            InvitationTemplate(firstName, tempPassword, organizationName), ct);

    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
        => SendAsync(toEmail, "password-reset",
            "Réinitialisation de votre mot de passe Sentys",
            PasswordResetTemplate(resetLink), ct);

    public Task SendDeadlineReminderAsync(string toEmail, string parcoursName, DateTime deadline, CancellationToken ct = default)
        => SendAsync(toEmail, "deadline-reminder",
            $"Rappel : parcours « {parcoursName} » à terminer",
            Layout($"Le parcours <strong>{parcoursName}</strong> doit être terminé avant le " +
                   $"<strong>{deadline:dd/MM/yyyy}</strong>. Connectez-vous à Sentys pour le compléter."), ct);

    public Task SendFeedbackConfirmationAsync(string toEmail, CancellationToken ct = default)
        => SendAsync(toEmail, "feedback-confirmation",
            "Nous avons bien reçu votre message",
            Layout("Merci pour votre retour. Notre équipe l'a bien reçu et reviendra vers vous si nécessaire."), ct);

    private async Task SendAsync(string toEmail, string type, string subject, string htmlBody, CancellationToken ct)
    {
        var status = "sent";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            req.Headers.TryAddWithoutValidation("api-key", _apiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                sender = new { name = _senderName, email = _senderEmail },
                to = new[] { new { email = toEmail } },
                subject,
                htmlContent = htmlBody,
            };
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
                status = $"failed:{(int)res.StatusCode}";
        }
        catch (Exception ex)
        {
            status = "failed:exception";
            _logger.LogError(ex, "Brevo send failed type={Type} to={Email}", type, toEmail);
        }

        await PersistLogAsync(toEmail, type, subject, status, ct);
    }

    private async Task PersistLogAsync(string toEmail, string type, string subject, string status, CancellationToken ct)
    {
        _logger.LogInformation("[MAIL brevo] type={Type} to={Email} status={Status}", type, toEmail, status);
        try
        {
            _db.MailLogs.Add(new MailLog
            {
                Id = Guid.NewGuid(),
                ToEmail = toEmail,
                Type = type,
                Body = subject.Length > 2000 ? subject[..2000] : subject,
                Status = status,
                SentAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist MailLog for {Email}", toEmail);
        }
    }

    // ── Templates (sobres, charte Sentys) ────────────────────────────────────
    private static string Layout(string innerHtml) =>
        $@"<div style=""font-family:Inter,Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;color:#334155"">
  <div style=""font-size:18px;font-weight:700;color:#1E293B;margin-bottom:16px"">Sentys</div>
  <div style=""font-size:14px;line-height:1.6"">{innerHtml}</div>
  <hr style=""border:none;border-top:1px solid #E2E8F0;margin:24px 0"" />
  <div style=""font-size:12px;color:#64748B"">Email automatique — merci de ne pas y répondre.</div>
</div>";

    private static string PasswordResetTemplate(string resetLink) =>
        Layout(
            "Vous avez demandé la réinitialisation de votre mot de passe.<br/><br/>" +
            $@"<a href=""{resetLink}"" style=""display:inline-block;background:#3B82F6;color:#fff;text-decoration:none;padding:12px 20px;border-radius:8px;font-weight:600"">Réinitialiser mon mot de passe</a>" +
            "<br/><br/>Ce lien expire dans 1 heure. Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.");

    private static string InvitationTemplate(string firstName, string tempPassword, string organizationName) =>
        Layout(
            $"Bonjour {firstName},<br/><br/>Un accès à la plateforme de formation cyber Sentys a été créé pour " +
            $"<strong>{organizationName}</strong>.<br/><br/>Mot de passe temporaire : " +
            $@"<code style=""background:#F1F5F9;padding:2px 6px;border-radius:4px"">{tempPassword}</code>" +
            "<br/><br/>Connectez-vous puis changez-le dès votre première connexion.");
}
