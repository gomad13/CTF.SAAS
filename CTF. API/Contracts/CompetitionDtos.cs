using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Contracts;

public record CompetitionStatusDto(bool IsEnabled);

public record ScoreboardEntryDto(
    int Rank,
    Guid UserId,
    string DisplayName,
    string Initials,
    int Score,
    int ChallengesCompleted,
    bool IsCurrentUser,
    bool IsTopThree,
    int BasePoints = 0,
    int SpeedBonus = 0,
    string? TeamName = null,
    string? TeamColor = null
);

public record PodiumDto(
    ScoreboardEntryDto? First,
    ScoreboardEntryDto? Second,
    ScoreboardEntryDto? Third
);

// ── Classement par equipe ────────────────────────────────────────────────────
public record TeamLeaderboardEntryDto(
    int Rank,
    Guid TeamId,
    string Name,
    string? Color,
    string? Icon,
    int Score,
    int MemberCount,
    bool IsCurrentUserTeam,
    bool IsTopThree
);

public record TeamPodiumDto(
    TeamLeaderboardEntryDto? First,
    TeamLeaderboardEntryDto? Second,
    TeamLeaderboardEntryDto? Third
);

// ── Rang de l'utilisateur connecte (individuel + son equipe) ─────────────────
public record MyRankDto(
    int? IndividualRank,
    int IndividualScore,
    int IndividualBasePoints,
    int IndividualSpeedBonus,
    int TotalParticipants,
    Guid? TeamId,
    string? TeamName,
    string? TeamColor,
    int? TeamRank,
    int TeamScore,
    int TotalTeams,
    string? TeamIcon = null
);

// ── Enregistrement du temps de resolution (bonus rapidite) ───────────────────
public record RecordDurationRequestDto(
    [Required] Guid ChallengeId,
    [Range(1, 7200)] int DurationSeconds
);

public record ToggleCompetitionRequestDto(
    [Required] bool IsEnabled
);

public record ToggleCompetitionResponseDto(
    bool IsEnabled,
    DateTime UpdatedAt,
    Guid UpdatedBy
);
