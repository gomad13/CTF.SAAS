namespace CTF.Api.Contracts.Coaching;

/// <summary>
/// Coaching post-incident sérialisé pour le front. Aucune donnée interne LLM
/// (tokens, durée, modèle) n'est exposée pour ne pas révéler l'infra.
/// </summary>
public record CoachingFeedbackDto(
    Guid Id,
    Guid ChallengeAttemptId,
    string ChallengeType,
    string Content,
    string Status,
    DateTime CreatedAt);

public record CoachingGenerateRequest(Guid AttemptId);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
