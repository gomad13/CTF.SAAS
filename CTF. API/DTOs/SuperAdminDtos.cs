using System.ComponentModel.DataAnnotations;

namespace CTF.Api.DTOs;

public record CreateTenantDto(
    [Required] [StringLength(120, MinimumLength = 2)] string Name
);

public record UpdateTenantDto(
    [StringLength(120, MinimumLength = 2)] string? Name
);

public record ChangeRoleDto(
    [Required] [RegularExpression("^(user|admin|SuperAdmin)$", ErrorMessage = "Role must be user, admin or SuperAdmin.")]
    string Role
);

public record MoveTenantDto([Required] Guid TenantId);

public record CreateLicenseDto(
    [Required] Guid TenantId,
    [Required] [StringLength(50, MinimumLength = 2)] string Plan,
    [Range(1, 100000)] int MaxUsers,
    [Required] DateTime ExpiresAt,
    [StringLength(500)] string? Notes
);

public record UpdateLicenseDto(
    [StringLength(50, MinimumLength = 2)] string? Plan,
    [Range(1, 100000)] int? MaxUsers,
    DateTime? ExpiresAt,
    bool? IsActive,
    [StringLength(500)] string? Notes
);

public record CreateAnnouncementDto(
    [Required] [StringLength(200, MinimumLength = 2)] string Title,
    [Required] [StringLength(2000, MinimumLength = 1)] string Message,
    [Required] [RegularExpression("^(info|warning|success|danger|error|maintenance|update)$")] string Type,
    Guid? TenantId,
    DateTime? ExpiresAt
);

public record UpdateAnnouncementDto(
    [StringLength(200, MinimumLength = 2)] string? Title,
    [StringLength(2000, MinimumLength = 1)] string? Message,
    [RegularExpression("^(info|warning|success|danger|error|maintenance|update)$")] string? Type,
    DateTime? ExpiresAt,
    bool? IsActive
);

public record CreateSuperUserDto(
    [Required] [EmailAddress] [StringLength(255)] string Email,
    [Required] [StringLength(80, MinimumLength = 1)] string FirstName,
    [Required] [StringLength(80, MinimumLength = 1)] string LastName,
    [Required] Guid TenantId,
    [Required] [RegularExpression("^(user|admin)$", ErrorMessage = "Role must be user or admin.")]
    string Role
);

// ── [MULTI-SOCIETES] Gestion des appartenances user ↔ société (SuperAdmin) ──

/// <summary>Ajoute (ou met à jour) l appartenance d un user à une société avec un rôle.</summary>
public record AddUserTenantDto(
    [Required] Guid TenantId,
    [Required] [RegularExpression("^(user|admin|owner)$", ErrorMessage = "Role must be user, admin or owner.")]
    string Role,
    bool MakeDefault = false
);

/// <summary>Change le rôle d un user dans une société déjà rejointe.</summary>
public record UpdateUserTenantRoleDto(
    [Required] [RegularExpression("^(user|admin|owner)$", ErrorMessage = "Role must be user, admin or owner.")]
    string Role
);
