namespace CTF.Api.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? SsoProvider { get; set; }
    public DateTime CreatedAt { get; set; }
}
