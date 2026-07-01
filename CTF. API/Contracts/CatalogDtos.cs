namespace CTF.Api.Contracts;

// ── Liste paginée parcours catalogue ─────────────────────────────────────────
public record CatalogPathListItemDto(
    Guid Id,
    string Title,
    string? Description,
    string? Level,
    string? Sector,
    string Status,
    int? EstimatedMinutes,
    string? Tags,
    int ChallengesCount,
    int TenantsWithAccessCount,
    DateTime CreatedAt,
    DateTime? PublishedAt
);

// ── Détail parcours catalogue ─────────────────────────────────────────────────
public record CatalogPathDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string? Level,
    string? Sector,
    string Status,
    int? EstimatedMinutes,
    string? Tags,
    bool IsCatalog,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    List<CatalogModuleDto> Modules
);

public record CatalogModuleDto(
    Guid Id,
    string Title,
    int SortOrder,
    List<CatalogChallengeDto> Challenges
);

public record CatalogChallengeDto(
    Guid Id,
    string Title,
    string Type,
    string? ContentType,
    string? Category,
    int? Difficulty,
    int Points,
    int SortOrder
);

// ── Création / modification basique d'un parcours catalogue ──────────────────
public record CreateCatalogPathDto(
    string Title,
    string? Description,
    string Level,
    string Sector,
    int? EstimatedMinutes,
    string? Tags
);

public record UpdateCatalogPathDto(
    string Title,
    string? Description,
    string? Level,
    string? Sector,
    int? EstimatedMinutes,
    string? Tags,
    string? Status
);

// ── Gestion d'accès (par parcours → vue tenants) ─────────────────────────────
public record CatalogAccessTenantDto(
    Guid TenantId,
    string TenantName,
    bool HasAccess,
    DateTime? GrantedAt,
    string? GrantedByEmail,
    DateTime? RevokedAt,
    string? RevokedByEmail
);

public record GrantAccessRequestDto(List<Guid> TenantIds);

// ── Vue par tenant (→ liste parcours catalogue avec statut) ──────────────────
public record TenantCatalogAccessDto(
    Guid PathId,
    string Title,
    string? Sector,
    string? Level,
    int? EstimatedMinutes,
    bool HasAccess,
    DateTime? GrantedAt
);

public record GrantPathsToTenantDto(List<Guid> PathIds);

// ── Stats parcours catalogue ─────────────────────────────────────────────────
public record CatalogPathStatsDto(
    Guid PathId,
    int TenantsWithAccess,
    int TotalUsersWithAccess,
    int UsersStarted,
    int UsersCompleted,
    double AverageCompletionPercent
);
