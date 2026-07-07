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

// ── Onglet GROUPE (analytics par équipe) ──────────────────────────────────
/// <summary>Une équipe et ses métriques agrégées (pour le classement des groupes).</summary>
public record GroupRowDto(
    string TeamId,
    string Team,
    int MemberCount,
    int Mastery,            // maîtrise combinée équipe (bas = faible)
    int AvgScore,
    int CompletionRate,
    int? AvgRisk,           // risque moyen des membres (null si aucun score)
    string RiskBand,
    int ParticipationRate
);
public record GroupsComparisonDto(List<GroupRowDto> Groups);

// ── Onglet ENTREPRISE — erreurs par COMPORTEMENT à risque (décisionnel) ───
/// <summary>Un comportement à risque (bucket de Challenge.Category) et son taux d'échec réel.</summary>
public record BehaviorRowDto(
    string Behavior,
    int ErrorRate,       // % de complétions échouées (score < 50) sur ce comportement
    int AvgScore,        // score moyen (0-100)
    int Attempts,        // nb de complétions rattachées
    int FailedAttempts
);
public record BehaviorErrorsDto(List<BehaviorRowDto> Behaviors, int TotalAttempts);

// ── Onglet RAPPORT FINANCIER (estimation de pertes potentielles ÉVITÉES) ──
/// <summary>Un point mensuel : activité réelle + couverture réelle de formation (base d'ancrage de l'estimation).</summary>
public record FinancialTrendPointDto(
    string Label,
    int Completions,             // complétions du mois (VRAIE activité)
    int CumulativeParticipation, // % de salariés ayant ≥1 complétion à fin de mois (VRAI)
    int Cri,                     // CRI moyen du mois, report du dernier connu (VRAI)
    double Coverage              // t du mois = participation/100 × CRI/100 (VRAI, 0..1)
);
/// <summary>Base RÉELLE du calcul financier. Les paramètres p/C/h/r (hypothèses) sont appliqués côté client (éditables).</summary>
public record FinancialAnalyticsDto(
    int EmployeeCount,       // N — salariés enregistrés du tenant (VRAI)
    int ParticipationRate,   // % (VRAI)
    int AvgCri,              // CRI moyen 0-100 (VRAI, 0 si aucun score)
    double Coverage,         // t final = participation/100 × CRI/100 (VRAI, 0..1)
    int TotalCompletions,
    List<FinancialTrendPointDto> Trend
);

// ── Onglet INDIVIDUEL (analytics par utilisateur) ─────────────────────────
public record AnalyticsUserDto(string UserId, string Name);
public record AnalyticsUsersDto(List<AnalyticsUserDto> Users);
public record IndividualProfileDto(
    string Name,
    int Completions,
    int AvgScore,
    int ThemesAttempted,
    string? LastActivityAt,
    string? LastLoginAt,
    string CreatedAt
);
