namespace CTF.Api.Models.Scenarios;

/// <summary>
/// Email Inbox interne livré dans la boîte de l'employé cible (jamais
/// envoyé via SMTP en V1). Sert aussi pour les notifications systèmes
/// (post-coaching, rapport admin).
/// </summary>
public class ScenarioEmail
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Étape source (null pour notifications système hors scénario).</summary>
    public Guid? InstanceStepId { get; set; }

    public Guid TenantId { get; set; }
    public Guid RecipientUserId { get; set; }

    /// <summary>Nom affiché dans le From (ex. "Marie Dupont" ou "IT Support").</summary>
    public string FromName { get; set; } = "";

    /// <summary>Email From rendu (ex. "marie.dupont@acme.com").</summary>
    public string FromEmail { get; set; } = "";

    public string Subject { get; set; } = "";

    /// <summary>HTML rendu prêt à afficher (liens réécrits + pixel injecté).</summary>
    public string BodyHtml { get; set; } = "";

    /// <summary>GUID v4 utilisé dans les URLs de tracking (open + click).</summary>
    public string TrackingToken { get; set; } = "";

    /// <summary>True si l'étape source a is_attack_step = true.</summary>
    public bool IsAttackStep { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? FirstReadAt { get; set; }
    public DateTime? FirstClickAt { get; set; }
    public DateTime? ReportedAt { get; set; }

    /// <summary>True si l'email est une notification système (ne déclenche pas la state-machine).</summary>
    public bool IsSystemNotification { get; set; }
}
