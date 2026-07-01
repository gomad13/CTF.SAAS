using Microsoft.EntityFrameworkCore;
using CTF.Api.Models;

namespace CTF.Api.Data;

/// <summary>
/// Seed du tenant entreprise CyberMed Innovations + 5 employés.
/// Promeut h.madoumier@orange.fr en admin de ce tenant.
/// Idempotent.
/// </summary>
public static class CompanySeed
{
    private static readonly Guid CompanyTenantId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;

        // ── Tenant ─────────────────────────────────────────────────────────
        if (!await db.Tenants.AnyAsync(t => t.Id == CompanyTenantId))
        {
            db.Tenants.Add(new Tenant
            {
                Id        = CompanyTenantId,
                Name      = "CyberMed Innovations",
                CreatedAt = now,
            });
            await db.SaveChangesAsync();
        }

        // ── Whitelist SSO : domaine CyberMed → tenant ────────────────────────
        const string cmDomain = "cybermed-innovations.fr";
        if (!await db.TenantEmailDomains.AnyAsync(d => d.Domain == cmDomain))
        {
            db.TenantEmailDomains.Add(new TenantEmailDomain
            {
                Id = Guid.Parse("b0000000-0000-0000-0000-0000000000aa"),
                TenantId = CompanyTenantId,
                Domain = cmDomain,
                IsAutoProvisioningEnabled = true,
                CreatedAt = now,
                CreatedBy = null,
            });
            await db.SaveChangesAsync();
        }

        // ── 5 employés ──────────────────────────────────────────────────────
        var employees = new[]
        {
            ("b0000000-0000-0000-0000-000000000001", "claire.dupont@cybermed-innovations.fr",   "Claire", "Dupont"),
            ("b0000000-0000-0000-0000-000000000002", "thomas.bernard@cybermed-innovations.fr",  "Thomas", "Bernard"),
            ("b0000000-0000-0000-0000-000000000003", "sarah.martin@cybermed-innovations.fr",    "Sarah",  "Martin"),
            ("b0000000-0000-0000-0000-000000000004", "lucas.petit@cybermed-innovations.fr",     "Lucas",  "Petit"),
            ("b0000000-0000-0000-0000-000000000005", "amina.leblanc@cybermed-innovations.fr",   "Amina",  "Leblanc"),
        };

        foreach (var (idStr, email, first, last) in employees)
        {
            var id = Guid.Parse(idStr);
            if (await db.Users.AnyAsync(u => u.Id == id)) continue;

            db.Users.Add(new User
            {
                Id           = id,
                Email        = email,
                FirstName    = first,
                LastName     = last,
                DisplayName  = $"{first} {last}",
                Role         = "user",
                TenantId     = CompanyTenantId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employe@2026"),
                IsActive     = true,
                CreatedAt    = now,
            });
        }

