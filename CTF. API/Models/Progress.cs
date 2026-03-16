namespace CTF.Api.Models;

public class Progress
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid PathId { get; set; }

    // not_started, in_progress, completed
    public string Status { get; set; } = "not_started";

    public int Percent { get; set; } = 0;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
