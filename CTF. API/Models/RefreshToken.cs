using System.Text.Json.Serialization;

namespace CTF.Api.Models;

public class RefreshToken
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public Guid     UserId      { get; set; }
    [JsonIgnore]
    public string   Token       { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt   { get; set; }
    public bool     IsRevoked   { get; set; } = false;
    public string   CreatedByIp { get; set; } = string.Empty;
    public string?  RevokedByIp { get; set; }
    public DateTime? RevokedAt  { get; set; }

    /// <summary>[MULTI-SOCIETES] Société active au moment de l'émission, restaurée au refresh.</summary>
    public Guid?    ActiveTenantId { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive  => !IsRevoked && !IsExpired;
}
