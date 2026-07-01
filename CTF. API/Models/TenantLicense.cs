namespace CTF.Api.Models;

public class TenantLicense
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public Guid     TenantId  { get; set; }
    public string   Plan      { get; set; } = "trial"; // trial | starter | pro | enterprise
    public int      MaxUsers  { get; set; } = 5;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
    public bool     IsActive  { get; set; } = true;
    public string   Notes     { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
