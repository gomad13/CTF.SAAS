using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Models;

public class MandatoryAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PathId { get; set; }

    [MaxLength(20)]
    public string AssignedToType { get; set; } = "all_users";

    public Guid? AssignedToId { get; set; }

    public DateTime Deadline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
}
