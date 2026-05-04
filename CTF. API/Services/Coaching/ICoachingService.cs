using CTF.Api.Contracts.Coaching;

namespace CTF.Api.Services.Coaching;

public interface ICoachingService
{
    /// <summary>
    /// Génère le coaching pour un challenge attempt donné. Si le coaching
    /// existe déjà pour cet <c>attemptId</c>, on le renvoie tel quel
    /// (idempotence + protection rate-limit).
    /// </summary>
    Task<CoachingFeedbackDto> GenerateForAttemptAsync(
        Guid attemptId, Guid userId, Guid tenantId, CancellationToken ct);

    Task<CoachingFeedbackDto?> GetByIdAsync(
        Guid id, Guid userId, Guid tenantId, CancellationToken ct);

    Task<PagedResult<CoachingFeedbackDto>> GetHistoryAsync(
        Guid userId, Guid tenantId, int page, int pageSize, CancellationToken ct);
}
