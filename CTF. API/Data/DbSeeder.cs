using Microsoft.EntityFrameworkCore;
using CTF.Api.Models;

namespace CTF.Api.Data;

public static class DbSeeder
{
    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");
    private static readonly Guid MainTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminId      = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // Fixed GUIDs for demo content (idempotent upsert)
    private static readonly Guid DemoPathId    = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DemoModule1Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid DemoModule2Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid DemoC1Id      = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly Guid DemoC2Id      = Guid.Parse("00000000-0000-0000-0000-000000000011");
    private static readonly Guid DemoC3Id      = Guid.Parse("00000000-0000-0000-0000-000000000012");
    private static readonly Guid DemoC4Id      = Guid.Parse("00000000-0000-0000-0000-000000000013");
    private static readonly Guid DemoC5Id      = Guid.Parse("00000000-0000-0000-0000-000000000014");
    private static readonly Guid DemoC6Id      = Guid.Parse("00000000-0000-0000-0000-000000000015");

    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        await SeedDemoTenantAsync(db, now);
        await SeedMainContentAsync(db, now);
    }

    // ── Tenant demo ────────────────────────────────────────────────────────────
    private static async Task SeedDemoTenantAsync(AppDbContext db, DateTime now)
    {
        if (!await db.Tenants.AnyAsync(t => t.Id == DemoTenantId))
        {
            db.Tenants.Add(new Tenant { Id = DemoTenantId, Name = "Demo", CreatedAt = now });
            await db.SaveChangesAsync();
        }

        if (!await db.Paths.AnyAsync(p => p.Id == DemoPathId))
        {
            db.Paths.Add(new LearningPath
            {
                Id          = DemoPathId,
                TenantId    = DemoTenantId,
                Type        = "demo",
                Title       = "Parcours Découverte — Introduction à la Cybersécurité",
                Description = "Découvrez les bases de la cybersécurité en 6 missions courtes et immersives. Aucun prérequis technique.",
                Level       = "beginner",
                Status      = "published",
                Version     = 1,
                CreatedBy   = AdminId,
                CreatedAt   = now,
                PublishedAt = now
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Modules.AnyAsync(m => m.Id == DemoModule1Id))
        {
            db.Modules.AddRange(
                new Module { Id = DemoModule1Id, TenantId = DemoTenantId, PathId = DemoPathId, Title = "Les bases", SortOrder = 1, CreatedAt = now },
                new Module { Id = DemoModule2Id, TenantId = DemoTenantId, PathId = DemoPathId, Title = "Bonnes pratiques", SortOrder = 2, CreatedAt = now }
            );
            await db.SaveChangesAsync();
        }

        // Upsert each challenge by fixed GUID
        await UpsertChallenge(db, new Challenge
        {
            Id            = DemoC1Id,
            TenantId      = DemoTenantId,
            ModuleId      = DemoModule1Id,
            Type          = "quiz",
            Title         = "Mot de passe : longueur ou complexité ?",
            Instructions  =
                "Tu crées un nouveau compte sur l'outil de ticketing de ton entreprise. " +
                "Le service IT t'impose un mot de passe fort — c'est toi qui choisis.\n\n" +
                "Laquelle de ces options est la plus résistante aux attaques par force brute ?\n\n" +
                "A) alice1990 — ton prénom suivi de ton année de naissance\n" +
                "B) Cheval-Soleil-Garage-Lune — quatre mots aléatoires assemblés\n" +
                "C) P@ssw0rd1 — avec des substitutions de caractères classiques",
            CorrectAnswer = "B|Une passphrase de 4 mots (~28 caractères) offre bien plus d'entropie qu'un mot court avec substitutions. " +
                            "Les attaquants testent en priorité les variantes prévisibles comme P@ssw0rd et les données personnelles (prénom+date). " +
                            "La longueur prime sur la complexité artificielle.",
            Difficulty    = 1,
            Points        = 50,
            Status        = "published",
            CreatedBy     = AdminId,
            CreatedAt     = now,
            PublishedAt   = now
        });

        await UpsertChallenge(db, new Challenge
        {
            Id            = DemoC2Id,
            TenantId      = DemoTenantId,
            ModuleId      = DemoModule1Id,
            Type          = "quiz",
            Title         = "Phishing : repérer le piège",
            Instructions  =
                "Tu ouvres ta messagerie ce matin. Un email attire ton attention :\n\n" +
                "📧 De : support@g00gle-secure.tk\n" +
                "📌 Objet : URGENT — Votre compte Google sera suspendu dans 24h\n" +
                "💬 « Cliquez ici pour vérifier votre identité : http://g00gle-secure.tk/verify »\n\n" +
                "Parmi ces réponses, laquelle décrit la meilleure réaction ?\n\n" +
                "A) Cliquer sur le lien pour vérifier si le compte est vraiment en danger\n" +
                "B) Identifier les indices suspects puis signaler l'email au service IT\n" +
                "C) Répondre à l'expéditeur pour demander des précisions",
            CorrectAnswer = "B|Indices : domaine g00gle-secure.tk (chiffre 0 à la place de la lettre o + extension .tk peu fiable), " +
                            "ton d'urgence artificielle (délai 24h), lien qui ne pointe pas vers google.com. " +
                            "Ne jamais cliquer — transférer au service IT via un canal séparé (Teams, appel).",
            Difficulty    = 1,
            Points        = 60,
            Status        = "published",
            CreatedBy     = AdminId,
            CreatedAt     = now,
            PublishedAt   = now
        });

        await UpsertChallenge(db, new Challenge
        {
            Id            = DemoC3Id,
            TenantId      = DemoTenantId,
            ModuleId      = DemoModule1Id,
            Type          = "scenario",
            Title         = "Clé USB abandonnée : que faire ?",
            Instructions  =
                "C'est lundi matin. En arrivant à ton bureau, tu trouves une clé USB dans le couloir, " +
                "étiquetée à la main : « Salaires 2024 ».\n\n" +
                "Que fais-tu ?\n\n" +
                "A) Tu la branches sur ton poste pour identifier à qui elle appartient\n" +
                "B) Tu la remets au service IT sans la brancher\n" +
                "C) Tu la gardes dans ton tiroir pour y revenir plus tard",
            CorrectAnswer = "B|Une clé USB abandonnée peut contenir un malware à exécution automatique (attaque HID / rubber ducky) " +
                            "qui s'installe dès la connexion. L'étiquette intrigante (« Salaires ») est délibérément conçue pour piquer ta curiosité. " +
                            "Seul le service IT peut l'analyser dans un environnement isolé.",
            Difficulty    = 1,
            Points        = 70,
            Status        = "published",
            CreatedBy     = AdminId,
            CreatedAt     = now,
            PublishedAt   = now
        });

        await UpsertChallenge(db, new Challenge
        {
            Id            = DemoC4Id,
            TenantId      = DemoTenantId,
            ModuleId      = DemoModule2Id,
            Type          = "scenario",
            Title         = "2FA : quelqu'un veut ton code",
            Instructions  =
                "Tu reçois un SMS : un code à 6 chiffres pour valider une connexion à ton compte pro.\n\n" +
                "Trente secondes plus tard, ton téléphone sonne. Une voix familière — " +
                "ou presque — te dit : « C'est Marc du support, on a eu un incident de sécurité. " +
                "Envoie-moi ce code par SMS tout de suite, c'est urgent. »\n\n" +
                "Que révèle cette situation et que fais-tu ?\n\n" +
                "A) Le compte est compromis — tu envoies le code puis changes ton mot de passe\n" +
                "B) C'est une attaque en temps réel — tu raccroches et ne transmets jamais le code\n" +
                "C) Le 2FA est défaillant — tu appelles le vrai support pour signaler le bug",
            CorrectAnswer = "B|Scénario classique de Real-Time Phishing : l'attaquant a déjà ton mot de passe et te soutire le code 2FA " +
                            "pendant la fenêtre de validité (30 secondes). Ne jamais partager un code OTP, même à quelqu'un qui " +
                            "se présente comme le support. Raccrocher, ne pas rappeler le numéro entrant, et signaler l'incident via un canal officiel.",
            Difficulty    = 1,
            Points        = 60,
            Status        = "published",
            CreatedBy     = AdminId,
            CreatedAt     = now,
            PublishedAt   = now
        });

        await UpsertChallenge(db, new Challenge
        {
            Id            = DemoC5Id,
            TenantId      = DemoTenantId,
            ModuleId      = DemoModule2Id,
            Type          = "scenario",
            Title         = "Mot de passe partagé par messagerie",
            Instructions  =
                "Il est 18h. Tu reçois ce message sur Teams :\n\n" +
                "💬 « Salut, je suis bloqué — j'ai oublié le mot de passe du compte partagé pour le projet X. " +
                "Tu peux me l'envoyer ici ou par email ? Merci ! »\n\n" +
                "Que fais-tu ?\n\n" +
                "A) Tu envoies le mot de passe par email (rapide et pratique)\n" +
                "B) Tu l'envoies via Teams (chiffrement de bout en bout)\n" +
                "C) Tu réinitialises le mot de passe partagé et le communiques via votre gestionnaire d'équipe ou en personne",
            CorrectAnswer = "C|Email et messagerie professionnelle ne sont pas des canaux sûrs pour des secrets : " +
                            "les messages peuvent être archivés, compromis ou consultés par des administrateurs. " +
                            "Un gestionnaire de mots de passe partagé (Bitwarden, 1Password Teams) ou une communication " +
                            "en face-à-face restent les seules options acceptables.",
            Difficulty    = 2,
            Points        = 70,
            Status        = "published",
            CreatedBy     = AdminId,
            CreatedAt     = now,
            PublishedAt   = now
        });

        await UpsertChallenge(db, new Challenge
        {
            Id            = DemoC6Id,
            TenantId      = DemoTenantId,
            ModuleId      = DemoModule2Id,
            Type          = "quiz",
            Title         = "Mises à jour : tout de suite ou plus tard ?",
            Instructions  =
                "Ton poste affiche une notification : « Mise à jour de sécurité disponible. Redémarrage requis. »\n\n" +
                "Tu es en plein travail sur un dossier important. Quelle est la meilleure décision ?\n\n" +
                "A) Ignorer la notification — les mises à jour n'apportent que de nouvelles fonctionnalités\n" +
                "B) Planifier la mise à jour pour le soir même ou au plus tôt\n" +
                "C) Reporter à la semaine prochaine quand tu auras du temps libre",
            CorrectAnswer = "B|Après la publication d'une CVE (faille connue), des exploits automatisés apparaissent parfois en quelques heures. " +
                            "Chaque heure sans correctif représente une fenêtre d'exposition active. " +
                            "Planifier la mise à jour dès le soir est le bon équilibre entre continuité de travail et sécurité.",
            Difficulty    = 2,
            Points        = 80,
            Status        = "published",
            CreatedBy     = AdminId,
            CreatedAt     = now,
            PublishedAt   = now
        });
    }

    // ── Contenu principal tenant 11111111 ────────────────────────────────────
    private static async Task SeedMainContentAsync(AppDbContext db, DateTime now)
    {
        if (await db.Paths.AnyAsync(p => p.TenantId == MainTenantId))
            return;

        if (!await db.Tenants.AnyAsync(t => t.Id == MainTenantId))
        {
            db.Tenants.Add(new Tenant { Id = MainTenantId, Name = "Organisation principale", CreatedAt = now });
            await db.SaveChangesAsync();
        }

        var pathId = Guid.NewGuid();

        db.Paths.Add(new LearningPath
        {
            Id          = pathId,
            TenantId    = MainTenantId,
            Type        = "general",
            Title       = "Parcours Général — Sensibilisation Cybersécurité",
            Description = "Un parcours sous forme de mini-missions (style escape game) pour maîtriser phishing, mots de passe, social engineering et ingénierie physique.",
            Level       = "beginner",
            Status      = "published",
            Version     = 1,
            CreatedBy   = AdminId,
            CreatedAt   = now,
            PublishedAt = now
        });

        var module1 = new Module { Id = Guid.NewGuid(), TenantId = MainTenantId, PathId = pathId, Title = "Phishing, mots de passe & sessions", SortOrder = 1, CreatedAt = now };
        var module2 = new Module { Id = Guid.NewGuid(), TenantId = MainTenantId, PathId = pathId, Title = "Social engineering", SortOrder = 2, CreatedAt = now };
        db.Modules.AddRange(module1, module2);

        // ── Module 1 ──────────────────────────────────────────────────────────

        // Email IDs referenced inside the JSON string literals for email-sort

        db.Challenges.AddRange(

            // M1-C1 — email-sort : Boîte mail à trier
            new Challenge
            {
                Id            = Guid.NewGuid(),
                TenantId      = MainTenantId,
                ModuleId      = module1.Id,
                Type          = "email-sort",
                Title         = "Mission 1 — Boîte mail : trie les priorités",
                Instructions  =
                    """
                    {
                      "context": "Tu reprends ton poste après une réunion de 2h. Quatre emails s'affichent dans ta messagerie. Classe-les du plus suspect au moins suspect en les glissant.",
                      "emails": [
                        {
                          "id": "em1",
                          "from": "admin@microsofft-helpdesk.tk",
                          "subject": "URGENT — Votre licence Office 365 expire dans 2h",
                          "preview": "Pour éviter la perte de vos données, cliquez immédiatement sur ce lien et saisissez vos identifiants Microsoft..."
                        },
                        {
                          "id": "em2",
                          "from": "it-support@votre-entreprise.com",
                          "subject": "Mise à jour obligatoire — Merci d'agir avant vendredi",
                          "preview": "Votre poste doit être mis à jour. Connectez-vous à l'intranet.votre-entreprise.com pour lancer la procédure."
                        },
                        {
                          "id": "em3",
                          "from": "notifications@linkedin.com",
                          "subject": "Sophie Martin a consulté votre profil LinkedIn",
                          "preview": "Vous avez 3 nouvelles demandes de connexion cette semaine..."
                        },
                        {
                          "id": "em4",
                          "from": "rh@votre-entreprise.com",
                          "subject": "Rappel : formulaire entretien annuel à compléter",
                          "preview": "Merci de remplir votre auto-évaluation sur l'intranet avant le 15 du mois."
                        }
                      ]
                    }
                    """,
                CorrectAnswer = "ordre:em1,em2,em3,em4|" +
                                "em1 est un phishing évident. em2 est légitime mais mérite vérification. em3 est une notification standard. em4 est un email RH classique.|" +
                                "Domaine microsofft-helpdesk.tk (double f + extension .tk), urgence extrême (2h), demande d'identifiants — phishing caractérisé.|" +
                                "Email IT interne légitime — vérifier que le lien pointe bien vers l'intranet interne avant de cliquer.|" +
                                "Notification d'un service connu sur domaine officiel linkedin.com — risque faible.|" +
                                "Email RH interne sans lien externe suspect — aucun signal d'alerte.",
                Difficulty    = 1,
                Points        = 80,
                Status        = "published",
                CreatedBy     = AdminId,
                CreatedAt     = now,
                PublishedAt   = now
            },

            // M1-C2 — email : Email d'hameçonnage simulé
            new Challenge
            {
                Id            = Guid.NewGuid(),
                TenantId      = MainTenantId,
                ModuleId      = module1.Id,
                Type          = "email",
                Title         = "Mission 2 — Email urgent : le piège classique",
                Instructions  =
                    """
                    {
                      "context": "Tu ouvres ta messagerie professionnelle. Parmi les nouveaux messages, cet email attire ton attention. Identifie les indices suspects et décris la bonne réaction.",
                      "from": "securite@arnazone-verify.com",
                      "subject": "⚠️ Action requise — Votre commande #FR-29471 est bloquée",
                      "body": "Cher client,\n\nNous avons détecté une activité inhabituelle sur votre compte. Pour sécuriser votre compte et débloquer votre commande, vous devez confirmer vos informations de paiement dans les 24 heures.\n\n➡️ Cliquez ici pour vérifier votre compte :\nhttp://arnazone-verify.com/secure/fr/login\n\nSi vous ne réalisez pas cette vérification, votre compte sera définitivement suspendu et votre commande annulée.\n\nCordialement,\nL'équipe Sécurité Amazon"
                    }
                    """,
                CorrectAnswer = "signaux|Indices clés : (1) domaine expéditeur arnazone-verify.com ≠ amazon.fr, (2) urgence artificielle (24h + suspension), " +
                                "(3) lien vers arnazone-verify.com et non amazon.fr, (4) Amazon ne demande jamais de confirmer ses coordonnées bancaires par email. " +
                                "Bonne réaction : ne pas cliquer, signaler au service IT, supprimer.",
                Difficulty    = 1,
                Points        = 60,
                Status        = "published",
                CreatedBy     = AdminId,
                CreatedAt     = now,
                PublishedAt   = now
            },

            // M1-C3 — quiz : Mot de passe
            new Challenge
            {
                Id            = Guid.NewGuid(),
                TenantId      = MainTenantId,
                ModuleId      = module1.Id,
                Type          = "quiz",
                Title         = "Mission 3 — Mot de passe : longueur > complexité ?",
                Instructions  =
                    "Tu dois créer le mot de passe maître de ton nouveau gestionnaire de mots de passe. " +
                    "Il protégera tous tes autres accès.\n\n" +
                    "Le service exige au moins 12 caractères. Lequel choisirais-tu ?\n\n" +
                    "A) P@ssw0rd123! — 12 caractères avec substitutions classiques\n" +
                    "B) correct-horse-battery-staple — passphrase de 4 mots, 30 caractères\n" +
                    "C) X7#kL2$mQ9!w — 12 caractères aléatoires complexes",
                CorrectAnswer = "B|La passphrase fait 30 caractères contre 12 — l'entropie est bien supérieure. " +
                                "Elle est aussi mémorisable sans la noter. P@ssw0rd123! figure dans toutes les listes de mots de passe courants. " +
                                "X7#kL2$mQ9!w est solide mais quasi impossible à retenir sans l'écrire quelque part, ce qui annule le bénéfice.",
                Difficulty    = 1,
                Points        = 60,
                Status        = "published",
                CreatedBy     = AdminId,
                CreatedAt     = now,
                PublishedAt   = now
            },

            // M1-C4 — scenario : Session non verrouillée
            new Challenge
            {
                Id            = Guid.NewGuid(),
                TenantId      = MainTenantId,
                ModuleId      = module1.Id,
                Type          = "scenario",
                Title         = "Mission 4 — Session déverrouillée : 2 minutes chrono",
                Instructions  =
                    "Il est 14h30. Tu reçois un appel urgent et tu quittes ton bureau précipitamment, " +
                    "laissant ton écran déverrouillé avec ton email professionnel et l'ERP ouverts.\n\n" +
                    "Que fais-tu pour éviter que ça se reproduise ?\n\n" +
                    "A) Rien — tes collègues sont de confiance et tu n'étais absent que 2 minutes\n" +
                    "B) Activer le verrouillage automatique après 2 min d'inactivité et utiliser Win+L avant chaque départ\n" +
                    "C) Fermer uniquement les onglets contenant des données sensibles avant de partir",
                CorrectAnswer = "B|Un écran déverrouillé donne accès à tous tes outils ouverts en quelques secondes — " +
                                "qu'il s'agisse d'un tiers malveillant ou d'une simple erreur d'un collègue. " +
                                "Win+L (Windows) ou Ctrl+Cmd+Q (Mac) verrouillent instantanément. " +
                                "Le verrouillage automatique à 2-3 min est le filet de sécurité si tu oublies.",
                Difficulty    = 2,
                Points        = 70,
                Status        = "published",
                CreatedBy     = AdminId,
                CreatedAt     = now,
                PublishedAt   = now
            },

            // ── Module 2 ──────────────────────────────────────────────────────────

            // M2-C1 — chat : Faux collègue
            new Challenge
            {
                Id            = Guid.NewGuid(),
                TenantId      = MainTenantId,
                ModuleId      = module2.Id,
                Type          = "chat",
                Title         = "Mission 5 — Faux collègue : \"Tu peux m'aider vite fait ?\"",
                Instructions  =
                    """
                    {
                      "context": "Tu reçois ce message sur Microsoft Teams d'un contact inconnu. Identifie les signaux d'ingénierie sociale et décris la bonne réaction.",
                      "contact": { "name": "Alex Fontaine", "title": "Nouveau consultant IT", "avatar": "A" },
                      "messages": [
                        { "from": "other", "text": "Salut ! Je commence ma mission ici la semaine prochaine. Mon manager est en déplacement jusqu'à jeudi." },
                        { "from": "other", "text": "Est-ce que tu peux m'envoyer les codes Wi-Fi invité et le numéro de badge pour l'accueil ? J'ai besoin de préparer mon arrivée." },
                        { "from": "other", "text": "C'est un peu urgent — j'ai une réunion de lancement lundi matin 😅 Merci !" }
                      ]
                    }
                    """,
                CorrectAnswer = "vérifier|Technique classique d'ingénierie sociale : urgence + manager absent + demande d'accès par messagerie. " +
                                "Ne jamais transmettre des accès (Wi-Fi, badge) sans vérification d'identité via un canal officiel. " +
                                "Réponse correcte : demander à l'IT ou aux RH de valider cette personne avant toute action. " +
                                "Si besoin urgent, l'accueil peut émettre un badge visiteur le jour J.",
                Difficulty    = 1,
                Points        = 70,
                Status        = "published",
                CreatedBy     = AdminId,
                CreatedAt     = now,
                PublishedAt   = now
            },

            // M2-C2 — scenario : Appel support IT
            new Challenge
            {
                Id            = Guid.NewGuid(),
                TenantId      = MainTenantId,
                ModuleId      = module2.Id,
                Type          = "scenario",
                Title         = "Mission 6 — Appel du support IT : vishing",
                Instructions  =
                    "Il est 9h15. Tu décroches ton téléphone.\n\n" +
                    "« Bonjour, je suis Marc du support IT central. On a détecté une activité suspecte sur ton compte. " +
                    "Pour sécuriser ça rapidement, j'ai besoin que tu me confirmes ton mot de passe temporaire " +
                    "ou que tu cliques sur le lien que je vais t'envoyer. »\n\n" +
                    "Que fais-tu ?\n\n" +
                    "A) Tu donnes l'information — c'est le support IT, il en a le droit\n" +
                    "B) Tu raccroches et rappelles le support IT via le numéro officiel de l'intranet\n" +
                    "C) Tu demandes son nom et numéro de badge, puis tu transmets l'info s'il répond",
                CorrectAnswer = "B|Le support IT légitime ne demande JAMAIS un mot de passe par téléphone. " +
                                "Cette attaque s'appelle le vishing (voice phishing). La seule réponse saine : " +
                                "raccrocher et rappeler via le numéro officiel que tu connais déjà (intranet, badge). " +
                                "Ne jamais utiliser le numéro fourni par l'appelant — il peut te rediriger vers l'attaquant.",
                Difficulty    = 2,
                Points        = 80,
                Status        = "published",
                CreatedBy     = AdminId,
                CreatedAt     = now,
                PublishedAt   = now
            },

            // M2-C3 — scenario : Tailgating
            new Challenge
            {
                Id            = Guid.NewGuid(),
                TenantId      = MainTenantId,
                ModuleId      = module2.Id,
                Type          = "scenario",
                Title         = "Mission 7 — Tailgating : la porte qui s'ouvre",
                Instructions  =
                    "Tu passes le sas sécurisé de l'open space avec ton badge. " +
                    "Derrière toi, quelqu'un s'approche rapidement en souriant : " +
                    "« Ah merci, j'ai oublié mon badge ce matin... »\n\n" +
                    "Que fais-tu ?\n\n" +
                    "A) Tu laisses la porte ouverte — c'est poli et il a l'air de travailler ici\n" +
                    "B) Tu lui expliques que tu ne peux pas et tu lui indiques l'accueil pour un badge visiteur\n" +
                    "C) Tu lui demandes son nom et son service, puis tu le laisses entrer s'il répond",
                CorrectAnswer = "B|Le tailgating (ou piggybacking) est une technique d'intrusion physique qui exploite la politesse. " +
                                "Même si la personne semble légitime, tu n'as aucun moyen de le vérifier sur le moment. " +
                                "La phrase clé : « Je ne peux pas vous laisser passer sans badge — l'accueil peut vous en créer un en 2 minutes. » " +
                                "Cela ne crée pas de conflit et respecte les règles de sécurité.",
                Difficulty    = 2,
                Points        = 75,
                Status        = "published",
                CreatedBy     = AdminId,
                CreatedAt     = now,
                PublishedAt   = now
            },

            // M2-C4 — scenario : CEO fraud
            new Challenge
            {
                Id            = Guid.NewGuid(),
                TenantId      = MainTenantId,
                ModuleId      = module2.Id,
                Type          = "scenario",
                Title         = "Mission 8 — \"Le directeur te demande ça maintenant\"",
                Instructions  =
                    "Il est 17h45. Tu reçois cet email depuis l'adresse : pdg@votre-entreprise.com\n\n" +
                    "« Je suis en réunion urgente avec un client stratégique. J'ai besoin que tu effectues " +
                    "un virement de 12 500 € sur ce RIB avant 18h. C'est confidentiel — n'en parle à personne. " +
                    "Confirme-moi par email quand c'est fait. »\n\n" +
                    "Que fais-tu ?\n\n" +
                    "A) Tu effectues le virement — c'est le PDG et le délai est réel\n" +
                    "B) Tu rappelles directement le PDG sur son numéro connu pour vérifier la demande\n" +
                    "C) Tu transmets l'email à la comptabilité pour qu'elle agisse à ta place",
                CorrectAnswer = "B|C'est une arnaque au président (CEO fraud / BEC). Les signaux : urgence extrême, confidentialité imposée, " +
                                "demande financière inhabituelle hors circuit normal. L'adresse email peut être usurpée (spoofing). " +
                                "Règle absolue : toute demande financière urgente doit être vérifiée par appel téléphonique direct " +
                                "sur un numéro connu — jamais par retour d'email.",
                Difficulty    = 3,
                Points        = 100,
                Status        = "published",
                CreatedBy     = AdminId,
                CreatedAt     = now,
                PublishedAt   = now
            },

            // M2-C5 — terminal : Fausse alerte sécurité (NOUVEAU)
            new Challenge
            {
                Id            = Guid.NewGuid(),
                TenantId      = MainTenantId,
                ModuleId      = module2.Id,
                Type          = "terminal",
                Title         = "Mission 9 — Fausse alerte virus : scareware",
                Instructions  =
                    """
                    {
                      "context": "Tu travailles sur un dossier important. Soudainement, cette fenêtre surgit et bloque ton écran.",
                      "popup": {
                        "title": "🚨 ALERTE CRITIQUE — VIRUS DÉTECTÉ",
                        "message": "Votre ordinateur est infecté par 3 menaces actives (Trojan.GenericKD, Spyware.InfoStealer, Rootkit.TDSS). Vos fichiers et mots de passe sont en danger immédiat.\n\nAppellez IMMÉDIATEMENT notre ligne de support sécurité :\n📞 +33 1 76 36 10 44\n\nNE PAS éteindre ou redémarrer votre ordinateur — cela aggraverait l'infection.",
                        "buttons": ["Appeler maintenant", "Supprimer les virus (Recommandé)", "Fermer"]
                      }
                    }
                    """,
                CorrectAnswer = "fermer|C'est un scareware (logiciel de peur) conçu pour pousser à appeler des arnaqueurs qui demanderont " +
                                "un accès distant à ton poste et/ou un paiement. Ton vrai antivirus ne bloque jamais l'écran pour " +
                                "te faire appeler un numéro. Bonne réaction : fermer la fenêtre (Alt+F4). " +
                                "Si impossible à fermer, redémarrer le PC. Puis lancer un vrai scan antivirus.",
                Difficulty    = 1,
                Points        = 60,
                Status        = "published",
                CreatedBy     = AdminId,
                CreatedAt     = now,
                PublishedAt   = now
            }
        );

        await db.SaveChangesAsync();
    }

    private static async Task UpsertChallenge(AppDbContext db, Challenge c)
    {
        var existing = await db.Challenges.FindAsync(c.Id);
        if (existing is null)
        {
            db.Challenges.Add(c);
        }
        else
        {
            existing.Instructions  = c.Instructions;
            existing.CorrectAnswer = c.CorrectAnswer;
            existing.Title         = c.Title;
            existing.Type          = c.Type;
            existing.Difficulty    = c.Difficulty;
            existing.Points        = c.Points;
        }
        await db.SaveChangesAsync();
    }
}
