namespace CTF.Api.Contracts;

// ================================
// Import CSV result
// ================================
public record ImportUsersResult(
    int Created,
    int Updated,
    int Skipped,
    List<string> Errors
);

// ================================
// Dashboard overview stats
// ================================
public record StatsOverviewDto(
    int Total,
    int Grey,
    int Yellow,
    int Green,
    int Red,
    double AvgProgress,
    double AvgScore
);

// ================================
// Tracking row (table employees)
// ================================
public record TrackingUserRowDto(
    Guid UserId,
    string DisplayName,
    string Email,
    int ProgressPercent,
    int Score,
    string StatusColor,
    DateTime? LastActivityAt
);

// ================================
// Generic paged result
// ================================
public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int Total
);

// ================================
// Admin paths list (dropdown)
// ================================
public record AdminPathListItemDto(
    Guid Id,
    string Title,
    string Status,
    DateTime? PublishedAt
);
