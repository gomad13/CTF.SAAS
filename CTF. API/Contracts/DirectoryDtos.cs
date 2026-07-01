namespace CTF.Api.Contracts;

public record DirectoryUserRowDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive,
    Guid? TeamId,
    string? TeamName,
    string? TeamColor,
    DateTime CreatedAt,
    DateTime? LastActivityAt,
    DateTime? LastLoginAt,
    int AssignedPathsCount,
    int CompletedPathsCount
);

public record DirectoryAggregationsDto(
    int Total,
    int ActiveCount,
    int SuspendedCount,
    int AdminCount,
    Dictionary<string, int> ByTeam // teamName → count
);

public record DirectoryListResponseDto(
    List<DirectoryUserRowDto> Items,
    int Page,
    int PageSize,
    int Total,
    DirectoryAggregationsDto Aggregations
);

public record DirectoryUserDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive,
    Guid? TeamId,
    string? TeamName,
    string? TeamColor,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? LastActivityAt,
    DateTime? LastLoginAt,
    List<DirectoryParcoursDto> Parcours,
    List<DirectoryAuditDto> AuditLog
);

public record DirectoryParcoursDto(
    Guid PathId,
    string Title,
    string? Sector,
    string? Level,
    string Status,          // not_started | in_progress | completed
    int Percent,
    DateTime? DueAt,
    bool IsMandatory,
    string Source           // global | team | compliance | individual
);

public record DirectoryAuditDto(
    Guid Id,
    string Action,
    string? Details,
    Guid ActorId,
    string? ActorEmail,
    DateTime CreatedAt
);

public record DirectoryPatchDto(
    Guid? TeamId,
    string? Role,
    bool? IsActive,
    string? FirstName,
    string? LastName
);

public record DirectoryBulkActionDto(
    List<Guid> UserIds,
    string Action,          // assign_team | suspend | reactivate | delete
    Dictionary<string, object>? Params
);

public record DirectoryInviteDto(
    string Email,
    string FirstName,
    string LastName,
    string Role,           // "user" | "admin"
    Guid? TeamId
);
