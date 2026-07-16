using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Models;

public class TenantEmailDomain
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [MaxLength(255)]
    public string Domain { get; set; } = default!;

    public bool IsAutoProvisioningEnabled { get; set; } = true;

    // ── Vérification de propriété par DNS TXT (PASSE 1) ──────────────────────
    // Token nonce publié en DNS (_sentys-verification.<domaine> TXT = "sentys-verify=<token>").
    // Non secret (destiné à être publié) mais non devinable (CSPRNG). null = ligne historique déjà vérifiée.
    [MaxLength(64)]
    public string? VerificationToken { get; set; }

    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? LastCheckedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
}