        // ── Promouvoir h.madoumier en admin du tenant CyberMed ─────────────
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "h.madoumier@orange.fr");
        if (admin != null)
        {
            admin.Role     = "admin";
            admin.TenantId = CompanyTenantId;
        }

        await db.SaveChangesAsync();

        // ── Assigner les parcours démo + médical à tous les users CyberMed ──
        await AssignPathsToTenantUsersAsync(db, now);

        // ── 3 collaborateurs avec progression réaliste ─────────────────────
        await SeedCollaboratorsWithProgressAsync(db, now);
    }

    private static async Task SeedCollaboratorsWithProgressAsync(AppDbContext db, DateTime now)
    {
        var collaborators = new[]
        {
            ("c0000000-0000-0000-0000-000000000001", "j.marchand@cybermed-innovations.fr", "Julien", "Marchand", 45, 1),
            ("c0000000-0000-0000-0000-000000000002", "n.benali@cybermed-innovations.fr",  "Nadia",  "Benali",   30, 5),
            ("c0000000-0000-0000-0000-000000000003", "m.lefebvre@cybermed-innovations.fr","Marc",   "Lefebvre", 10, 8),
        };

        foreach (var (idStr, email, first, last, daysCreated, daysLogin) in collaborators)
        {
            var id = Guid.Parse(idStr);
            if (await db.Users.AnyAsync(u => u.Id == id)) continue;

            db.Users.Add(new User
            {
                Id           = id,
                Email        = email,
                FirstName    = first,
                LastName     = last,
                DisplayName  = $"{first} {last}",
                Role         = "user",
                TenantId     = CompanyTenantId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employe@2026"),
                IsActive     = true,
                CreatedAt    = now.AddDays(-daysCreated),
                LastLoginAt  = now.AddDays(-daysLogin),
            });
        }
        await db.SaveChangesAsync();

        var julienId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
        var nadiaId  = Guid.Parse("c0000000-0000-0000-0000-000000000002");
        var marcId   = Guid.Parse("c0000000-0000-0000-0000-000000000003");

        // Julien — 11 completions, scores élevés (90-100%)
        var julienCompletions = new (string ChallengeId, int Points, int Score, int DaysAgo)[]
        {
            ("10000000-0000-0000-0000-000000000001", 100, 100, 40),
            ("10000000-0000-0000-0000-000000000002", 138, 92,  39),
            ("10000000-0000-0000-0000-000000000003", 180, 90,  38),
            ("10000000-0000-0000-0000-000000000004", 158, 90,  37),
            ("10000000-0000-0000-0000-000000000005", 113, 90,  36),
            ("20000000-0000-0000-0000-000000000001", 100, 100, 20),
            ("20000000-0000-0000-0000-000000000008", 125, 100, 19),
            ("20000000-0000-0000-0000-000000000002", 113, 90,  18),
            ("20000000-0000-0000-0000-000000000007", 135, 90,  17),
            ("20000000-0000-0000-0000-000000000003", 158, 90,  16),
            ("20000000-0000-0000-0000-000000000004", 135, 90,  15),
        };

        // Nadia — 6 completions, scores moyens (60-80%)
        var nadiaCompletions = new (string ChallengeId, int Points, int Score, int DaysAgo)[]
        {
            ("10000000-0000-0000-0000-000000000001", 75,  75, 25),
            ("10000000-0000-0000-0000-000000000002", 105, 70, 24),
            ("10000000-0000-0000-0000-000000000003", 120, 60, 23),
            ("20000000-0000-0000-0000-000000000001", 80,  80, 10),
            ("20000000-0000-0000-0000-000000000008", 88,  70, 9),
            ("20000000-0000-0000-0000-000000000002", 75,  60, 8),
        };

        // Marc — 1 completion (débutant)
        var marcCompletions = new (string ChallengeId, int Points, int Score, int DaysAgo)[]
        {
            ("10000000-0000-0000-0000-000000000001", 50, 50, 8),
        };

        await InsertCompletionsAsync(db, julienId, julienCompletions, now);
        await InsertCompletionsAsync(db, nadiaId,  nadiaCompletions,  now);
        await InsertCompletionsAsync(db, marcId,   marcCompletions,   now);
    }

    private static async Task InsertCompletionsAsync(
        AppDbContext db,
        Guid userId,
        (string ChallengeId, int Points, int Score, int DaysAgo)[] completions,
        DateTime now)
    {
        foreach (var (challengeIdStr, points, score, daysAgo) in completions)
        {
            var challengeId = Guid.Parse(challengeIdStr);
            if (await db.ChallengeCompletions.AnyAsync(cc => cc.UserId == userId && cc.ChallengeId == challengeId))
                continue;

            var challenge = await db.Challenges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == challengeId);

            db.ChallengeCompletions.Add(new ChallengeCompletion
            {
                Id             = Guid.NewGuid(),
                UserId         = userId,
                TenantId       = CompanyTenantId,
                ChallengeId    = challengeId,
                ChallengeTitle = challenge?.Title ?? "—",
                PointsEarned   = points,
                ScorePercent   = score,
                IsDemo         = true,
                CompletedAt    = now.AddDays(-daysAgo),
            });
        }
        await db.SaveChangesAsync();
    }

    private static readonly Guid DemoPathId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid MedPathId  = Guid.Parse("20000000-0000-0000-0000-000000000000");

    private static async Task AssignPathsToTenantUsersAsync(AppDbContext db, DateTime now)
    {
        var pathIds = new[] { DemoPathId, MedPathId };

        // All CyberMed users (admin + employees)
        var userIds = await db.Users
            .Where(u => u.TenantId == CompanyTenantId)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in userIds)
        {
            foreach (var pathId in pathIds)
            {
                // Check path exists
                if (!await db.Paths.AnyAsync(p => p.Id == pathId)) continue;

                // Skip if already assigned
                if (await db.Assignments.AnyAsync(a => a.UserId == userId && a.PathId == pathId))
                    continue;

                db.Assignments.Add(new Assignment
                {
                    Id         = Guid.NewGuid(),
                    TenantId   = CompanyTenantId,
                    UserId     = userId,
                    PathId     = pathId,
                    Status     = Assignment.Statuses.Assigned,
                    AssignedAt = now,
                    UpdatedAt  = now
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
