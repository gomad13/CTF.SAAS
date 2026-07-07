using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Contracts;

// ── Contenus inclus dans une campagne ─────────────────────────────────────
// ContentType : "Parcours" (→ LearningPath.Id) | "Scenario" (→ ScenarioTemplate.Id)
public record CampaignContentItem(
    [Required] [RegularExpression("^(Parcours|Scenario)$")] string ContentType,
    [Required] Guid ContentId,
    int DisplayOrder
);

// ── Création / mise à jour ────────────────────────────────────────────────
public record CreateCampaignRequest(
    [Required] [StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    [Required] [MinLength(1)] List<CampaignContentItem> Contents,
    bool AssignToWholeTenant,
    List<Guid>? AssignedUserIds
);

public record UpdateCampaignRequest(
    [Required] [StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    [Required] [MinLength(1)] List<CampaignContentItem> Contents
);

public record AssignEmployeesRequest(
    bool AssignToWholeTenant,
    List<Guid>? UserIds
);

// ── Lecture ────────────────────────────────────────────────────────────────
public record CampaignSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int AssignedCount,
    int ContentCount,
    double GlobalCompletion,
    bool IsArchived,
    DateTime CreatedAt
);

public record CampaignContentDto(
    Guid Id,
    string ContentType,
    Guid ContentId,
    string Title,
    string? Category,
    int DisplayOrder
);

public record CampaignAssignmentDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    DateTime AssignedAt
);

public record CampaignDetailDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    bool AssignedToWholeTenant,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<CampaignContentDto> Contents,
    List<CampaignAssignmentDto> Assignments
);

// ── Dashboard ──────────────────────────────────────────────────────────────
public record EmployeeProgressDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    double CompletionPercentage,
    string Status,        // "NotStarted" | "InProgress" | "Completed"
    bool IsLate
);

public record CampaignDashboardDto(
    Guid CampaignId,
    string Name,
    string Status,
    int TotalAssigned,
    int NotStarted,
    int InProgress,
    int Completed,
    double GlobalCompletionPercentage,
    double AverageSuccessRate,
    int LateEmployeesCount,
    List<EmployeeProgressDto> EmployeeProgress
);

// ── Catalogue disponible ──────────────────────────────────────────────────
public record AvailableContentDto(
    string ContentType,   // "Parcours" | "Scenario"
    Guid ContentId,
    string Title,
    string? Category
);

// ── Vue employé ───────────────────────────────────────────────────────────
public record EmployeeCampaignContentDto(
    Guid CampaignContentId,
    string ContentType,
    Guid ContentId,
    string Title,
    string Status,
    double? CompletionPercentage,
    bool? IsSuccess
);

public record EmployeeCampaignDto(
    Guid CampaignId,
    string Name,
    string? Description,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    double MyCompletionPercentage,
    List<EmployeeCampaignContentDto> Contents
);

// ── Efficacité des campagnes (lecture seule, agrégation ; VRAIES données tenant) ──
public record CampaignEfficacyPointDto(string Label, int Value);
/// <summary>Résultats scénarios (phishing) d'une campagne : e-mails d'attaque, clics, signalements.</summary>
public record CampaignScenarioResultDto(int ScenariosEvaluated, int AttackEmails, int Clicked, int Reported, int ClickRate, int ReportRate);
/// <summary>Efficacité détaillée d'une campagne.</summary>
public record CampaignEfficacyDto(
    Guid CampaignId,
    string Name,
    string Status,
    int TotalAssigned,
    int Started,
    int CompletedUsers,
    int ParticipationRate,   // % d'assignés ayant démarré
    int CompletionRate,      // % d'assignés ayant tout terminé
    int AverageSuccessRate,  // % de contenus réussis (IsSuccess)
    List<CampaignEfficacyPointDto> CompletionTrend,
    CampaignScenarioResultDto? Scenario
);
/// <summary>Ligne de synthèse pour comparer les campagnes.</summary>
public record CampaignEfficacyRowDto(Guid CampaignId, string Name, string Status, int TotalAssigned, int ParticipationRate, int CompletionRate);
public record CampaignsEfficacyDto(List<CampaignEfficacyRowDto> Campaigns);
