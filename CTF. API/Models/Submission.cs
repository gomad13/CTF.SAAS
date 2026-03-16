namespace CTF.Api.Models;

public class Submission
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid ChallengeId { get; set; }

    public int AttemptNo { get; set; } = 1;
    public bool IsCorrect { get; set; } = false;
    public int ScoreAwarded { get; set; } = 0;

    public DateTime SubmittedAt { get; set; }
}
