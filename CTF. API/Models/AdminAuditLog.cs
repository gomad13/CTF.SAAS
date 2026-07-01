namespace CTF.Api.Models;

public class AdminAuditLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AdminId { get; set; }

    public string Action { get; set; } = default!;
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }
}