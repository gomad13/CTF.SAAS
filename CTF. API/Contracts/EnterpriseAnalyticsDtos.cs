namespace CTF.Api.Contracts;

// Analytics — onglet ENTREPRISE (vraies données du tenant, agrégations SQL)

/// <summary>Un thème (Challenge.Category normalisée) et sa faiblesse combinée score × complétion.</summary>
public record WeakTopicDto(
    string Theme,
    int AvgScore,        // score moyen (0-100) des complétions du tenant sur ce thème
    int CompletionRate,  // couverture org (0-100) = complétions / (users × challenges du thème)
    int Mastery,         // maîtrise combinée = AvgScore × CompletionRate / 100 (bas = faible)
    int Completions
);

public record EnterpriseWeakTopicsDto(List<WeakTopicDto> Topics, int ThemesEvaluated);

public record RiskPointDto(string Label, int Value);

public record EnterpriseRiskDto(
    int? GlobalScore,          // moyenne du DERNIER score par user (null si aucun score)
    string Band,               // Excellent/Bon/Moyen/À renforcer (seuils 80/60/40)
    List<RiskPointDto> Trend,  // courbe mensuelle (moyenne du mois, report du dernier connu si mois vide)
    int UsersScored
);

public record EnterpriseEngagementDto(
    int TotalUsers,
    int Active7d,
    int Active30d,
    int NeverConnected,
    int ParticipationRate,          // % d'users ayant complété ≥1 challenge
    int TotalCompletions,
    int AvgCompletionsPerActiveUser
);
