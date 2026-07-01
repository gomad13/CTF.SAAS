namespace CTF.Api.Models;

/// <summary>
/// Log d'audit des actions Admin tenant sur les users (annuaire).
/// Distinct de SuperAdminAuditLog (scope tenant, pas global).
/// </summary>
public class AdminActionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ActorId { get; set; }      // Admin qui effectue l'action
    public Guid TargetUserId { get; set; } // User affecté
    public string Action { get; set; } = string.Empty;    // "role_change", "team_change", "suspend", "reactivate", "delete", "invite"
    public string? Details { get; set; }   // JSON diff / contexte
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
