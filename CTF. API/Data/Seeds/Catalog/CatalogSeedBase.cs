using CTF.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Helpers partagés pour les seeds du catalogue (parcours IsCatalog = true).
/// Les parcours catalogue utilisent TenantId = Guid.Empty (Demo) et sont ensuite
/// accordés aux autres tenants via TenantParcoursAccess par le SuperAdmin.
/// </summary>
internal static class CatalogSeedBase
{
    public static readonly Guid CatalogTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");
    public static readonly Guid CatalogAuthorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task UpsertPathAsync(AppDbContext db, LearningPath p)
    {
        var existing = await db.Paths.FindAsync(p.Id);
        if (existing is null)
        {
            db.Paths.Add(p);
        }
        else
        {
            existing.Title = p.Title;
            existing.Description = p.Description;
            existing.Level = p.Level;
            existing.Status = p.Status;
            existing.Version = p.Version;
            existing.IsCatalog = p.IsCatalog;
            existing.Sector = p.Sector;
            existing.EstimatedMinutes = p.EstimatedMinutes;
            existing.Tags = p.Tags;
            existing.Type = p.Type;
            existing.PublishedAt = p.PublishedAt;
        }
        await db.SaveChangesAsync();
    }

    public static async Task UpsertModuleAsync(AppDbContext db, Module m)
    {
        var existing = await db.Modules.FindAsync(m.Id);
        if (existing is null)
        {
            db.Modules.Add(m);
        }
        else
        {
            existing.Title = m.Title;
            existing.SortOrder = m.SortOrder;
            existing.PathId = m.PathId;
        }
        await db.SaveChangesAsync();
    }

    public static async Task UpsertChallengeAsync(AppDbContext db, Challenge c)
    {
        var existing = await db.Challenges.FindAsync(c.Id);
        if (existing is null)
        {
            db.Challenges.Add(c);
        }
        else
        {
            existing.Title = c.Title;
            existing.Instructions = c.Instructions;
            existing.Category = c.Category;
            existing.Type = c.Type;
            existing.ContentType = c.ContentType;
            existing.ContentJson = c.ContentJson;
            existing.CorrectAnswer = c.CorrectAnswer;
            existing.Points = c.Points;
            existing.Difficulty = c.Difficulty;
            existing.SortOrder = c.SortOrder;
            existing.ModuleId = c.ModuleId;
            existing.Status = c.Status;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Accorde automatiquement au tenant Demo l'accès à un parcours catalogue.
    /// Les autres tenants doivent être accordés manuellement par le SuperAdmin (business model à la carte).
    /// </summary>
    public static async Task EnsureDemoAccessAsync(AppDbContext db, Guid pathId, DateTime now)
    {
        var demoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

        var exists = await db.TenantParcoursAccesses
            .AnyAsync(a => a.TenantId == demoTenantId && a.PathId == pathId);

        if (exists) return;

        db.TenantParcoursAccesses.Add(new TenantParcoursAccess
        {
            Id = Guid.NewGuid(),
            TenantId = demoTenantId,
            PathId = pathId,
            GrantedAt = now,
            GrantedBy = CatalogAuthorId,
            RevokedAt = null,
            RevokedBy = null
        });
        await db.SaveChangesAsync();
    }
}
