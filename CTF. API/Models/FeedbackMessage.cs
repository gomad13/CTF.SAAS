namespace CTF.Api.Models;

/// <summary>
/// Messages de feedback envoyés par les utilisateurs (authentifiés ou non).
/// Lus par les SuperAdmins via /api/superadmin/feedback.
/// </summary>
public class FeedbackMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Optionnels (null si feedback non authentifié)
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }

    /// <summary>
    /// Catégorie : "bug" | "amelioration" | "question" | "autre".
    /// Whitelist appliquée côté controller.
    /// </summary>
    public string Subject { get; set; } = "autre";

    public string Message { get; set; } = string.Empty;

    /// <summary>URL où l'utilisateur se trouvait quand il a soumis le feedback.</summary>
    public string? Page { get; set; }

    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>"new" | "read" | "responded" | "archived"</summary>
    public string Status { get; set; } = "new";

    /// <summary>Notes internes — non exposées à l'auteur du feedback.</summary>
    public string? AdminNotes { get; set; }
}
