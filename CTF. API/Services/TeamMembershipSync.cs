using CTF.Api.Data;
using CTF.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services;

/// <summary>
/// Maintient la cohérence entre l'« équipe principale » dénormalisée (<see cref="User.TeamId"/>)
/// et la table d'appartenance many-to-many (<see cref="TeamMembership"/>) lorsqu'une écriture
/// hors du service Équipes modifie l'équipe d'un utilisateur (annuaire, création d'utilisateur…).
///
/// Les opérations sont ajoutées au change-tracker ; c'est l'appelant qui appelle SaveChanges.
/// </summary>
public static class TeamMembershipSync
{
    /// <summary>
    /// Aligne les appartenances après l'affectation d'une équipe principale.
    /// <paramref name="newTeamId"/> null = retrait de TOUTES les équipes ; sinon garantit
    /// (sans doublon) l'appartenance à cette équipe, sans toucher aux autres (multi-équipes préservé).
    /// </summary>
    public static async Task ApplyPrimaryTeamAsync(
        AppDbContext db, Guid tenantId, Guid userId, Guid? newTeamId, CancellationToken ct = default)
    {
        if (newTeamId is null)
        {
            var all = await db.TeamMemberships
                .Where(m => m.TenantId == tenantId && m.UserId == userId)
                .ToListAsync(ct);
            if (all.Count > 0) db.TeamMemberships.RemoveRange(all);
            return;
        }

        var exists = await db.TeamMemberships
            .AnyAsync(m => m.TeamId == newTeamId.Value && m.UserId == userId, ct);
        if (!exists)
        {
            db.TeamMemberships.Add(new TeamMembership
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TeamId = newTeamId.Value,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
            });
        }
    }
}
