using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services;

/// <summary>
/// Helpers centralisés pour l'activation/désactivation des modes entreprise sur un tenant.
/// </summary>
public static class ModeToggleHelper
{
    public enum Mode { Analytics, Compliance, Teams, Campaigns }

    public static async Task<ToggleModeResponseDto> ToggleAsync(
        AppDbContext db, Guid tenantId, Guid adminUserId, Mode mode, bool enabled, CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} introuvable.");

        var now = DateTime.UtcNow;
        switch (mode)
        {
            case Mode.Analytics:
                tenant.IsAnalyticsEnabled = enabled;
                tenant.AnalyticsUpdatedAt = now;
                tenant.AnalyticsUpdatedBy = adminUserId;
                break;
            case Mode.Compliance:
                tenant.IsComplianceEnabled = enabled;
                tenant.ComplianceUpdatedAt = now;
                tenant.ComplianceUpdatedBy = adminUserId;
                break;
            case Mode.Teams:
                tenant.IsTeamsEnabled = enabled;
                tenant.TeamsUpdatedAt = now;
                tenant.TeamsUpdatedBy = adminUserId;
                break;
            case Mode.Campaigns:
                tenant.IsCampaignsEnabled = enabled;
                tenant.CampaignsUpdatedAt = now;
                tenant.CampaignsUpdatedBy = adminUserId;
                break;
        }
        await db.SaveChangesAsync(ct);
        return new ToggleModeResponseDto(enabled, now, adminUserId);
    }

    public static async Task<bool> IsEnabledAsync(AppDbContext db, Guid tenantId, Mode mode, CancellationToken ct)
    {
        var column = mode switch
        {
            Mode.Analytics => "IsAnalyticsEnabled",
            Mode.Compliance => "IsComplianceEnabled",
            Mode.Teams => "IsTeamsEnabled",
            Mode.Campaigns => "IsCampaignsEnabled",
            _ => null,
        };
        if (column is null) return false;

        // Lecture fraiche du flag de mode (AsNoTracking -> pas de cache d'entite,
        // SELECT direct en READ COMMITTED). Remplace l'ancienne approche par
        // connexion Npgsql dediee qui echouait : GetDbConnection().ConnectionString
        // ne contient plus le mot de passe (Persist Security Info=false), donc la
        // nouvelle connexion partait sans mot de passe (erreur SCRAM -> 500).
        var flags = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new
            {
                t.IsAnalyticsEnabled,
                t.IsComplianceEnabled,
                t.IsTeamsEnabled,
                t.IsCampaignsEnabled,
            })
            .FirstOrDefaultAsync(ct);

        if (flags is null) return false;

        return mode switch
        {
            Mode.Analytics  => flags.IsAnalyticsEnabled,
            Mode.Compliance => flags.IsComplianceEnabled,
            Mode.Teams      => flags.IsTeamsEnabled,
            Mode.Campaigns  => flags.IsCampaignsEnabled,
            _ => false,
        };
    }
}
