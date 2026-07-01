namespace CTF.Api.Models;

public class SuperAdminAuditLog
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public string   Action      { get; set; } = string.Empty;
    public string   Description { get; set; } = string.Empty;
    public string   PerformedBy { get; set; } = string.Empty;
    public string   IpAddress   { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public string   Severity    { get; set; } = "info"; // info | warning | critical
}
