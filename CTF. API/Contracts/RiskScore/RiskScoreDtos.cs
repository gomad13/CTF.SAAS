namespace CTF.Api.Contracts.RiskScore;

/// <summary>
/// Score CRI courant + composantes détaillées + date du calcul.
/// <see cref="Score"/> est nullable : null = données insuffisantes (cf. service).
/// </summary>
public record RiskScoreDto(
    int? Score,
    RiskScoreComponentsDto Components,
    DateTime ComputedAt);

/// <summary>
/// Détail des 4 composantes pondérées qui forment le score.
/// Chaque valeur est sur 100, indépendamment du poids dans la somme finale.
/// </summary>
public record RiskScoreComponentsDto(
    double SuccessRate,
    double SpeedScore,
    double DiversityScore,
    double RegressionScore);

/// <summary>
/// Point unitaire dans l'historique (date + score).
/// Score nullable pour conserver la traçabilité des moments où l'user n'avait
/// pas assez de données (ex : 1ers jours après onboarding).
/// </summary>
public record RiskScoreHistoryPointDto(DateTime Date, int? Score);
