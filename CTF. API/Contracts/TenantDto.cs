namespace CTF.Api.Contracts;

public record TenantDto(
    Guid Id,
    string Name,
    string? SsoProvider,
    DateTime CreatedAt,
    bool IsActive,
    bool IsCompetitionModeEnabled
);
