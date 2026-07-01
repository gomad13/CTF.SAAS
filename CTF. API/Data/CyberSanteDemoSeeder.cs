using Microsoft.EntityFrameworkCore;
using CTF.Api.Models;

namespace CTF.Api.Data;

/// <summary>
/// Seed du parcours démo « Cybersécurité en milieu médical » pour le tenant
/// CyberMed Innovations. Réutilise INTÉGRALEMENT les mécanismes existants :
///
///   • Module 1 — boîte mail (ContentType="mailbox") + question IA (ContentType="free_text")
///   • Module 2 — Q1 password_quiz (3 étapes QCM), Q2/Q4 free_text (Ollama),
///                Q3 multichoice (QCM serveur)
///
/// Aucune nouvelle entité, aucun nouvel endpoint : les ContentTypes existants
/// couvrent tous les besoins (QCM serveur via password_quiz/multichoice,
/// éval Ollama via free_text). La sécurité « IsCorrect jamais exposé avant
/// soumission » est déjà assurée par <c>StripSensitiveKeys</c> du
/// <c>ChallengeInteractiveController</c> (clé "is_correct" filtrée à la lecture).
///
/// Idempotent : Upsert sur Path/Module/Challenge par Id stable.
/// </summary>
public static class CyberSanteDemoSeeder
{
    // ── Tenant cible et identité de seed (admin technique placeholder) ──────
    private static readonly Guid CyberMedTenantId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid SeedAdminId      = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ── Guids stables (préfixe d0... = parcours démo médical CyberMed) ─────
    private static readonly Guid PathId    = Guid.Parse("d0000000-0000-0000-0000-000000000000");
    private static readonly Guid Module1Id = Guid.Parse("d0000000-0001-0000-0000-000000000000");
    private static readonly Guid Module2Id = Guid.Parse("d0000000-0002-0000-0000-000000000000");

