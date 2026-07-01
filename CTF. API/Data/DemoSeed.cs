using Microsoft.EntityFrameworkCore;
using CTF.Api.Models;

namespace CTF.Api.Data;

/// <summary>
/// Seed du parcours démo interactif — 5 challenges de sensibilisation.
/// Supprime et recrée les challenges demo à chaque démarrage DEV.
/// </summary>
public static class DemoSeed
{
    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");
    private static readonly Guid AdminId      = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DemoPathId   = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DemoModuleId = Guid.Parse("10000000-0000-0000-0000-000000000000");
    private static readonly Guid C1Id         = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid C2Id         = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid C3Id         = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid C4Id         = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid C5Id         = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid C6Id         = Guid.Parse("10000000-0000-0000-0000-000000000006");

    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        await CleanDemoContentAsync(db);
        await EnsurePathAsync(db, now);
        await EnsureModuleAsync(db, now);
        await UpsertChallengesAsync(db, now);
        await AssignDemoPathToAllUsersAsync(db, now);
    }

    /// <summary>Assign the demo path to ALL users who don't have it yet.</summary>
    private static async Task AssignDemoPathToAllUsersAsync(AppDbContext db, DateTime now)
    {
        var allUserIds = await db.Users
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, u.TenantId })
            .ToListAsync();

        foreach (var u in allUserIds)
        {
            if (await db.Assignments.AnyAsync(a => a.UserId == u.Id && a.PathId == DemoPathId))
                continue;

            db.Assignments.Add(new Assignment
            {
                Id         = Guid.NewGuid(),
                TenantId   = u.TenantId,
                UserId     = u.Id,
                PathId     = DemoPathId,
                Status     = Assignment.Statuses.Assigned,
                AssignedAt = now,
                UpdatedAt  = now
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task CleanDemoContentAsync(AppDbContext db)
    {
        var oldChallenges = await db.Challenges
            .Where(c => c.TenantId == DemoTenantId)
            .ToListAsync();
        db.Challenges.RemoveRange(oldChallenges);

        var oldModules = await db.Modules
            .Where(m => m.TenantId == DemoTenantId && m.Id != DemoModuleId)
            .ToListAsync();
        db.Modules.RemoveRange(oldModules);

        await db.SaveChangesAsync();
    }

    private static async Task EnsurePathAsync(AppDbContext db, DateTime now)
    {
        var path = await db.Paths.FindAsync(DemoPathId);
        if (path is null)
        {
            db.Paths.Add(new LearningPath
            {
                Id          = DemoPathId,
                TenantId    = DemoTenantId,
                Type        = "demo",
                Title       = "Parcours Sensibilisation Cybersécurité",
                Description = "5 missions immersives pour reconnaître les cyberattaques et obligations légales.",
                Level       = "beginner",
                Status      = "published",
                Version     = 3,
                CreatedBy   = AdminId,
                CreatedAt   = now,
                PublishedAt = now
            });
        }
        else
        {
            path.Title       = "Parcours Sensibilisation Cybersécurité";
            path.Description = "5 missions immersives pour reconnaître les cyberattaques et obligations légales.";
            path.Version     = 3;
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureModuleAsync(AppDbContext db, DateTime now)
    {
        if (!await db.Modules.AnyAsync(m => m.Id == DemoModuleId))
        {
            db.Modules.Add(new Module
            {
                Id        = DemoModuleId,
                TenantId  = DemoTenantId,
                PathId    = DemoPathId,
                Title     = "Missions de sensibilisation",
                SortOrder = 1,
                CreatedAt = now
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task UpsertChallengesAsync(AppDbContext db, DateTime now)
    {
        // ── Challenge 1 — Arnaque au Président (ceo_fraud) ───────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C1Id,
            TenantId     = DemoTenantId,
            ModuleId     = DemoModuleId,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Ingénierie Sociale",
            Title        = "Arnaque au Président",
            Instructions = "Vous recevez un email urgent de votre PDG demandant un virement confidentiel. Analysez l'email et choisissez la ou les bonnes réactions.",
            Difficulty   = 1,
            Points       = 100,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Pierre Dumont — PDG",
    "from_address": "p.dumont@groupe-lemaire-direction.com",
    "to": "vous@entreprise.fr",
    "subject": "URGENT - Virement confidentiel à effectuer",
    "sent_at": "Aujourd'hui, 14h37",
    "body": "Bonjour,\n\nJe suis actuellement en réunion stratégique confidentielle à l'étranger et je ne suis pas joignable par téléphone.\n\nJ'ai besoin que vous effectuiez un virement urgent de 47 500 € sur le compte d'un nouveau partenaire. Cette opération est strictement confidentielle — n'en parlez à personne dans l'entreprise, y compris à votre responsable direct.\n\nRIB :\nBanque : Crédit Européen\nIBAN : DE89 3704 0044 0532 0130 00\nBénéficiaire : EuroPart GmbH\n\nJe compte sur votre discrétion et votre réactivité. Je vous rembourserai les détails dès mon retour.\n\nCordialement,\nPierre Dumont\nPrésident-Directeur Général\nGroupe Lemaire"
  },
  "choices": [
    { "id": "pay",    "label": "Effectuer le virement immédiatement",                    "icon": "bank",  "is_correct": false, "explanation": "C'est exactement ce que l'arnaqueur espère. Ne jamais effectuer un virement sur instruction par email, surtout en urgence et en secret." },
    { "id": "report", "label": "Signaler à votre responsable et à la direction",           "icon": "flag",  "is_correct": true,  "explanation": "Excellente réaction ! L'arnaque au président joue sur l'urgence et le secret. Signaler immédiatement brise le mécanisme de manipulation." },
    { "id": "nothing","label": "Ne rien faire et ignorer l'email",                         "icon": "x",     "is_correct": false, "explanation": "Ignorer ne suffit pas : l'arnaqueur va relancer. L'email doit être signalé pour protéger vos collègues." },
    { "id": "verify", "label": "Rappeler le PDG sur son numéro habituel pour vérifier",    "icon": "phone", "is_correct": true,  "explanation": "Très bonne approche ! Toujours vérifier par un canal indépendant (téléphone habituel, jamais celui donné dans l'email)." }
  ],
  "red_flags": [
    "Adresse email suspecte (groupe-lemaire-direction.com ≠ domaine officiel)",
    "Demande de confidentialité absolue",
    "Urgence artificielle",
    "Virement vers un IBAN étranger inconnu",
    "Indisponibilité téléphonique invoquée",
    "Demande hors procédure habituelle"
  ]
}
"""
        });

        // ── Challenge 2 — La Boîte Mail Piégée (mailbox) ─────────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C2Id,
            TenantId     = DemoTenantId,
            ModuleId     = DemoModuleId,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "La Boîte Mail Piégée",
            Instructions = "Voici votre boîte de réception. Cochez uniquement les emails que vous considérez comme dangereux ou suspects.",
            Difficulty   = 1,
            Points       = 150,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "emails": [
    { "id": "mail_1", "from_name": "Netflix", "from_address": "noreply@netflix-securite-compte.fr", "subject": "Votre compte va être suspendu — action requise", "preview": "Votre moyen de paiement a expiré. Cliquez ici pour mettre à jour...", "sent_at": "Aujourd'hui 09:12", "is_dangerous": true, "body": "Cher(e) client(e),\n\nNous n'avons pas pu renouveler votre abonnement Netflix. Votre compte sera suspendu dans 24h si vous ne mettez pas à jour vos informations de paiement.\n\nMettez à jour maintenant → http://netflix-paiement-fr.xyz/update\n\nL'équipe Netflix", "red_flags": ["Domaine suspect (netflix-securite-compte.fr)", "Lien vers un domaine .xyz non officiel", "Urgence artificielle (24h)", "Netflix n'envoie jamais ce type d'email"] },
    { "id": "mail_2", "from_name": "RH Entreprise", "from_address": "rh@votre-entreprise.fr", "subject": "Rappel : formulaire congés à compléter avant vendredi", "preview": "Bonjour, merci de compléter le formulaire de demande de congés...", "sent_at": "Hier 16:45", "is_dangerous": false, "body": "Bonjour,\n\nMerci de compléter le formulaire de demande de congés via notre portail RH habituel avant vendredi 17h.\n\nLien portail : https://rh.votre-entreprise.fr/conges\n\nCordialement,\nService RH", "red_flags": [] },
    { "id": "mail_3", "from_name": "Chronopost", "from_address": "suivi@chron0post-livraison.com", "subject": "Votre colis ne peut pas être livré — frais de douane 2,99€", "preview": "Un colis vous est destiné mais des frais de douane sont requis...", "sent_at": "Aujourd'hui 11:03", "is_dangerous": true, "body": "Bonjour,\n\nVotre colis n°FR847291038 ne peut pas être livré. Des frais de douane de 2,99 € sont requis.\n\nPayez maintenant : http://chron0post-livraison.com/payer\n\nSans paiement sous 48h, le colis sera retourné.\n\nChronopost", "red_flags": ["Domaine avec zéro à la place du o (chron0post)", "Demande de paiement par lien email", "Montant faible pour tromper la vigilance"] },
    { "id": "mail_4", "from_name": "Antoine Bernard", "from_address": "a.bernard@votre-entreprise.fr", "subject": "CR réunion projet Alpha — 03/04", "preview": "Bonjour à tous, voici le compte-rendu de notre réunion...", "sent_at": "Hier 18:20", "is_dangerous": false, "body": "Bonjour à tous,\n\nVeuillez trouver ci-joint le compte-rendu de notre réunion projet Alpha du 3 avril.\n\nProchaine réunion : mardi 10 avril à 10h en salle B.\n\nBonne soirée,\nAntoine", "red_flags": [] },
    { "id": "mail_5", "from_name": "Microsoft 365", "from_address": "security@microsoft-365-alert.net", "subject": "Connexion suspecte détectée sur votre compte", "preview": "Une connexion depuis la Russie a été détectée. Sécurisez votre compte...", "sent_at": "Aujourd'hui 08:47", "is_dangerous": true, "body": "Alerte de sécurité Microsoft 365\n\nUne connexion suspecte depuis Moscou, Russie a été détectée sur votre compte.\n\nSi ce n'est pas vous, sécurisez votre compte immédiatement :\n→ Cliquez ici pour vérifier\n\nSi vous ne réagissez pas dans l'heure, votre compte sera bloqué.\n\nMicrosoft Security Team", "red_flags": ["Domaine non officiel (microsoft-365-alert.net)", "Urgence extrême (1 heure)", "Microsoft utilise account.microsoft.com, jamais ce domaine"] },
    { "id": "mail_6", "from_name": "Julien Martin — Comptabilité", "from_address": "j.martin@votre-entreprise.fr", "subject": "Facture fournisseur à valider", "preview": "Bonjour, merci de valider la facture en pièce jointe avant fin de mois...", "sent_at": "Aujourd'hui 10:15", "is_dangerous": false, "body": "Bonjour,\n\nPouvez-vous valider la facture fournisseur Imprimerie Dupont (1 240 € HT) en pièce jointe ?\n\nElle doit être approuvée avant le 30 pour le paiement du mois.\n\nMerci,\nJulien Martin\nComptabilité", "red_flags": [] }
  ]
}
"""
        });

        // ── Challenge 3 — Décryptez le Phishing (multichoice) ────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C3Id,
            TenantId     = DemoTenantId,
            ModuleId     = DemoModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Phishing",
            Title        = "Décryptez le Phishing",
            Instructions = "Analysez cet email bancaire et identifiez tous les éléments qui prouvent que c'est un phishing.",
            Difficulty   = 2,
            Points       = 200,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Société Générale — Service Client",
    "from_address": "securite-client@societe-generale-verification.com",
    "to": "vous@email.fr",
    "subject": "🔒 Action requise : Vérification de sécurité de votre compte",
    "sent_at": "Aujourd'hui 10:23",
    "body": "Cher(e) client(e),\n\nNous avons détecté une activité inhabituelle sur votre compte bancaire. Pour des raisons de sécurité, nous avons temporairement limité l'accès à certaines fonctionnalités.\n\nPour rétablir votre accès complet, veuillez vérifier votre identité dans les 24 heures :\n\n→ http://sg-securite-verification.com/auth?token=8f72ka\n\nInformations requises :\n• Numéro de compte\n• Code confidentiel\n• Numéro de carte bleue\n\nSans action de votre part, votre compte sera suspendu.\n\nCordialement,\nService Sécurité Clients — Société Générale\n📞 01 42 14 75 00 | 🌐 www.societegenerale.fr"
  },
  "question": "Parmi ces éléments, lesquels prouvent que cet email est un phishing ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Le domaine expéditeur 'societe-generale-verification.com' n'est pas le vrai domaine de la Société Générale (societegenerale.fr)", "is_correct": true,  "explanation": "Le vrai domaine de la Société Générale est societegenerale.fr. Tout autre domaine est une usurpation d'identité." },
    { "id": "B", "label": "Le lien pointe vers 'sg-securite-verification.com', un domaine inconnu sans rapport avec la banque",                              "is_correct": true,  "explanation": "Les banques utilisent uniquement leurs domaines officiels. Un lien vers un domaine tiers est un signal d'alarme majeur." },
    { "id": "C", "label": "La Société Générale demande votre code confidentiel et numéro de carte par email",                                              "is_correct": true,  "explanation": "Aucune banque ne demande jamais votre code PIN, mot de passe ou numéro de carte par email. C'est une règle absolue." },
    { "id": "D", "label": "L'email mentionne le vrai site www.societegenerale.fr en bas, donc il est authentique",                                         "is_correct": false, "explanation": "Afficher le vrai site en pied de mail est une technique classique de phishing pour paraître légitime. Cela ne prouve rien." }
  ],
  "red_flags": [
    "Domaine expéditeur frauduleux (societe-generale-verification.com)",
    "URL du lien non officielle (sg-securite-verification.com)",
    "Demande de code confidentiel + numéro de carte — jamais par email",
    "Urgence artificielle (24h, suspension de compte)",
    "Emoji dans le sujet pour attirer l'attention",
    "Token de traçage dans l'URL (?token=8f72ka)"
  ]
}
"""
        });

        // ── Challenge 4 — La Fuite de Données RGPD (multichoice) ─────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C4Id,
            TenantId     = DemoTenantId,
            ModuleId     = DemoModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "RGPD",
            Title        = "La Fuite de Données RGPD",
            Instructions = "Un incident de sécurité vient de survenir. Identifiez les obligations légales que vous devez respecter selon le RGPD.",
            Difficulty   = 2,
            Points       = 175,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Incident de sécurité chez MedConnect",
    "context": "Vous êtes responsable informatique chez MedConnect, une startup qui gère des dossiers médicaux en ligne. Ce matin, vous découvrez qu'un fichier contenant les noms, dates de naissance, numéros de sécurité sociale et diagnostics de 8 400 patients a été exposé publiquement sur internet pendant 72 heures suite à une mauvaise configuration d'un serveur cloud.\n\nLe fichier est maintenant sécurisé. Que devez-vous faire ?",
    "icon": "alert"
  },
  "question": "Quelles actions êtes-vous légalement obligé d'entreprendre selon le RGPD ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Notifier la CNIL dans les 72 heures suivant la découverte de la violation",                                              "is_correct": true,  "explanation": "L'article 33 du RGPD impose une notification à l'autorité de contrôle (CNIL en France) dans les 72h. Au-delà, vous êtes en infraction et risquez une amende pouvant atteindre 10 millions d'euros ou 2% du CA mondial." },
    { "id": "B", "label": "Informer les 8 400 patients concernés de la fuite de leurs données médicales",                                          "is_correct": true,  "explanation": "L'article 34 du RGPD oblige à notifier les personnes concernées 'dans les meilleurs délais' quand la violation est susceptible d'engendrer un risque élevé — ce qui est clairement le cas avec des données médicales." },
    { "id": "C", "label": "Ne rien dire publiquement pour éviter la mauvaise publicité et gérer ça en interne",                                    "is_correct": false, "explanation": "C'est illégal et aggrave considérablement la situation. Dissimuler une violation RGPD est une infraction supplémentaire. Les amendes pour non-notification peuvent atteindre 10M€." },
    { "id": "D", "label": "Documenter la violation dans un registre interne avec la nature des données, les causes et les mesures correctives",   "is_correct": true,  "explanation": "L'article 33 al.5 impose de documenter toutes les violations, même celles non notifiées. Ce registre doit être tenu à disposition de la CNIL en cas de contrôle." }
  ],
  "red_flags": [
    "Données médicales = données sensibles (art. 9 RGPD) — protection renforcée obligatoire",
    "72h : délai légal de notification à la CNIL — non négociable",
    "8 400 personnes affectées = violation à fort impact",
    "Numéros de sécurité sociale exposés = risque d'usurpation d'identité",
    "La dissimulation est une infraction supplémentaire au RGPD",
    "Un DPO (Délégué à la Protection des Données) doit être impliqué"
  ],
  "savoir_plus": "Le RGPD distingue deux niveaux : violation à notifier à la CNIL uniquement (risque faible) et violation à notifier aussi aux personnes concernées (risque élevé). Les données médicales relèvent toujours du risque élevé."
}
"""
        });

        // ── Challenge 5 — Le Tournoi des Mots de Passe (password_quiz) ────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C5Id,
            TenantId     = DemoTenantId,
            ModuleId     = DemoModuleId,
            Type         = "interactive",
            ContentType  = "password_quiz",
            Category     = "Authentification",
            Title        = "Le Tournoi des Mots de Passe",
            Instructions = "3 questions sur les bonnes pratiques en matière de mots de passe. Une seule bonne réponse par question.",
            Difficulty   = 1,
            Points       = 125,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "intro": "Vous devez choisir un nouveau mot de passe pour votre compte professionnel. Votre entreprise impose : minimum 12 caractères, au moins une majuscule, un chiffre et un caractère spécial.",
  "rounds": [
    {
      "id": "round1",
      "question": "Lequel de ces mots de passe est le plus sécurisé ?",
      "choices": [
        { "id": "A", "label": "Password123!",       "is_correct": false, "explanation": "Trop prévisible. 'Password' est le mot le plus testé par les attaquants. La majuscule initiale et ! final sont des patterns connus." },
        { "id": "B", "label": "Jean1975Paris!",     "is_correct": false, "explanation": "Contient des informations personnelles facilement devinables (prénom, année de naissance, ville). Les attaques par dictionnaire ciblent ces patterns." },
        { "id": "C", "label": "Tr0ubad0ur$Lune#7", "is_correct": true,  "explanation": "Excellent ! Long, mélange de mots non liés, substitutions de caractères, symboles variés. Difficile à deviner et à craquer par force brute." },
        { "id": "D", "label": "123456789012",       "is_correct": false, "explanation": "Suite numérique = mot de passe le plus cracké au monde. Même long, il n'a aucune entropie." }
      ]
    },
    {
      "id": "round2",
      "question": "Quelle est la meilleure pratique pour gérer vos mots de passe professionnels ?",
      "choices": [
        { "id": "A", "label": "Utiliser le même mot de passe fort partout pour s'en souvenir facilement",                 "is_correct": false, "explanation": "Une seule fuite suffit à compromettre TOUS vos comptes. C'est le risque numéro 1 en entreprise." },
        { "id": "B", "label": "Les noter dans un fichier Excel protégé par un mot de passe sur votre bureau",             "is_correct": false, "explanation": "Un fichier sur le bureau est accessible à toute personne ayant accès à votre poste. Excel n'est pas un gestionnaire de mots de passe sécurisé." },
        { "id": "C", "label": "Utiliser un gestionnaire de mots de passe (Bitwarden, KeePass, 1Password)",               "is_correct": true,  "explanation": "Un gestionnaire génère et stocke des mots de passe uniques et complexes pour chaque service. C'est la méthode recommandée par l'ANSSI." },
        { "id": "D", "label": "Changer son mot de passe chaque semaine pour maximiser la sécurité",                       "is_correct": false, "explanation": "Les changements trop fréquents poussent les utilisateurs à choisir des mots de passe plus faibles. L'ANSSI recommande de changer uniquement en cas de compromission." }
      ]
    },
    {
      "id": "round3",
      "question": "Vous recevez un email vous demandant de réinitialiser votre mot de passe d'entreprise. Que faites-vous ?",
      "choices": [
        { "id": "A", "label": "Cliquer sur le lien dans l'email et entrer votre nouveau mot de passe",                                            "is_correct": false, "explanation": "Les liens dans les emails de phishing redirigent vers de faux portails. Ne jamais cliquer — aller directement sur le site officiel." },
        { "id": "B", "label": "Vérifier l'expéditeur, puis aller manuellement sur le portail RH officiel sans cliquer sur le lien",              "is_correct": true,  "explanation": "Bonne pratique ! Toujours accéder aux portails de sécurité en tapant l'URL directement dans le navigateur, jamais depuis un lien email." },
        { "id": "C", "label": "Transférer l'email à un collègue pour qu'il vérifie si c'est légitime",                                           "is_correct": false, "explanation": "Votre collègue pourrait cliquer sur le lien. Contacter directement votre service informatique par téléphone ou messagerie interne." },
        { "id": "D", "label": "Ignorer l'email, vous n'avez pas demandé de réinitialisation",                                                     "is_correct": true,  "explanation": "Si vous n'avez pas demandé de réinitialisation, ignorer ET signaler au service informatique — quelqu'un essaie peut-être de prendre le contrôle de votre compte." }
      ]
    }
  ]
}
"""
        });

        // ── C6 — Analyse de Situation : Que Feriez-Vous ? (free_text) ────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C6Id,
            TenantId     = DemoTenantId,
            ModuleId     = DemoModuleId,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Analyse Libre",
            Title        = "Analyse de Situation : Que Feriez-Vous ?",
            Instructions = "Trois situations concrètes à analyser. Rédigez votre réponse, l'IA évaluera votre raisonnement.",
            Difficulty   = 2,
            Points       = 150,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "questions": [
    {
      "id": "q1",
      "question": "Vous recevez un email de votre banque vous demandant de vérifier votre compte en cliquant sur un lien. L'email semble officiel mais l'URL du lien est 'ma-banque-securite.net'. Que faites-vous et pourquoi ?",
      "context": "phishing bancaire",
      "expected_elements": "Ne pas cliquer sur le lien. Vérifier l'URL officielle de la banque. Contacter directement la banque par téléphone. Signaler l'email comme phishing. Identifier le domaine suspect ma-banque-securite.net comme frauduleux car différent du domaine officiel.",
      "min_chars": 80,
      "hint": "Pensez à analyser l'URL et aux démarches à suivre..."
    },
    {
      "id": "q2",
      "question": "Votre collègue vous dit qu'il utilise le même mot de passe pour tous ses comptes professionnels depuis 5 ans et ne voit pas pourquoi changer. Quels arguments lui donneriez-vous pour le convaincre de changer ses pratiques ?",
      "context": "gestion des mots de passe",
      "expected_elements": "Risque de compromission en cascade si un seul compte est piraté. Recommandation d'un gestionnaire de mots de passe. Utiliser des mots de passe uniques et complexes. Activer la double authentification. Exemples de fuites de données réelles.",
      "min_chars": 80,
      "hint": "Pensez aux conséquences concrètes pour l'entreprise..."
    },
    {
      "id": "q3",
      "question": "Vous êtes en télétravail dans un café et vous devez accéder aux documents confidentiels de votre entreprise. Le café propose un WiFi gratuit 'CafeWifi_Free'. Décrivez les risques et comment vous connecter de façon sécurisée.",
      "context": "sécurité WiFi et télétravail",
      "expected_elements": "Risques du WiFi public : attaque man-in-the-middle, interception des données. Utiliser un VPN d'entreprise. Utiliser le partage de connexion du téléphone. Éviter les réseaux WiFi ouverts pour les données sensibles. Vérifier le certificat HTTPS des sites visités.",
      "min_chars": 80,
      "hint": "Quels risques sur un réseau WiFi public ? Comment les éviter ?"
    }
  ]
}
"""
        });
    }

    private static async Task UpsertAsync(AppDbContext db, Challenge c)
    {
        var existing = await db.Challenges.FindAsync(c.Id);
        if (existing is null)
        {
            db.Challenges.Add(c);
        }
        else
        {
            existing.Title        = c.Title;
            existing.Instructions = c.Instructions;
            existing.Category     = c.Category;
            existing.ContentType  = c.ContentType;
            existing.ContentJson  = c.ContentJson;
            existing.Points       = c.Points;
            existing.Difficulty   = c.Difficulty;
        }
        await db.SaveChangesAsync();
    }
}
