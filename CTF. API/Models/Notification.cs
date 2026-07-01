using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    [MaxLength(40)]
    public string Type { get; set; } = "info";

    [MaxLength(500)]
    public string Message { get; set; } = default!;

    [MaxLength(300)]
    public string? Link { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
