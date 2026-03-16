namespace CTF.Api.Models;

public class Team
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
