using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Models;

public class TenantEmailDomain
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [MaxLength(255)]
    public string Domain { get; set; } = default!;

    public bool IsAutoProvisioningEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
}
