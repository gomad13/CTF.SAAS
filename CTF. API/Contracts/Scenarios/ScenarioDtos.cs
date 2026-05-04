namespace CTF.Api.Contracts.Scenarios;

// ── Catalogue (admin) ────────────────────────────────────────────────────────

public sealed record ScenarioCatalogItemDto(
    Guid Id,
    string ExternalId,
    string Version,
    string Name,
    string Description,
    string Category,
    string Difficulty,
    int DurationDays,
    int EmailCount,
    int AttackStepCount);

// ── Lancement ────────────────────────────────────────────────────────────────

public sealed record LaunchScenarioRequest(
    Guid TemplateId,
    Guid TargetUserId,
    Guid SenderUserId,
    string Mode,                    // "normal" | "demo"
    DateTime? ScheduledStartAt,     // null = now
    List<StepCustomizationDto>? StepOverrides);

public sealed record StepCustomizationDto(
    string StepId,
    string? Subject,
    string? BodyTemplate);

public sealed record EligibleSenderDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email);

// ── Instances (admin) ────────────────────────────────────────────────────────

public sealed record ScenarioInstanceListItemDto(
    Guid Id,
    string TemplateName,
    string TemplateExternalId,
    string Category,
    string TargetEmail,
    string TargetFullName,
    string SenderEmail,
    string SenderFullName,
    string Mode,
    string Status,
    string? CurrentStepId,
    DateTime ScheduledStartAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? StopReason,
    int EmailsSent,
    int OpenedCount,
    int ClickedCount,
    int ReportedCount);

public sealed record ScenarioInstanceDetailDto(
    Guid Id,
    Guid TenantId,
    Guid TemplateId,
    string TemplateName,
    string TemplateExternalId,
    string Category,
    Guid TargetUserId,
    string TargetEmail,
    string TargetFullName,
    Guid SenderUserId,
    string SenderEmail,
    string SenderFullName,
    Guid LaunchedByUserId,
    string Mode,
    string Status,
    string? CurrentStepId,
    DateTime ScheduledStartAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? StopReason,
    List<ScenarioInstanceStepDto> Steps,
    List<ScenarioInstanceEmailDto> Emails);

public sealed record ScenarioInstanceStepDto(
    Guid Id,
    string StepId,
    int StepOrder,
    string Status,
    DateTime ScheduledAt,
    DateTime? SentAt);

public sealed record ScenarioInstanceEmailDto(
    Guid Id,
    string Subject,
    string FromName,
    string FromEmail,
    bool IsAttackStep,
    DateTime SentAt,
    DateTime? FirstReadAt,
    DateTime? FirstClickAt,
    DateTime? ReportedAt);

public sealed record StopScenarioInstanceRequest(string Reason);

// ── Inbox (employé) ──────────────────────────────────────────────────────────

public sealed record InboxEmailListItemDto(
    Guid Id,
    string Subject,
    string FromName,
    string FromEmail,
    DateTime SentAt,
    bool IsRead,
    bool IsReported,
    bool IsSystemNotification);

public sealed record InboxEmailDetailDto(
    Guid Id,
    string Subject,
    string FromName,
    string FromEmail,
    string BodyHtml,
    DateTime SentAt,
    bool IsRead,
    bool IsReported,
    bool IsSystemNotification);

public sealed record ReportPhishingResponse(
    bool Success,
    bool TriggeredOutcome,
    string OutcomeKey,
    string Message);

// ── Consentement expéditeur fictif ───────────────────────────────────────────

public sealed record SenderConsentDto(bool ConsentsToBeFictionalSender);
