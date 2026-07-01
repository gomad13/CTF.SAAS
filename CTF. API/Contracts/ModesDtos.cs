using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Contracts;

public record ModeStatusDto(bool IsEnabled);

public record ToggleModeRequestDto([Required] bool IsEnabled);

public record ToggleModeResponseDto(bool IsEnabled, DateTime UpdatedAt, Guid UpdatedBy);

public record AllModesStatusDto(
    bool Competition,
    bool Analytics,
    bool Compliance,
    bool Teams,
    bool Campaigns
);

// ── Analytics ───────────────────────────────────────────────────────────
public record AnalyticsKpisDto(
    int ActiveUsers7d,
    int ActiveUsers30d,
    int TotalCompletions,
    int AverageScore,
    int AverageCompletionPercent
);

public record ActivityPointDto(DateTime Date, int Completions);

public record CompletionByPathDto(Guid PathId, string Title, int Completions, int AverageScore);

public record ChallengeTypeStatDto(string Type, int Completions);

public record AnalyticsOverviewDto(
    AnalyticsKpisDto Kpis,
    List<ActivityPointDto> Activity,
    List<CompletionByPathDto> ByPath,
    List<ChallengeTypeStatDto> ByType
);

// ── Compliance ──────────────────────────────────────────────────────────
public record CreateMandatoryAssignmentDto(
    [Required] Guid PathId,
    [Required] [RegularExpression("^(all_users|team|user)$")] string AssignedToType,
    Guid? AssignedToId,
    [Required] DateTime Deadline
);

public record MandatoryAssignmentDto(
    Guid Id,
    Guid PathId,
    string PathTitle,
    string AssignedToType,
    Guid? AssignedToId,
    DateTime Deadline,
    int CompletionRatePercent,
    int UsersTargeted,
    int UsersCompleted,
    DateTime CreatedAt
);

public record ComplianceOverviewDto(
    int TotalAssignments,
    int OverallCompliancePercent,
    List<MandatoryAssignmentDto> Assignments
);

// ── Teams ──────────────────────────────────────────────────────────────
public record CreateTeamDto(
    [Required] [StringLength(120, MinimumLength = 1)] string Name,
    [StringLength(500)] string? Description,
    [RegularExpression("^#[0-9a-fA-F]{6}$")] string? Color,
    [StringLength(60)] string? Icon,
    Guid? ManagerId,
    [Range(1, 10000)] int? MaxMembers = null,
    bool IsOpen = false
);

public record UpdateTeamDto(
    [StringLength(120, MinimumLength = 1)] string? Name,
    [StringLength(500)] string? Description,
    [RegularExpression("^#[0-9a-fA-F]{6}$")] string? Color,
    [StringLength(60)] string? Icon,
    Guid? ManagerId,
    [Range(1, 10000)] int? MaxMembers = null,
    bool? IsOpen = null
);

public record TeamDto(
    Guid Id, string Name, string? Description, string? Color, string? Icon, Guid? ManagerId,
    int MemberCount, int ParcoursCount, int CompliancePercent, DateTime CreatedAt,
    int? MaxMembers = null, bool IsOpen = false);

// ── Équipes côté utilisateur (B4/B5) ─────────────────────────────────────
public record UserTeamDto(
    Guid TeamId, string Name, string? Description, string? Color, string? Icon,
    int MemberCount, int? MaxMembers, bool IsOpen, bool IsMember, bool IsFull);

public record UserTeamMemberDto(Guid UserId, string FirstName, string LastName, string? Role);

public record MyTeamDto(
    Guid TeamId, string Name, string? Description, string? Color, string? Icon,
    bool IsOpen, int MemberCount, int? MaxMembers, List<UserTeamMemberDto> Members);

public record JoinLeaveResultDto(bool Success, string? Error);

public record AssignUserToTeamDto([Required] Guid UserId, Guid? TeamId);

public record AddTeamMembersDto([Required] List<Guid> UserIds);

/// <summary>Résultat d'une affectation de membres (M4) : combien ajoutés, combien refusés faute de place, capacité.</summary>
public record AddTeamMembersResultDto(int Added, int Rejected, int CurrentCount, int? MaxMembers, string? Error);

public record TeamMemberDto(Guid UserId, string Email, string FirstName, string LastName, string? Role, bool IsActive, DateTime? JoinedAt);

/// <summary>Utilisateur du tenant sans équipe — à affecter par l'admin (M4 : affectation à l'arrivée).</summary>
public record UnassignedUserDto(Guid UserId, string Email, string FirstName, string LastName, bool IsActive, DateTime CreatedAt);

public record AssignParcoursToTeamDto(
    [Required] Guid PathId,
    DateTime? Deadline,
    bool IsMandatory
);

public record UpdateTeamParcoursDto(DateTime? Deadline, bool? IsMandatory);

public record TeamParcoursDto(
    Guid PathId, string Title, string? Level, int ModuleCount, int ChallengeCount,
    DateTime? Deadline, bool IsMandatory, int AvgCompletionPercent, DateTime AssignedAt);

public record TeamStatsDto(
    Guid TeamId, string TeamName,
    int MemberCount, int ParcoursCount,
    int OverallCompletionPercent,
    List<TeamParcoursDto> ParcoursProgress,
    List<TeamTopMemberDto> TopMembers);

public record TeamTopMemberDto(Guid UserId, string Email, string FirstName, string LastName, int ChallengesCompleted, int AvgScore);

// ── Campaigns ──────────────────────────────────────────────────────────
public record CreateCampaignDto(
    [Required] [StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    [Required] List<Guid> PathIds,
    [Required] [RegularExpression("^(all|team|user)$")] string TargetType,
    List<Guid>? TargetIds
);

public record CampaignDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int PathCount,
    int ParticipantCount,
    int AverageCompletionPercent,
    DateTime CreatedAt
);
