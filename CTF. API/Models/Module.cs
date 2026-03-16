namespace CTF.Api.Models;

public class Module
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PathId { get; set; }

    public string Title { get; set; } = default!;
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}
