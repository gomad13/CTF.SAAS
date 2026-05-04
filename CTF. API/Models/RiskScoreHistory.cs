namespace CTF.Api.Models;

/// <summary>
/// Historique des Cyber Resilience Index (CRI) calculés pour chaque utilisateur.
/// Une ligne = un calcul à un instant donné. Le score est nullable : <c>null</c>
/// signifie « données insuffisantes » (moins de 3 challenges tentés à l'instant
/// du calcul). On garde la composition détaillée du score en JSONB pour pouvoir
/// expliquer chaque variation a posteriori sans devoir recalculer.
/// </summary>
public class RiskScoreHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>Score 0-100 ou null si données insuffisantes.</summary>
    public int? Score { get; set; }

    /// <summary>Détail des 4 composantes (taux de réussite, vitesse, diversité, régression) en JSONB.</summary>
    public string Components { get; set; } = "{}";

    /// <summary>Timestamp UTC du calcul.</summary>
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
