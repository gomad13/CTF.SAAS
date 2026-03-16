using Microsoft.EntityFrameworkCore;
using CTF.Api.Models;

namespace CTF.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Ne seed qu'une seule fois
        if (await db.Paths.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        // IDs stables pour tests (tu peux changer si tu veux)
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var adminId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // ─────────────────────────────
        // LEARNING PATH (Général)
        // ─────────────────────────────
        var pathId = Guid.NewGuid();

        db.Paths.Add(new LearningPath
        {
            Id = pathId,
            TenantId = tenantId,
            Type = "general",
            JobFamily = null,
            Title = "Parcours Général – Sensibilisation Cybersécurité",
            Description = "Un parcours généraliste sous forme de mini-missions (style escape game) pour apprendre les bases : phishing, mots de passe, sessions, social engineering.",
            Level = "beginner",
            Status = "published",
            Version = 1,
            CreatedBy = adminId,
            CreatedAt = now,
            PublishedAt = now
        });

        // ─────────────────────────────
        // MODULE 1 : Phishing / MDP / Sessions
        // ─────────────────────────────
        var module1 = new Module
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PathId = pathId,
            Title = "Phishing, mots de passe & sessions",
            SortOrder = 1,
            CreatedAt = now
        };

        // ─────────────────────────────
        // MODULE 2 : Social Engineering
        // ─────────────────────────────
        var module2 = new Module
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PathId = pathId,
            Title = "Social engineering",
            SortOrder = 2,
            CreatedAt = now
        };

        db.Modules.AddRange(module1, module2);

        // ─────────────────────────────
        // CHALLENGES (8)
        // Type: quiz / chat / email / scenario
        // Status: published
        // ─────────────────────────────
        db.Challenges.AddRange(
            // Module 1 (4)
            new Challenges
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = module1.Id,
                Type = "email",
                Title = "Mission 1 — Email urgent : le piège classique",
                Instructions = "Tu reçois un email \"URGENT\" te demandant de cliquer sur un lien. Identifie au moins 3 signaux suspects (expéditeur, lien, ton, urgence, pièce jointe...).",
                Difficulty = 1,
                Points = 50,
                Status = "published",
                CreatedBy = adminId,
                CreatedAt = now,
                PublishedAt = now
            },
            new Challenges
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = module1.Id,
                Type = "quiz",
                Title = "Mission 2 — Mot de passe : longueur > complexité ?",
                Instructions = "Choisis le mot de passe le plus solide parmi plusieurs propositions et explique pourquoi (longueur, unicité, gestionnaire...).",
                Difficulty = 1,
                Points = 60,
                Status = "published",
                CreatedBy = adminId,
                CreatedAt = now,
                PublishedAt = now
            },
            new Challenges
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = module1.Id,
                Type = "scenario",
                Title = "Mission 3 — Session non verrouillée",
                Instructions = "Tu quittes ton poste 2 minutes. Un collègue peut-il accéder à tes outils ? Liste les risques et la bonne pratique immédiate.",
                Difficulty = 2,
                Points = 70,
                Status = "published",
                CreatedBy = adminId,
                CreatedAt = now,
                PublishedAt = now
            },
            new Challenges
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = module1.Id,
                Type = "quiz",
                Title = "Mission 4 — 2FA : le filet de sécurité",
                Instructions = "Explique en 3 points pourquoi activer le 2FA réduit fortement le risque, même si le mot de passe fuite.",
                Difficulty = 2,
                Points = 80,
                Status = "published",
                CreatedBy = adminId,
                CreatedAt = now,
                PublishedAt = now
            },

            // Module 2 (4)
            new Challenges
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = module2.Id,
                Type = "chat",
                Title = "Mission 5 — Faux collègue : \"Tu peux m’aider vite fait ?\"",
                Instructions = "Quelqu’un se présente comme nouveau et demande des infos (badge, Wi-Fi, accès). Quelles questions poser / quels réflexes avoir ?",
                Difficulty = 1,
                Points = 60,
                Status = "published",
                CreatedBy = adminId,
                CreatedAt = now,
                PublishedAt = now
            },
            new Challenges
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = module2.Id,
                Type = "scenario",
                Title = "Mission 6 — Appel du support IT (pression)",
                Instructions = "On t’appelle : \"je suis du support IT\" et on te demande un code / validation. Que fais-tu ? Donne le bon protocole.",
                Difficulty = 2,
                Points = 80,
                Status = "published",
                CreatedBy = adminId,
                CreatedAt = now,
                PublishedAt = now
            },
            new Challenges
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = module2.Id,
                Type = "scenario",
                Title = "Mission 7 — Tailgating : la porte qui s’ouvre",
                Instructions = "Quelqu’un te suit pour entrer (politesse). Quels risques ? Quelle phrase simple dire sans créer de conflit ?",
                Difficulty = 2,
                Points = 75,
                Status = "published",
                CreatedBy = adminId,
                CreatedAt = now,
                PublishedAt = now
            },
            new Challenges
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = module2.Id,
                Type = "scenario",
                Title = "Mission 8 — \"Le directeur te demande ça maintenant\"",
                Instructions = "Une demande urgente \"hiérarchie\" arrive (virement, doc, accès). Quelles vérifications minimales fais-tu avant d’agir ?",
                Difficulty = 3,
                Points = 100,
                Status = "published",
                CreatedBy = adminId,
                CreatedAt = now,
                PublishedAt = now
            }
        );

        await db.SaveChangesAsync();
    }
}
