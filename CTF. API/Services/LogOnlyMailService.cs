using CTF.Api.Data;
using CTF.Api.Models;

namespace CTF.Api.Services;

/// <summary>
/// Implémentation par défaut tant que Brevo n'est pas configuré (clé API absente).
/// Persiste une trace NEUTRE dans <see cref="MailLog"/> (jamais de secret) et,
/// UNIQUEMENT en environnement Development, écrit le détail sensible (lien de reset,
/// code 2FA, mot de passe temporaire) en console pour tester en local.
/// </summary>
/// <remarks>
/// SÉCURITÉ : aucun secret d'authentification (token de reset, code 2FA, mot de passe
/// temporaire) ne doit jamais être écrit dans les logs de production ni persisté dans
/// la table MailLogs. Le body persisté est TOUJOURS neutre ; le <paramref name="devDetail"/>
/// n'apparaît qu'en console et seulement si <see cref="IHostEnvironment.IsDevelopment"/>.
/// </remarks>
public class LogOnlyMailService : IMailService
{
    private readonly ILogger<LogOnlyMailService> _logger;
    private readonly AppDbContext _db;
    private readonly IHostEnvironment _env;

    public LogOnlyMailService(ILogger<LogOnlyMailService> logger, AppDbContext db, IHostEnvironment env)
    {
        _logger = logger;
        _db = db;
        _env = env;
    }

    public Task SendInvitationAsync(string toEmail, string firstName, string tempPassword, string organizationName, CancellationToken ct = default)
        => LogAsync(toEmail, "invitation",
            safeBody: $"Invitation Sentys pour {firstName} ({organizationName})",
            devDetail: $"mot de passe temporaire : {tempPassword}", ct);

    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
        => LogAsync(toEmail, "password-reset",
            safeBody: "Réinitialisation mot de passe demandée",
            devDetail: $"lien : {resetLink}", ct);

    public Task SendDeadlineReminderAsync(string toEmail, string parcoursName, DateTime deadline, CancellationToken ct = default)
        => LogAsync(toEmail, "deadline-reminder",
            safeBody: $"Rappel deadline parcours '{parcoursName}' — échéance {deadline:yyyy-MM-dd}",
            devDetail: null, ct);

    public Task SendFeedbackConfirmationAsync(string toEmail, CancellationToken ct = default)
        => LogAsync(toEmail, "feedback-confirmation",
            safeBody: "Confirmation réception feedback Sentys",
            devDetail: null, ct);

    public Task SendTwoFactorCodeAsync(string toEmail, string code, CancellationToken ct = default)
        => LogAsync(toEmail, "two-factor-code",
            safeBody: "Code de vérification 2FA envoyé (expire dans 10 minutes)",
            devDetail: $"code : {code}", ct);

    public Task SendSupportMessageAsync(string fromEmail, string subject, string message, CancellationToken ct = default)
        => LogAsync("support", "support",
            safeBody: $"[Support] de {fromEmail} — sujet: {subject}",
            devDetail: null, ct);

    // safeBody : jamais de secret → console (prod incluse) + persistance MailLogs.
    // devDetail : secret éventuel → console UNIQUEMENT en Development, jamais persisté.
    private async Task LogAsync(string toEmail, string type, string safeBody, string? devDetail, CancellationToken ct)
    {
        if (_env.IsDevelopment() && !string.IsNullOrEmpty(devDetail))
            _logger.LogInformation("[MAIL log-only] type={Type} to={Email} body={Body} ({Detail})", type, toEmail, safeBody, devDetail);
        else
            _logger.LogInformation("[MAIL log-only] type={Type} to={Email} body={Body}", type, toEmail, safeBody);

        try
        {
            _db.MailLogs.Add(new MailLog
            {
                Id = Guid.NewGuid(),
                ToEmail = toEmail,
                Type = type,
                Body = safeBody.Length > 2000 ? safeBody[..2000] : safeBody, // JAMAIS devDetail
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