    private static readonly Guid M1MailboxId  = Guid.Parse("d0000000-0001-0001-0000-000000000000");
    private static readonly Guid M1IaQId      = Guid.Parse("d0000000-0001-0002-0000-000000000000");
    private static readonly Guid M2Q1PcId     = Guid.Parse("d0000000-0002-0001-0000-000000000000");
    private static readonly Guid M2Q2UsbId    = Guid.Parse("d0000000-0002-0002-0000-000000000000");
    private static readonly Guid M2Q3SuppId   = Guid.Parse("d0000000-0002-0003-0000-000000000000");
    private static readonly Guid M2Q4SessId   = Guid.Parse("d0000000-0002-0004-0000-000000000000");

    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        await EnsurePathAsync(db, now);
        await EnsureModulesAsync(db, now);
        await UpsertChallengesAsync(db, now);
        await AssignToCyberMedUsersAsync(db, now);
    }

    // ── Path ─────────────────────────────────────────────────────────────────
    private static async Task EnsurePathAsync(AppDbContext db, DateTime now)
    {
        var path = await db.Paths.FindAsync(PathId);
        const string title       = "Cybersécurité en milieu médical";
        const string description = "Parcours de sensibilisation à la cybersécurité adapté au quotidien des professionnels de santé : reconnaître les menaces et adopter les bons réflexes.";

        if (path is null)
        {
            db.Paths.Add(new LearningPath
            {
                Id          = PathId,
                TenantId    = CyberMedTenantId,
                Type        = "demo",
                Title       = title,
                Description = description,
                Level       = "intermediate",
                Status      = "published",
                Version     = 1,
                CreatedBy   = SeedAdminId,
                CreatedAt   = now,
                PublishedAt = now,
            });
        }
        else
        {
            path.Title       = title;
            path.Description = description;
            path.Status      = "published";
        }
        await db.SaveChangesAsync();
    }

    // ── Modules ──────────────────────────────────────────────────────────────
    private static async Task EnsureModulesAsync(AppDbContext db, DateTime now)
    {
        var modules = new (Guid Id, string Title, int Order)[]
        {
            (Module1Id, "Module 1 — Sensibilisation cyber (boîte mail santé)", 1),
            (Module2Id, "Module 2 — Procédures et réflexes (scénarios concrets)", 2),
        };

        foreach (var (id, title, order) in modules)
        {
            var existing = await db.Modules.FindAsync(id);
            if (existing is null)
            {
                db.Modules.Add(new Module
                {
                    Id        = id,
                    TenantId  = CyberMedTenantId,
                    PathId    = PathId,
                    Title     = title,
                    SortOrder = order,
                    CreatedAt = now,
                });
            }
            else
            {
                existing.Title     = title;
                existing.SortOrder = order;
            }
        }
        await db.SaveChangesAsync();
    }

    // ── Challenges (6 au total : 2 dans M1, 4 dans M2) ──────────────────────
    private static async Task UpsertChallengesAsync(AppDbContext db, DateTime now)
    {
        await UpsertChallengeAsync(db, BuildModule1Mailbox(now));
        await UpsertChallengeAsync(db, BuildModule1IaQuestion(now));
        await UpsertChallengeAsync(db, BuildModule2Q1Ransomware(now));
        await UpsertChallengeAsync(db, BuildModule2Q2UsbKey(now));
        await UpsertChallengeAsync(db, BuildModule2Q3FakeSupport(now));
        await UpsertChallengeAsync(db, BuildModule2Q4LockedSession(now));
    }

    private static async Task UpsertChallengeAsync(AppDbContext db, Challenge c)
    {
        var existing = await db.Challenges.FindAsync(c.Id);
        if (existing is null)
        {
            db.Challenges.Add(c);
        }
        else
        {
            existing.TenantId                 = c.TenantId;
            existing.ModuleId                 = c.ModuleId;
            existing.Type                     = c.Type;
            existing.ContentType              = c.ContentType;
            existing.Category                 = c.Category;
            existing.Title                    = c.Title;
            existing.Instructions             = c.Instructions;
            existing.Difficulty               = c.Difficulty;
            existing.Points                   = c.Points;
            existing.SortOrder                = c.SortOrder;
            existing.Status                   = c.Status;
            existing.ContentJson              = c.ContentJson;
            existing.InstructionTitle         = c.InstructionTitle;
            existing.InstructionBody          = c.InstructionBody;
            existing.InstructionShortReminder = c.InstructionShortReminder;
            existing.PublishedAt              = c.PublishedAt;
        }
        await db.SaveChangesAsync();
    }

    // ── Module 1 — Boîte mail santé piégée ───────────────────────────────────
    private static Challenge BuildModule1Mailbox(DateTime now) => new()
    {
        Id           = M1MailboxId,
        TenantId     = CyberMedTenantId,
        ModuleId     = Module1Id,
        Type         = "interactive",
        ContentType  = "mailbox",
        Category     = "Analyse Email",
        Title        = "Boîte mail du cabinet — repérer les pièges",
        Instructions = "Cochez uniquement les emails que vous considérez comme dangereux ou suspects.",
        Difficulty   = 2,
        Points       = 150,
        SortOrder    = 1,
        Status       = "published",
        CreatedBy    = SeedAdminId,
        CreatedAt    = now,
        PublishedAt  = now,
        InstructionTitle         = "Identifie les emails piégés dans la boîte mail santé",
        InstructionBody          =
            "Tu es professionnel de santé. Voici la boîte mail de ton cabinet ce matin : 5 messages t'attendent.\n\n" +
            "Ton objectif : identifier les emails malveillants et les signaler. Attention, un email légitime se cache parmi eux — le signaler à tort serait aussi une erreur.\n\n" +
            "Indices à chercher :\n" +
            "- Domaine d'expédition suspect (variante d'un vrai domaine, .net/.com inhabituels)\n" +
            "- Urgence artificielle, confidentialité injustifiée, contournement de procédure\n" +
            "- Pièces jointes .zip ou liens externes non sécurisés\n" +
            "- Demande exposant des données patients (RGPD art. 9 — données de santé)\n\n" +
            "Prends ton temps, observe avant de cliquer.",
        InstructionShortReminder = "Coche uniquement les emails dangereux. Un faux signalement sur un email légitime compte aussi comme erreur.",
        ContentJson  = """
{
  "context": "Boîte mail du cabinet médical — lundi matin",
  "emails": [
    {
      "id": "m1-email-doctolib",
      "from_name": "Doctolib Sécurité",
      "from_address": "securite@doctolib-verif.fr",
      "subject": "Vérification urgente de votre compte praticien",
      "preview": "Votre compte Doctolib sera suspendu sous 24h si vous ne confirmez pas...",
      "sent_at": "Aujourd'hui, 08:12",
      "body": "Bonjour Docteur,\n\nNous avons détecté une activité inhabituelle sur votre compte praticien Doctolib. Pour éviter une suspension dans les 24 heures, vous devez confirmer immédiatement vos identifiants en cliquant sur le lien ci-dessous :\n\nhttp://doctolib-verif.fr/login\n\nSi vous ne procédez pas à cette vérification, votre compte sera désactivé et vos rendez-vous annulés.\n\nCordialement,\nService de Sécurité Doctolib",
      "is_dangerous": true,
      "red_flags": [
        "Domaine d'expédition non officiel (doctolib-verif.fr au lieu de doctolib.fr)",
        "Urgence artificielle (24 heures)",
        "Lien vers un faux portail",
        "Demande d'identifiants par email — Doctolib ne procède jamais ainsi"
      ]
    },
    {
      "id": "m1-email-labo",
      "from_name": "Laboratoire BioNordik",
      "from_address": "resultats@bionordik-secure.net",
      "subject": "Résultats d'analyses — Patient M. Perrin (document sécurisé)",
      "preview": "Veuillez trouver en pièce jointe les résultats du patient...",
      "sent_at": "Aujourd'hui, 09:34",
      "body": "Bonjour Docteur,\n\nVeuillez trouver en pièce jointe les résultats d'analyses de votre patient M. Perrin. Pour des raisons de sécurité, le document est compressé et protégé.\n\nOuvrez le fichier : resultats_perrin.zip\n\nMot de passe d'ouverture : transmis dans un email séparé.\n\nMerci de confirmer la bonne réception.\n\nLaboratoire BioNordik",
      "is_dangerous": true,
      "red_flags": [
        "Domaine douteux (bionordik-secure.net — pas l'extension habituelle d'un laboratoire français)",
        "Pièce jointe .zip protégée par mot de passe — technique classique pour échapper aux antivirus",
        "Pression à ouvrir le document",
        "Un vrai laboratoire transmet les résultats via le DMP ou une messagerie sécurisée de santé, pas un .zip"
      ]
    },
    {
      "id": "m1-email-direction",
      "from_name": "Dr Martin (Directeur)",
      "from_address": "direction.urgent@clinique-peupliers-dir.fr",
      "subject": "Demande confidentielle — traitement urgent",
      "preview": "Je suis en réunion et ne peux pas être dérangé...",
      "sent_at": "Aujourd'hui, 10:01",
      "body": "Bonjour,\n\nJe suis en réunion à l'ARS et ne peux pas être dérangé jusqu'à 17h.\n\nJ'ai besoin que vous procédiez en urgence au règlement d'une facture fournisseur de 8 450 €. C'est confidentiel : n'en parlez à personne pour l'instant.\n\nJe vous communique l'IBAN par retour de mail. Merci de votre discrétion.\n\nDr Martin\nDirecteur",
      "is_dangerous": true,
      "red_flags": [
        "Domaine usurpé (clinique-peupliers-dir.fr — variante du vrai domaine clinique-peupliers.fr)",
        "Combinaison urgence + confidentialité + contournement de procédure = signature de la fraude au président",
        "Demande financière exceptionnelle hors circuit habituel",
        "Indisponibilité téléphonique invoquée pour empêcher la vérification"
      ]
    },
    {
      "id": "m1-email-fuite",
      "from_name": "inconnu",
      "from_address": "contact@partage-medical-fr.com",
      "subject": "Liste de patients — accès partagé",
      "preview": "Voici le lien vers la liste complète des patients de votre service...",
      "sent_at": "Aujourd'hui, 10:42",
      "body": "Bonjour,\n\nVoici le lien vers la liste complète des patients de votre service, avec leurs coordonnées et pathologies, accessible librement ici :\n\nhttp://partage-medical-fr.com/patients-export\n\nCe lien est temporaire.",
      "is_dangerous": true,
      "red_flags": [
        "Exposition manifeste de données de santé (RGPD article 9 — données sensibles)",
        "Lien externe non sécurisé (http, pas https)",
        "Expéditeur inconnu sans contexte légitime",
        "Réflexe attendu : signaler immédiatement au DPO / RSSI — c'est une fuite potentielle à notifier à la CNIL sous 72h"
      ]
    },
    {
      "id": "m1-email-legit",
      "from_name": "Secrétariat Clinique des Peupliers",
      "from_address": "secretariat@clinique-peupliers.fr",
      "subject": "Planning astreintes — semaine 17",
      "preview": "Bonjour, vous trouverez ci-joint le planning des astreintes...",
      "sent_at": "Aujourd'hui, 11:05",
      "body": "Bonjour,\n\nVous trouverez ci-joint le planning des astreintes de la semaine 17.\n\nN'hésitez pas à signaler toute indisponibilité avant vendredi midi.\n\nBien cordialement,\nLe Secrétariat\nClinique des Peupliers",
      "is_dangerous": false,
      "red_flags": []
    }
  ]
}
"""
    };

    // ── Module 1 — Question IA finale (free_text Ollama) ─────────────────────
    private static Challenge BuildModule1IaQuestion(DateTime now) => new()
    {
        Id           = M1IaQId,
        TenantId     = CyberMedTenantId,
        ModuleId     = Module1Id,
        Type         = "interactive",
        ContentType  = "free_text",
        Category     = "RGPD — Échange de données de santé",
        Title        = "Transmettre un compte-rendu opératoire en toute sécurité",
        Instructions = "Rédigez votre démarche. L'IA évaluera votre réponse contre une grille pédagogique cachée.",
        Difficulty   = 3,
        Points       = 150,
        SortOrder    = 2,
        Status       = "published",
        CreatedBy    = SeedAdminId,
        CreatedAt    = now,
        PublishedAt  = now,
        InstructionTitle         = "Échange de données de santé : la méthode",
        InstructionBody          =
            "Tu dois transmettre un compte-rendu opératoire à un confrère d'un autre établissement.\n\n" +
            "Ce document contient des données de santé — données sensibles au sens du RGPD (article 9).\n\n" +
            "Explique précisément comment tu procèdes pour rester conforme : canal, vérifications, précautions, traçabilité.\n\n" +
            "Une IA pédagogique analysera ta réponse et te donnera un retour bienveillant en français.",
        InstructionShortReminder = "Détaille ta démarche : canal sécurisé, vérification du destinataire, minimisation, traçabilité.",
        ContentJson  = """
{
  "questions": [
    {
      "id": "m1-q-msm",
      "question": "Vous devez transmettre un compte-rendu opératoire (rapport du bloc) contenant des données de santé à un confrère d'un autre établissement. Comment vous y prenez-vous pour le faire de manière sécurisée et conforme au RGPD ?",
      "context": "cybersécurité en milieu médical (MSSanté, RGPD article 9)",
      "expected_elements": "Utiliser une messagerie sécurisée de santé (MSSanté ou équivalent professionnel chiffré) plutôt qu'un email classique. Ne jamais envoyer via un email personnel ou non chiffré. Vérifier l'identité exacte du destinataire avant envoi. Chiffrer le document si transmission par un autre canal. Appliquer la minimisation : ne transmettre que les données strictement nécessaires au soin. Tracer l'envoi (qui, quand, à qui). Bannir les clés USB non chiffrées et les services cloud grand public.",
      "min_chars": 80
    }
  ]
}
"""
    };

    // ── Module 2 — Q1 — PC infecté (password_quiz : 3 étapes QCM) ────────────
    private static Challenge BuildModule2Q1Ransomware(DateTime now) => new()
    {
        Id           = M2Q1PcId,
        TenantId     = CyberMedTenantId,
        ModuleId     = Module2Id,
        Type         = "interactive",
        ContentType  = "password_quiz",
        Category     = "Réponse à incident — Rançongiciel",
        Title        = "PC infecté en consultation — la bonne procédure",
        Instructions = "Trois étapes successives. À chaque étape, choisissez la bonne réaction.",
        Difficulty   = 2,
        Points       = 150,
        SortOrder    = 1,
        Status       = "published",
        CreatedBy    = SeedAdminId,
        CreatedAt    = now,
        PublishedAt  = now,
        InstructionTitle         = "Rançongiciel sur ton poste : les 3 premiers réflexes",
        InstructionBody          =
            "Tu es en consultation. Ton ordinateur ralentit fortement, des fenêtres s'ouvrent toutes seules, un message réclame un paiement pour « débloquer tes fichiers ». Tu suspectes un rançongiciel.\n\n" +
            "Trois étapes vont s'enchaîner :\n" +
            "1. Quelle est la réaction immédiate ?\n" +
            "2. Qui prévenir, et comment ?\n" +
            "3. Comment assurer la continuité des soins en attendant ?\n\n" +
            "Pour chaque étape, choisis la bonne option. Tu verras les explications à la fin.",
        InstructionShortReminder = "3 étapes : réaction immédiate → alerte → continuité des soins. Choisis la bonne option à chaque étape.",
        ContentJson  = """
{
  "intro": "Vous êtes en consultation. Votre ordinateur se met à ralentir fortement, des fenêtres s'ouvrent toutes seules, et un message réclame un paiement pour 'débloquer vos fichiers'. Vous suspectez un rançongiciel (ransomware).",
  "rounds": [
    {
      "id": "etape-1",
      "question": "Étape 1/3 — Réaction immédiate : que faites-vous EN PREMIER ?",
      "choices": [
        {
          "id": "pay",
          "label": "Je paie la rançon demandée pour récupérer rapidement l'accès aux fichiers",
          "is_correct": false,
          "explanation": "Payer ne garantit pas la restitution des fichiers et finance directement les attaquants. L'ANSSI déconseille fermement de céder à toute demande de rançon — la priorité est d'isoler le poste et de signaler l'incident."
        },
        {
          "id": "isolate",
          "label": "Je déconnecte le poste du réseau (débrancher le câble / couper le wifi) sans l'éteindre",
          "is_correct": true,
          "explanation": "Isoler le poste du réseau stoppe la propagation du rançongiciel aux autres machines de l'établissement, tout en préservant les traces présentes en mémoire (utiles à l'analyse forensique). On n'éteint pas brutalement, on déconnecte."
        },
        {
          "id": "shutdown",
          "label": "J'éteins immédiatement l'ordinateur en maintenant le bouton power",
          "is_correct": false,
          "explanation": "Éteindre brutalement détruit les éléments présents en RAM qui sont précieux pour l'analyse. Le bon ordre est : déconnecter du réseau d'abord, puis laisser l'équipe SI prendre la main."
        },
        {
          "id": "ignore",
          "label": "Je continue à travailler en ignorant le message, ça va sûrement passer",
          "is_correct": false,
          "explanation": "Ignorer laisse au rançongiciel le temps de chiffrer davantage de fichiers et de se propager. La fenêtre d'action pour contenir l'incident est courte."
        }
      ]
    },
    {
      "id": "etape-2",
      "question": "Étape 2/3 — Alerte : qui prévenez-vous, et comment ?",
      "choices": [
        {
          "id": "alone",
          "label": "Personne, je gère ça seul pour ne pas inquiéter mes collègues",
          "is_correct": false,
          "explanation": "Un rançongiciel est un incident de sécurité majeur. Le garder pour soi prive l'établissement d'une réponse coordonnée et retarde la mise en sécurité des autres postes."
        },
        {
          "id": "rssi",
          "label": "Je préviens immédiatement le référent informatique / RSSI selon la procédure de l'établissement",
          "is_correct": true,
          "explanation": "Le signalement immédiat au RSSI / référent SI permet une prise en charge rapide et coordonnée : déconnexion réseau, isolation, déclaration ANSSI/CNIL si données de santé impactées."
        },
        {
          "id": "colleagues",
          "label": "Je préviens uniquement mes collègues proches par message",
          "is_correct": false,
          "explanation": "Avertir les collègues du risque est utile, mais c'est le RSSI / référent SI qui doit piloter la réponse — c'est la procédure formelle de l'établissement."
        },
        {
          "id": "wait",
          "label": "J'attends de voir si le problème s'aggrave avant d'alerter qui que ce soit",
          "is_correct": false,
          "explanation": "Chaque minute d'attente laisse le rançongiciel chiffrer davantage. Plus l'alerte est précoce, mieux l'incident est contenu."
        }
      ]
    },
    {
      "id": "etape-3",
      "question": "Étape 3/3 — Continuité des soins : en attendant l'intervention SI, comment continuez-vous à soigner ?",
      "choices": [
        {
          "id": "stop",
          "label": "J'arrête toute activité dans le service jusqu'à la réparation complète",
          "is_correct": false,
          "explanation": "Arrêter les soins n'est pas acceptable en milieu médical. Tout établissement doit prévoir un plan de continuité (procédure dégradée) précisément pour ce genre de situation."
        },
        {
          "id": "degraded",
          "label": "Je passe en procédure dégradée (papier) et j'utilise un autre poste sain si nécessaire",
          "is_correct": true,
          "explanation": "La procédure dégradée (retour au papier) garantit la continuité des soins pendant l'intervention. On n'utilise plus le poste compromis ; on peut basculer sur un poste sain reconnu par le SI."
        },
        {
          "id": "keep",
          "label": "Je continue à utiliser mon poste infecté, les soins passent avant la sécurité",
          "is_correct": false,
          "explanation": "Continuer sur le poste compromis aggrave la situation : données patients exposées, propagation amplifiée. La procédure dégradée permet justement d'assurer les soins SANS toucher au poste infecté."
        },
        {
          "id": "usb",
          "label": "Je transfère les dossiers du poste infecté vers une clé USB pour continuer ailleurs",
          "is_correct": false,
          "explanation": "Brancher une clé USB sur un poste compromis est exactement le vecteur de propagation que cherche le rançongiciel. À ne jamais faire."
        }
      ]
    }
  ]
}
"""
    };

    // ── Module 2 — Q2 — Clé USB trouvée (free_text Ollama) ───────────────────
    private static Challenge BuildModule2Q2UsbKey(DateTime now) => new()
    {
        Id           = M2Q2UsbId,
        TenantId     = CyberMedTenantId,
        ModuleId     = Module2Id,
        Type         = "interactive",
        ContentType  = "free_text",
        Category     = "Hygiène numérique — Médias amovibles",
        Title        = "Une clé USB trouvée dans le couloir",
        Instructions = "Décrivez votre réaction. L'IA évaluera votre réponse.",
        Difficulty   = 2,
        Points       = 100,
        SortOrder    = 2,
        Status       = "published",
        CreatedBy    = SeedAdminId,
        CreatedAt    = now,
        PublishedAt  = now,
        InstructionTitle         = "Clé USB inconnue : ouvrir ou ne pas ouvrir ?",
        InstructionBody          =
            "Tu trouves une clé USB dans le couloir de l'établissement. Un papier est scotché dessus : « À ne pas perdre — données patients ».\n\n" +
            "Décris ce que tu fais, et surtout ce que tu NE fais PAS. Pense aussi à l'aspect confidentialité des données patients qui pourraient se trouver dessus.\n\n" +
            "L'IA évalue ta démarche contre une grille pédagogique et te donne un retour bienveillant.",
        InstructionShortReminder = "Clé USB inconnue, étiquette « données patients » : quels réflexes (et lesquels surtout pas) ?",
        ContentJson  = """
{
  "questions": [
    {
      "id": "m2-q2-usb",
      "question": "Vous trouvez une clé USB dans le couloir de l'établissement. Un papier est scotché dessus avec écrit : 'À ne pas perdre — données patients'. Que faites-vous ? Décrivez précisément.",
      "context": "cybersécurité en milieu médical (médias amovibles, confidentialité patient)",
      "expected_elements": "NE PAS brancher la clé sur un poste : risque de malware (clé piégée, technique BadUSB ou similaire). NE PAS tenter de l'ouvrir pour 'retrouver le propriétaire'. Remettre la clé au référent informatique / sécurité ou à l'accueil selon la procédure interne. Signaler la découverte — une clé contenant des données patients en libre accès dans un couloir est un incident potentiel de confidentialité. Rappeler que les données patients ne devraient jamais transiter par une clé non chiffrée.",
      "min_chars": 60
    }
  ]
}
"""
    };

    // ── Module 2 — Q3 — Faux support logiciel (multichoice QCM serveur) ──────
    private static Challenge BuildModule2Q3FakeSupport(DateTime now) => new()
    {
        Id           = M2Q3SuppId,
        TenantId     = CyberMedTenantId,
        ModuleId     = Module2Id,
        Type         = "interactive",
        ContentType  = "multichoice",
        Category     = "Ingénierie sociale — Vishing",
        Title        = "Faux appel du support du logiciel métier",
        Instructions = "Choisissez la bonne réaction face à cet appel.",
        Difficulty   = 1,
        Points       = 100,
        SortOrder    = 3,
        Status       = "published",
        CreatedBy    = SeedAdminId,
        CreatedAt    = now,
        PublishedAt  = now,
        InstructionTitle         = "Le « support IT » t'appelle. Vraiment ?",
        InstructionBody          =
            "Tu reçois un appel téléphonique. La personne se présente comme un technicien de l'éditeur de ton logiciel de dossier patient. Elle te demande ton identifiant et ton mot de passe pour « finaliser une mise à jour de sécurité urgente ».\n\n" +
            "Choisis la bonne réaction. Pense à : qui demande quoi, comment vérifier l'authenticité, comment signaler.",
        InstructionShortReminder = "On ne donne JAMAIS ses identifiants par téléphone. Choisis la bonne réaction.",
        ContentJson  = """
{
  "scenario": {
    "title": "Appel téléphonique du support logiciel métier",
    "context": "Vous recevez un appel. La personne se présente comme un technicien de l'éditeur de votre logiciel de dossier patient. Elle vous explique qu'une mise à jour de sécurité urgente est nécessaire et vous demande votre identifiant et votre mot de passe pour 'finaliser l'opération à distance'."
  },
  "question": "Quelle est la bonne réaction ?",
  "choices": [
    {
      "id": "give-all",
      "label": "Je communique mes identifiants : c'est le support officiel et la mise à jour est urgente",
      "is_correct": false,
      "explanation": "Un vrai support ne demande JAMAIS votre mot de passe. Communiquer ces identifiants donne à un attaquant un accès complet aux dossiers patients de l'établissement."
    },
    {
      "id": "verify",
      "label": "Je refuse de communiquer mes identifiants, je raccroche poliment, et je vérifie en rappelant l'éditeur via son numéro officiel",
      "is_correct": true,
      "explanation": "Aucun mot de passe ne se communique par téléphone, à personne, jamais. La procédure : raccrocher, rappeler l'éditeur via son numéro officiel connu (jamais celui donné par l'appelant), et signaler la tentative au référent sécurité. Cette technique d'ingénierie sociale s'appelle le vishing."
    },
    {
      "id": "id-only",
      "label": "Je donne seulement mon identifiant mais pas mon mot de passe, c'est moins risqué",
      "is_correct": false,
      "explanation": "Même l'identifiant seul ne se communique pas à un appelant non vérifié. Et un vrai support n'a pas besoin que vous le lui donniez — il dispose déjà des informations nécessaires."
    },
    {
      "id": "colleague",
      "label": "Je demande à un collègue de donner ses identifiants à la place des miens",
      "is_correct": false,
      "explanation": "Le problème n'est pas qui donne les identifiants : personne ne doit les communiquer par téléphone à un appelant non vérifié. La règle est la même pour tous."
    }
  ],
  "savoir_plus": "Réflexe à retenir : en cas de doute sur un appel, on raccroche et on rappelle via un numéro officiel. On signale aussi la tentative au référent sécurité — vous n'êtes peut-être pas la seule cible dans l'établissement."
}
"""
    };

    // ── Module 2 — Q4 — Session laissée ouverte (free_text Ollama) ───────────
    private static Challenge BuildModule2Q4LockedSession(DateTime now) => new()
    {
        Id           = M2Q4SessId,
        TenantId     = CyberMedTenantId,
        ModuleId     = Module2Id,
        Type         = "interactive",
        ContentType  = "free_text",
        Category     = "Hygiène numérique — Verrouillage poste",
        Title        = "Session ouverte sur dossier patient — appel d'urgence",
        Instructions = "Décrivez ce que vous faites avant de partir. L'IA évaluera votre réponse.",
        Difficulty   = 1,
        Points       = 80,
        SortOrder    = 4,
        Status       = "published",
        CreatedBy    = SeedAdminId,
        CreatedAt    = now,
        PublishedAt  = now,
        InstructionTitle         = "Quitter son poste : le réflexe verrouillage",
        InstructionBody          =
            "Tu es à ton poste de soin, le dossier d'un patient est ouvert à l'écran. On t'appelle en urgence au chevet d'un autre patient. Tu dois partir immédiatement.\n\n" +
            "Décris précisément ce que tu fais avec ton poste avant de partir, et pourquoi.\n\n" +
            "L'IA évalue ta démarche et te donne un retour bienveillant.",
        InstructionShortReminder = "Ton poste reste avec un dossier patient ouvert. Que fais-tu avant de quitter la pièce ?",
        ContentJson  = """
{
  "questions": [
    {
      "id": "m2-q4-session",
      "question": "Vous êtes à votre poste de soin, le dossier d'un patient est ouvert à l'écran. On vous appelle en urgence au chevet d'un autre patient. Vous devez partir immédiatement. Que faites-vous concernant votre poste ?",
      "context": "cybersécurité en milieu médical (verrouillage de session, RGPD article 9)",
      "expected_elements": "VERROUILLER la session avant de partir (raccourci Windows + L ou équivalent, carte CPS, etc.). NE JAMAIS laisser une session ouverte avec des données patients visibles et accessibles à toute personne passant. Enjeu RGPD : confidentialité des données de santé (article 9). Le réflexe vaut même pour une absence de quelques minutes — un visiteur, un autre soignant, un patient pourrait voir ou modifier le dossier. Verrouiller préserve aussi votre traçabilité : toute action ultérieure sera bien faite SOUS votre identité, pas sous celle de quelqu'un d'autre.",
      "min_chars": 50
    }
  ]
}
"""
    };

    // ── Assignment : tous les users CyberMed reçoivent le parcours ───────────
    private static async Task AssignToCyberMedUsersAsync(AppDbContext db, DateTime now)
    {
        var userIds = await db.Users
            .Where(u => u.TenantId == CyberMedTenantId)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in userIds)
        {
            var already = await db.Assignments.AnyAsync(a => a.UserId == userId && a.PathId == PathId);
            if (already) continue;

            db.Assignments.Add(new Assignment
            {
                Id         = Guid.NewGuid(),
                TenantId   = CyberMedTenantId,
                UserId     = userId,
                PathId     = PathId,
                Status     = Assignment.Statuses.Assigned,
                AssignedAt = now,
                UpdatedAt  = now,
            });
        }
        await db.SaveChangesAsync();
    }

    // ── Exposé pour les tests (constantes stables) ───────────────────────────
    public static class Ids
    {
        public static Guid Tenant     => CyberMedTenantId;
        public static Guid Path       => PathId;
        public static Guid Module1    => Module1Id;
        public static Guid Module2    => Module2Id;
        public static Guid M1Mailbox  => M1MailboxId;
        public static Guid M1IaQ      => M1IaQId;
        public static Guid M2Q1       => M2Q1PcId;
        public static Guid M2Q2       => M2Q2UsbId;
        public static Guid M2Q3       => M2Q3SuppId;
        public static Guid M2Q4       => M2Q4SessId;
    }
}
