namespace CTF.Api.Contracts;

public record ChallengeDto(
    Guid Id,
    Guid ModuleId,
    string Type,
    string Title,
    int? Difficulty,
    int Points,
    string Status,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    string? InstructionTitle,
    string? InstructionBody,
    string? InstructionShortReminder);

/// <summary>
/// Payload d'édition des consignes pédagogiques (endpoint admin).
/// Validation des longueurs faite côté controller (Title ≤ 200, Reminder ≤ 300).
/// </summary>
public record UpdateInstructionsRequest(
    string? InstructionTitle,
    string? InstructionBody,
    string? InstructionShortReminder);
