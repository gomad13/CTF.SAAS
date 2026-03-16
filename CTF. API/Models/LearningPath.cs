namespace CTF.Api.Models;

public class LearningPath
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Type { get; set; } = default!;
    public string? JobFamily { get; set; }

    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public string? Level { get; set; }
    public string Status { get; set; } = default!;
    public int Version { get; set; } = 1;

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
