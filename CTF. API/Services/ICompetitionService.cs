using CTF.Api.Contracts;

namespace CTF.Api.Services;

public interface ICompetitionService
{
    Task<bool> GetStatusAsync(Guid tenantId, CancellationToken ct = default);

    Task<PagedResult<ScoreboardEntryDto>> GetScoreboardAsync(
        Guid tenantId,
        Guid currentUserId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PodiumDto> GetPodiumAsync(Guid tenantId, Guid currentUserId, CancellationToken ct = default);

    Task<List<TeamLeaderboardEntryDto>> GetTeamLeaderboardAsync(Guid tenantId, Guid currentUserId, CancellationToken ct = default);

    Task<TeamPodiumDto> GetTeamPodiumAsync(Guid tenantId, Guid currentUserId, CancellationToken ct = default);

    Task<MyRankDto> GetMyRankAsync(Guid tenantId, Guid currentUserId, CancellationToken ct = default);

    Task<bool> RecordDurationAsync(Guid tenantId, Guid userId, Guid challengeId, int durationSeconds, CancellationToken ct = default);

    Task<ToggleCompetitionResponseDto> ToggleAsync(
        Guid tenantId,
        Guid adminUserId,
        bool enabled,
        CancellationToken ct = default);
}
