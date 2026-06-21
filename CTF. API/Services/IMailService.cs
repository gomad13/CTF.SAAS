namespace CTF.Api.Services;

/// <summary>
/// Service mail abstrait. Implémentations :
/// - <see cref="LogOnlyMailService"/> : log console seulement (mode bêta sans clé Brevo)
/// - <see cref="BrevoMailService"/> : appel API Brevo (à implémenter en Phase 3)
/// </summary>
public interface IMailService
{
    Task SendInvitationAsync(string toEmail, string firstName, string tempPassword, string organizationName, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendDeadlineReminderAsync(string toEmail, string parcoursName, DateTime deadline, CancellationToken ct = default);
    Task SendFeedbackConfirmationAsync(string toEmail, CancellationToken ct = default);

    /// <summary>M3 — Envoie un code de double authentification (6 chiffres) par email.</summary>
    Task SendTwoFactorCodeAsync(string toEmail, string code, CancellationToken ct = default);
}
