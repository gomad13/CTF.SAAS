using CTF.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services;

/// <summary>
/// Source unique de vérité pour résoudre le TenantId d'un user SSO via son email.
///
/// Règles (prompt SSO 2026-04-23) :
///  1. Normaliser email (trim + lower)
///  2. Extraire domaine (après '@')
///  3. Match exact `TenantEmailDomains.Domain = @domain AND IsAutoProvisioningEnabled = true`
///     → retourner TenantId + IsAutoProvisioningEnabled = true
///  4. Match avec `IsAutoProvisioningEnabled = false`
///     → refuser avec code "provisioning_disabled"
///  5. Aucun match → fallback tenant Demo (`00000000-0000-0000-0000-000000000000`)
///  6. Demo inexistant → InvalidOperationException ("Demo tenant missing")
///
/// **Jamais** de TenantId = Guid.Empty.
/// </summary>
public class TenantResolutionService
{
    public static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

    private readonly AppDbContext _db;
    private readonly ILogger<TenantResolutionService> _logger;

    public TenantResolutionService(AppDbContext db, ILogger<TenantResolutionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public record Resolution(Guid TenantId, string Source, bool IsDemoFallback, bool ProvisioningDisabled, string? Domain);

    public async Task<Resolution> ResolveForEmailAsync(string email, CancellationToken ct = default)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var atIdx = email.IndexOf('@');
        if (atIdx <= 0 || atIdx == email.Length - 1)
        {
            _logger.LogWarning("[TenantResolution] Email invalide : {Email}", MaskEmail(email));
            throw new ArgumentException("Email invalide (pas de domaine).", nameof(email));
        }

        var domain = email[(atIdx + 1)..];

        // Exact match whitelist
        var whitelistEntry = await _db.TenantEmailDomains.AsNoTracking()
            .Where(d => d.Domain == domain)
            .Select(d => new { d.TenantId, d.IsAutoProvisioningEnabled })
            .FirstOrDefaultAsync(ct);

        if (whitelistEntry is not null)
        {
            if (!whitelistEntry.IsAutoProvisioningEnabled)
            {
                _logger.LogWarning("[TenantResolution] domain={Domain} tenant={TenantId} but provisioning disabled",
                    domain, whitelistEntry.TenantId);
                return new Resolution(whitelistEntry.TenantId, "whitelist", false, true, domain);
            }

            if (whitelistEntry.TenantId == Guid.Empty)
                throw new InvalidOperationException(
                    $"Whitelist entry for domain '{domain}' targets Guid.Empty — invalid configuration.");

            _logger.LogInformation("[TenantResolution] domain={Domain} → tenant={TenantId} (whitelist)",
                domain, whitelistEntry.TenantId);
            return new Resolution(whitelistEntry.TenantId, "whitelist", false, false, domain);
        }

        // Fallback Demo
        var demoExists = await _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.Id == DemoTenantId, ct);
        if (!demoExists)
            throw new InvalidOperationException(
                "Demo tenant missing (00000000-0000-0000-0000-000000000000). Contact admin — SSO fallback cannot proceed.");

        _logger.LogInformation("[TenantResolution] domain={Domain} → tenant=Demo (fallback)", domain);
        return new Resolution(DemoTenantId, "demo_fallback", true, false, domain);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "***";
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var domain = email[(at + 1)..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***@{domain}";
    }
}
