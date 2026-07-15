using CTF.Api.Data;
using CTF.Api.Models;

namespace CTF.Api.Services;

/// <summary>
/// Implémentation par défaut tant que Brevo n'est pas configuré (clé API absente).
/// Logge le mail dans la console + persiste dans <see cref="MailLog"/> avec status = "logged-only".
/// Permet de tester en dev sans envoyer d'email réel.
/// </summary>
public class LogOnlyMailService : IMailService
{
    private readonly ILogger<LogOnlyMailService> _logger;
    private readonly AppDbContext _db;

    public LogOnlyMailService(ILogger<LogOnlyMailService> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public Task SendInvitationAsync(string toEmail, string firstName, string tempPassword, string organizationName, CancellationToken ct = default)
        => LogAsync(toEmail, "invitation",
            $"Invitation Sentys pour {firstName} ({organizationName}) — mot de passe temporaire {tempPassword}", ct);

    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
        => LogAsync(toEmail, "password-reset",
            $"Réinitialisation mot de passe — lien : {resetLink}", ct);

    public Task SendDeadlineReminderAsync(string toEmail, string parcoursName, DateTime deadline, CancellationToken ct = default)
        => LogAsync(toEmail, "deadline-reminder",
            $"Rappel deadline parcours '{parcoursName}' — échéance {deadline:yyyy-MM-dd}", ct);

    public Task SendFeedbackConfirmationAsync(string toEmail, CancellationToken ct = default)
        => LogAsync(toEmail, "feedback-confirmation",
            "Confirmation réception feedback Sentys", ct);

    public Task SendTwoFactorCodeAsync(string toEmail, string code, CancellationToken ct = default)
        => LogAsync(toEmail, "two-factor-code",
            $"Code de vérification 2FA : {code} (expire dans 10 minutes)", ct);

    public Task SendSupportMessageAsync(string fromEmail, string subject, string message, CancellationToken ct = default)
        => LogAsync("support", "support",
            $"[Support] de {fromEmail} — sujet: {subject}", ct);

    private async Task LogAsync(string toEmail, string type, string body, CancellationToken ct)
    {
        _logger.LogInformation("[MAIL log-only] type={Type} to={Email} body={Body}", type, toEmail, body);

        try
        {
            _db.MailLogs.Add(new MailLog
            {
                Id = Guid.NewGuid(),
                ToEmail = toEmail,
                Type = type,
                Body = body.Length > 2000 ? body[..2000] : body,
                Status = "logged-only",
                SentAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Ne jamais bloquer le flow métier sur un échec de log mail.
            _logger.LogWarning(ex, "Failed to persist MailLog for {Email}", toEmail);
        }
    }
}
