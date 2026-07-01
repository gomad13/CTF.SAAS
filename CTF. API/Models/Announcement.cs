namespace CTF.Api.Models;

public class Announcement
{
    public Guid      Id        { get; set; } = Guid.NewGuid();
    public string    Title     { get; set; } = string.Empty;
    public string    Message   { get; set; } = string.Empty;
    public string    Type      { get; set; } = "info"; // info | warning | maintenance | update
    public Guid?     TenantId  { get; set; }           // null = tous
    public bool      IsActive  { get; set; } = true;
    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string    CreatedBy { get; set; } = string.Empty;
}
