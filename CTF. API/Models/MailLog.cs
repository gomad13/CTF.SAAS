namespace CTF.Api.Models;

/// <summary>
/// Trace de chaque email envoyé (ou loggé en mode dev).
/// Permet d'auditer qui a reçu quoi, débuger les bounces, etc.
/// </summary>
public class MailLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ToEmail { get; set; } = string.Empty;
    /// <summary>"invitation" | "password-reset" | "deadline-reminder" | "feedback-confirmation"</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Corps tronqué à 2000 caractères (pour audit, pas pour reproduction).</summary>
    public string? Body { get; set; }
    /// <summary>"logged-only" | "queued" | "sent" | "failed" | "bounced"</summary>
    public string Status { get; set; } = "queued";
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
