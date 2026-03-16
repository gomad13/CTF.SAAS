namespace CTF.Api.Models;

public class Challenges
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ModuleId { get; set; }

    public string Type { get; set; } = default!;         // email, chat, web, quiz, document
    public string Title { get; set; } = default!;
    public string Instructions { get; set; } = default!;

    public int? Difficulty { get; set; }                 // 1-3
    public int Points { get; set; } = 10;

    public string Status { get; set; } = default!;       // draft, published, archived

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
