namespace CTF.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? TeamId { get; set; }

    public string Email { get; set; } = default!;
    public string? DisplayName { get; set; }

    public string Role { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // ✅ Ajout pour import CSV / admin panel
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
}
