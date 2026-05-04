using CTF.Api.Contracts.RiskScore;

namespace CTF.Api.Services.RiskScoring;

/// <summary>
/// Calcul, persistance et lecture du Cyber Resilience Index (CRI) — score 0-100
/// par utilisateur représentant sa résilience cyber sur les 90 derniers jours.
/// </summary>
public interface IRiskScoringService
{
    /// <summary>
    /// Calcule le score d'un user à un instant T, sans le persister.
    /// Utilisé par le contrôleur quand aucun score n'a encore été stocké.
    /// </summary>
    Task<RiskScoreDto> ComputeScoreForUserAsync(Guid userId, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Calcule et persiste le score pour TOUS les utilisateurs actifs, tous tenants confondus.
    /// Appelé par Hangfire chaque nuit à 02h00 UTC (cf. Program.cs RecurringJob).
    /// </summary>
    Task ComputeAndStoreScoresForAllActiveUsersAsync(CancellationToken ct);

    /// <summary>
    /// Retourne le dernier score connu pour l'user. <c>null</c> si jamais calculé.
    /// </summary>
    Task<RiskScoreDto?> GetLatestScoreAsync(Guid userId, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Historique des N derniers mois (au moins 1, au plus 24).
    /// Chaque point conserve le score et son timestamp.
    /// </summary>
    Task<IReadOnlyList<RiskScoreHistoryPointDto>> GetHistoryAsync(Guid userId, Guid tenantId, int months, CancellationToken ct);
}
