namespace CTF.Api.Contracts;

public record SubmissionDto(Guid Id, Guid UserId, int AttemptNo, bool IsCorrect, int ScoreAwarded, DateTime SubmittedAt);
