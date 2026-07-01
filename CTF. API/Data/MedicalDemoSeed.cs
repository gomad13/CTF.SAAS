using Microsoft.EntityFrameworkCore;
using CTF.Api.Models;

namespace CTF.Api.Data;

/// <summary>
/// Seed du parcours médical — 8 challenges de sensibilisation cybersécurité
/// spécifiques au secteur de la santé. Idempotent (upsert).
/// </summary>
public static class MedicalDemoSeed
{
    private static readonly Guid DemoTenantId   = Guid.Parse("00000000-0000-0000-0000-000000000000");
    private static readonly Guid AdminId        = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MedPathId      = Guid.Parse("20000000-0000-0000-0000-000000000000");
    private static readonly Guid MedModuleId    = Guid.Parse("20000000-0000-0000-0000-000000000099");
    private static readonly Guid C1Id           = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid C2Id           = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid C3Id           = Guid.Parse("20000000-0000-0000-0000-000000000003");
    private static readonly Guid C4Id           = Guid.Parse("20000000-0000-0000-0000-000000000004");
    private static readonly Guid C5Id           = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid C6Id           = Guid.Parse("20000000-0000-0000-0000-000000000006");
    private static readonly Guid C7Id           = Guid.Parse("20000000-0000-0000-0000-000000000007");
    private static readonly Guid C8Id           = Guid.Parse("20000000-0000-0000-0000-000000000008");
    private static readonly Guid C9Id           = Guid.Parse("20000000-0000-0000-0000-000000000009");

    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        await EnsurePathAsync(db, now);
        await EnsureModuleAsync(db, now);
        await UpsertChallengesAsync(db, now);
        await AssignToAllDemoUsersAsync(db, now);
    }

    private static async Task EnsurePathAsync(AppDbContext db, DateTime now)
    {
        var path = await db.Paths.FindAsync(MedPathId);
        if (path is null)
        {
            db.Paths.Add(new LearningPath
            {
                Id          = MedPathId,
                TenantId    = DemoTenantId,
                Type        = "demo",
                Title       = "Cybersécurité en Milieu Médical",
                Description = "Formation aux cybermenaces spécifiques au secteur de la santé. 8 modules basés sur des scénarios réels rencontrés dans les établissements médicaux.",
                Level       = "intermediate",
                Status      = "published",
                Version     = 1,
                CreatedBy   = AdminId,
                CreatedAt   = now,
                PublishedAt = now
            });
        }
        else
        {
            path.Title       = "Cybersécurité en Milieu Médical";
            path.Description = "Formation aux cybermenaces spécifiques au secteur de la santé. 8 modules basés sur des scénarios réels rencontrés dans les établissements médicaux.";
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureModuleAsync(AppDbContext db, DateTime now)
    {
        if (!await db.Modules.AnyAsync(m => m.Id == MedModuleId))
        {
            db.Modules.Add(new Module
            {
                Id        = MedModuleId,
                TenantId  = DemoTenantId,
                PathId    = MedPathId,
                Title     = "Menaces en milieu médical",
                SortOrder = 1,
                CreatedAt = now
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task UpsertChallengesAsync(AppDbContext db, DateTime now)
    {
        // ── C1 — Arnaque au Président Médical (ceo_fraud) ───────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C1Id,
            TenantId     = DemoTenantId,
            ModuleId     = MedModuleId,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Ingénierie Sociale",
            Title        = "L'Arnaque au Président Médical",
            Instructions = "Vous recevez un email urgent du directeur général de la clinique demandant un virement confidentiel pour un fournisseur d'équipements IRM. Analysez l'email et choisissez la ou les bonnes réactions.",
            Difficulty   = 1,
            Points       = 100,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Dr. Philippe Moreau — Directeur Général",
    "from_address": "p.moreau@clinique-saint-luc-direction.fr",
    "to": "comptabilite@clinique-saint-luc.fr",
    "subject": "URGENT ET CONFIDENTIEL — Virement fournisseur",
    "sent_at": "Aujourd'hui, 11h14",
    "body": "Bonjour,\n\nJe suis actuellement en réunion avec l'Agence Régionale de Santé et je ne peux pas être contacté par téléphone jusqu'à 17h.\n\nNous devons absolument régler aujourd'hui la facture de notre nouveau fournisseur d'équipements IRM. Suite à un incident technique, leur IBAN a changé.\n\nMerci d'effectuer un virement de 68 400 € vers :\nBénéficiaire : MedEquip Solutions GmbH\nIBAN : AT89 3200 0006 8765 4321\nBanque : EuroMed Bank\n\nCette opération est prioritaire et confidentielle — ne pas en informer le service achat pour l'instant, je vous expliquerai demain.\n\nMerci pour votre réactivité,\nDr. Philippe Moreau\nDirecteur Général — Clinique Saint-Luc"
  },
  "choices": [
    { "id": "pay", "label": "Effectuer le virement immédiatement, c'est le DG", "icon": "bank", "is_correct": false, "explanation": "C'est exactement le piège. Les arnaques au président ciblent particulièrement les établissements de santé. Ne jamais effectuer un virement exceptionnel sur simple email, même du DG." },
    { "id": "report", "label": "Alerter le responsable financier et le service achat", "icon": "flag", "is_correct": true, "explanation": "Parfait. La demande de confidentialité envers le service achat est un signal d'alarme majeur. Toujours impliquer les parties habituelles du processus de validation." },
    { "id": "callback", "label": "Rappeler le Dr. Moreau sur son numéro habituel du répertoire", "icon": "phone", "is_correct": true, "explanation": "Excellente réaction. Vérifier par un canal indépendant est la procédure correcte. Jamais via le numéro fourni dans l'email frauduleux." },
    { "id": "nothing", "label": "Attendre son retour demain pour confirmer", "icon": "clock", "is_correct": false, "explanation": "L'urgence est artificielle et fabriquée pour vous pousser à agir sans réfléchir. Mais ignorer sans alerter n'est pas suffisant — signalez immédiatement." }
  ],
  "red_flags": [
    "Adresse email suspecte (clinique-saint-luc-direction.fr ≠ domaine officiel)",
    "IBAN étranger (Autriche) pour un fournisseur français",
    "Demande de confidentialité envers les collègues habituels",
    "Indisponibilité téléphonique invoquée",
    "Urgence artificielle",
    "Changement d'IBAN non validé par le service achat"
  ]
}
"""
        });

        // ── C2 — Phishing Médical — Accès DMP (multichoice) ─────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C2Id,
            TenantId     = DemoTenantId,
            ModuleId     = MedModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Phishing",
            Title        = "Phishing Médical — Accès DMP",
            Instructions = "Vous recevez un email vous informant que votre accès au Dossier Médical Partagé expire. Identifiez les éléments prouvant qu'il s'agit d'un phishing.",
            Difficulty   = 1,
            Points       = 125,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Espace Santé — Assistance Technique",
    "from_address": "support@espace-sante-gouv-fr.net",
    "to": "dr.lambert@cabinet-lambert.fr",
    "subject": "⚕️ Votre accès au Dossier Médical Partagé expire dans 24h",
    "sent_at": "Aujourd'hui 08:45",
    "body": "Docteur,\n\nNous vous informons que votre accès professionnel au Dossier Médical Partagé (DMP) arrivera à expiration dans les prochaines 24 heures.\n\nPour maintenir l'accès aux dossiers de vos patients, vous devez renouveler votre authentification :\n\n→ Renouveler mon accès DMP\n   http://dmp-sante-professionnel.net/renouveler?token=med7829\n\nInformations requises lors du renouvellement :\n• Votre numéro RPPS\n• Votre carte CPS (numéro et code)\n• Vos identifiants Mon Espace Santé\n\nSans action, vos patients ne pourront plus partager leurs documents avec vous.\n\nService Support — Espace Santé\n📞 09 72 72 72 12 | esante.gouv.fr"
  },
  "question": "Quels éléments prouvent que cet email est une tentative de phishing ?",
  "choices": [
    { "id": "A", "label": "Le domaine expéditeur 'espace-sante-gouv-fr.net' imite le vrai domaine 'esante.gouv.fr' avec des tirets ajoutés", "is_correct": true, "explanation": "Le vrai domaine est esante.gouv.fr. Tout domaine qui l'imite avec des variations (tirets, .net, .com) est frauduleux. C'est la technique de typosquatting." },
    { "id": "B", "label": "Le lien pointe vers 'dmp-sante-professionnel.net' — domaine inconnu, pas esante.gouv.fr", "is_correct": true, "explanation": "L'URL de renouvellement doit toujours pointer vers esante.gouv.fr. Un domaine tiers signifie que vos identifiants seront volés." },
    { "id": "C", "label": "On vous demande votre numéro de carte CPS et son code — jamais demandé par email", "is_correct": true, "explanation": "La carte CPS ne doit JAMAIS être communiquée par email. C'est l'équivalent de votre signature professionnelle légale. Son vol permet d'accéder aux DMP de tous vos patients." },
    { "id": "D", "label": "L'email mentionne 'esante.gouv.fr' en bas, donc il provient bien de l'État", "is_correct": false, "explanation": "Afficher un lien officiel en pied de mail est une technique classique pour paraître légitime. Le domaine de l'expéditeur et du lien sont les seuls indicateurs fiables." }
  ],
  "red_flags": [
    "Domaine expéditeur frauduleux imitant esante.gouv.fr",
    "Lien vers domaine non officiel (dmp-sante-professionnel.net)",
    "Demande du code carte CPS — jamais par email",
    "Urgence artificielle (24h)",
    "Emoji médical pour paraître officiel",
    "Token de traçage dans l'URL"
  ]
}
"""
        });

        // ── C3 — Usurpation d'Identité Patient (multichoice) ────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C3Id,
            TenantId     = DemoTenantId,
            ModuleId     = MedModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Fraude",
            Title        = "Usurpation d'Identité Patient",
            Instructions = "Un patient se présente à l'accueil avec des documents qui semblent valides mais plusieurs détails vous interpellent. Identifiez les bonnes réactions.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 6,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Un patient se présente à l'accueil",
    "context": "Vous êtes à l'accueil d'un cabinet médical. Un homme d'une quarantaine d'années se présente pour un rendez-vous au nom de 'Martin Dubois, né le 12/03/1981'.\n\nIl présente une carte Vitale et une carte d'identité. Les informations correspondent au dossier patient.\nMais quelque chose vous interpelle : la photo de la carte d'identité semble légèrement différente du visage de la personne en face de vous, et la carte Vitale est très récente alors que le patient est enregistré depuis 10 ans dans le système.\n\nLe patient est pressé et insiste pour avoir son ordonnance habituelle de Tramadol (opioïde).",
    "icon": "alert"
  },
  "question": "Quelles sont les bonnes réactions face à cette situation ?",
  "choices": [
    { "id": "A", "label": "Demander un second document d'identité (passeport, permis de conduire) et comparer attentivement les photos", "is_correct": true, "explanation": "Correct. Un second document indépendant rend l'usurpation beaucoup plus difficile. Croiser plusieurs pièces est la procédure recommandée, surtout pour les prescriptions de médicaments sensibles." },
    { "id": "B", "label": "Accepter sans questionner — les documents correspondent au dossier", "is_correct": false, "explanation": "Les documents peuvent être falsifiés ou volés. Une carte Vitale neuve pour un patient ancien + une photo douteuse + une demande d'opioïdes = signaux d'alerte cumulés à ne pas ignorer." },
    { "id": "C", "label": "Informer le médecin des doutes avant la consultation, sans accuser le patient", "is_correct": true, "explanation": "Parfait. Le médecin doit être informé des anomalies constatées. Il pourra approfondir l'interrogatoire et prendre la décision adaptée sans mettre en cause directement le patient." },
    { "id": "D", "label": "Contacter la CPAM pour signaler une suspicion de fraude à la carte Vitale", "is_correct": true, "explanation": "En cas de doute avéré après vérification, signaler à la CPAM est la procédure officielle. La fraude à la carte Vitale est un délit et la CPAM dispose d'outils de détection." }
  ],
  "red_flags": [
    "Photo de la carte d'identité ne correspondant pas au visage",
    "Carte Vitale récente pour un patient ancien",
    "Demande insistante d'opioïdes (médicament à fort potentiel d'abus)",
    "Attitude pressée visant à éviter la vérification approfondie",
    "Combinaison de plusieurs signaux d'alerte simultanés"
  ]
}
"""
        });

        // ── C4 — Fraude à l'Assurance Maladie (multichoice) ─────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C4Id,
            TenantId     = DemoTenantId,
            ModuleId     = MedModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Fraude",
            Title        = "Fraude à l'Assurance Maladie",
            Instructions = "Un patient vous propose un arrangement financier impliquant la facturation de séances fictives. Identifiez la bonne conduite à tenir.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 7,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Demande suspecte de facturation",
    "context": "Vous gérez la facturation d'un cabinet de kinésithérapie. Un patient, M. Kowalski, vient régulièrement pour des séances remboursées par la CPAM.\n\nAujourd'hui il vous demande un service inhabituel : facturer 12 séances ce mois-ci alors qu'il n'en a effectué que 6, en échange d'un 'arrangement' financier. Il précise que 'tout le monde le fait' et que 'la Sécu ne vérifie jamais'.\n\nIl propose de partager avec vous le remboursement des 6 séances fictives.",
    "icon": "alert"
  },
  "question": "Quelle est la bonne conduite à tenir ?",
  "choices": [
    { "id": "A", "label": "Refuser catégoriquement — facturer des actes non réalisés est une fraude pénale", "is_correct": true, "explanation": "La facturation d'actes fictifs est une fraude à l'assurance maladie punie de 5 ans d'emprisonnement et 375 000€ d'amende. Le professionnel de santé est le premier responsable, pas le patient." },
    { "id": "B", "label": "Accepter discrètement — la CPAM ne contrôle pas chaque dossier", "is_correct": false, "explanation": "Faux et dangereux. La CPAM dispose d'algorithmes de détection des anomalies de facturation. Des pics inhabituels déclenchent automatiquement des contrôles. Les sanctions incluent la radiation de la convention." },
    { "id": "C", "label": "Signaler la tentative de fraude à la CPAM via le service fraude", "is_correct": true, "explanation": "Correct. Signaler protège le professionnel et permet à la CPAM d'identifier les patients qui tentent ce type de manipulation dans plusieurs cabinets. Le signalement est protégé." },
    { "id": "D", "label": "Conserver une trace écrite de la demande du patient (date, heure, contenu)", "is_correct": true, "explanation": "Documenter la tentative est une protection juridique essentielle. En cas de litige ultérieur, cette trace prouve que le professionnel a refusé et n'a pas participé à la fraude." }
  ],
  "red_flags": [
    "Proposition d'un arrangement financier illicite",
    "Minimisation du risque ('tout le monde le fait')",
    "Demande de facturation d'actes non réalisés",
    "Pression pour agir rapidement sans réfléchir",
    "La complicité engage la responsabilité pénale du professionnel"
  ]
}
"""
        });

        // ── C5 — Fuite de Données Patient — RGPD Médical (multichoice) ──────
        await UpsertAsync(db, new Challenge
        {
            Id           = C5Id,
            TenantId     = DemoTenantId,
            ModuleId     = MedModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "RGPD",
            Title        = "Fuite de Données Patient — RGPD Médical",
            Instructions = "Un serveur contenant 12 000 dossiers patients a été exposé publiquement pendant 3 jours. Identifiez vos obligations légales selon le RGPD.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 8,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Incident de sécurité au cabinet",
    "context": "Vous êtes responsable informatique d'une clinique de 45 médecins. Ce matin à 8h, une secrétaire signale qu'elle ne peut plus accéder aux dossiers patients. En vérifiant, vous découvrez que le serveur partagé contenant 12 000 dossiers patients (noms, diagnostics, traitements, numéros de sécu) a été accessible publiquement sur internet pendant 3 jours suite à une mauvaise configuration lors d'une mise à jour.\n\nLes données ne semblent pas avoir été téléchargées massivement, mais vous ne pouvez pas l'exclure.",
    "icon": "alert"
  },
  "question": "Quelles obligations légales devez-vous respecter selon le RGPD ?",
  "choices": [
    { "id": "A", "label": "Notifier la CNIL dans les 72 heures suivant la découverte de la violation", "is_correct": true, "explanation": "Article 33 du RGPD : toute violation de données personnelles doit être notifiée à la CNIL dans les 72h. Les données médicales sont des données sensibles (art. 9) — le délai est non négociable." },
    { "id": "B", "label": "Informer les 12 000 patients concernés de la violation de leurs données médicales", "is_correct": true, "explanation": "Article 34 du RGPD : quand la violation présente un risque élevé pour les droits des personnes (ce qui est le cas pour des données médicales), les personnes concernées doivent être notifiées sans délai." },
    { "id": "C", "label": "Gérer cela en interne discrètement pour protéger la réputation de la clinique", "is_correct": false, "explanation": "Dissimuler une violation RGPD est une infraction supplémentaire. L'amende peut atteindre 10M€ ou 2% du CA annuel. La CNIL est compétente pour sanctionner les établissements de santé." },
    { "id": "D", "label": "Documenter précisément la violation : nature des données, causes, mesures correctives", "is_correct": true, "explanation": "Article 33 al.5 : tout incident doit être consigné dans un registre des violations, même en l'absence de notification obligatoire. Ce registre est examiné par la CNIL en cas de contrôle." }
  ],
  "red_flags": [
    "Données médicales = catégorie spéciale art. 9 RGPD — protection maximale",
    "72h : délai légal de notification CNIL — non négociable",
    "12 000 patients affectés = incident à fort impact",
    "Numéros de sécurité sociale exposés = risque d'usurpation d'identité",
    "La dissimulation aggrave les sanctions",
    "Un DPO doit être nommé dans tout établissement de santé"
  ]
}
"""
        });

        // ── C6 — Ransomware Hospitalier (multichoice) ───────────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C6Id,
            TenantId     = DemoTenantId,
            ModuleId     = MedModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Ransomware",
            Title        = "Ransomware Hospitalier",
            Instructions = "Tous les postes de l'hôpital affichent un message de rançon. Les dossiers patients et le logiciel de planification des blocs sont inaccessibles. 3 opérations sont programmées dans 2 heures.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 9,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Alerte : vos fichiers sont chiffrés",
    "context": "Il est 7h du matin. Les soignants arrivant à l'hôpital découvrent que tous les postes affichent le même message :\n\n'Vos fichiers ont été chiffrés. Payez 80 000€ en Bitcoin sous 48h pour récupérer vos données. Après ce délai, le prix double. Contact : decrypt@protonmail.com'\n\nLes dossiers patients, les résultats d'examens et le logiciel de planification des blocs opératoires sont inaccessibles. 3 opérations sont programmées dans 2 heures.",
    "icon": "alert"
  },
  "question": "Quelles sont les actions prioritaires immédiates ?",
  "choices": [
    { "id": "A", "label": "Débrancher immédiatement tous les équipements du réseau (câbles ethernet, Wi-Fi) pour stopper la propagation", "is_correct": true, "explanation": "L'isolation réseau immédiate est la priorité absolue. Un ransomware se propage latéralement sur le réseau — chaque seconde compte pour limiter l'étendue du chiffrement." },
    { "id": "B", "label": "Payer la rançon rapidement pour récupérer les dossiers avant les opérations", "is_correct": false, "explanation": "Ne jamais payer. Rien ne garantit la récupération des données. Le paiement finance les criminels et en fait une cible récurrente. L'ANSSI et le gouvernement déconseillent formellement le paiement." },
    { "id": "C", "label": "Activer le Plan de Continuité d'Activité (PCA) : dossiers papier, procédures dégradées", "is_correct": true, "explanation": "Tout établissement de santé doit avoir un PCA cyber. Passer en mode dégradé (papier, communication verbale entre services) permet de maintenir les soins critiques pendant la gestion de la crise." },
    { "id": "D", "label": "Notifier immédiatement l'ANSSI, le CERT Santé et le procureur de la République", "is_correct": true, "explanation": "Obligatoire : signalement au CERT Santé (cybersécurité santé), à l'ANSSI pour les hôpitaux, et dépôt de plainte. Ces organismes peuvent fournir une assistance technique et légale immédiate." }
  ],
  "red_flags": [
    "Chiffrement simultané de tous les postes = propagation déjà avancée",
    "Délai court (48h) pour maximiser la pression",
    "Contact par email Protonmail = anonymat des criminels",
    "Les sauvegardes connectées au réseau sont souvent aussi chiffrées",
    "80% des hôpitaux victimes de ransomware avaient des sauvegardes insuffisantes"
  ]
}
"""
        });

        // ── C7 — Fausse Ordonnance Numérique (multichoice) ──────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C7Id,
            TenantId     = DemoTenantId,
            ModuleId     = MedModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Fraude",
            Title        = "Fausse Ordonnance Numérique",
            Instructions = "Un client présente une ordonnance numérique pour un médicament très contrôlé. Plusieurs détails sont suspects. Comment réagir ?",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 5,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Une ordonnance suspecte à la pharmacie",
    "context": "Vous êtes pharmacien. Un client présente une ordonnance numérique (QR code + PDF) pour 3 boîtes de Subutex (buprénorphine — traitement substitutif aux opioïdes, médicament très contrôlé).\n\nEn scannant le QR code, l'ordonnance semble valide. Mais en l'examinant attentivement :\n- Le numéro RPPS du médecin existe mais le Dr. Blanchard est cardiologue, pas addictologue\n- La date est un dimanche\n- La typographie du document est légèrement différente des ordonnances habituelles de ce médecin\n- Le patient est inconnu de votre officine",
    "icon": "alert"
  },
  "question": "Comment réagir face à cette ordonnance ?",
  "choices": [
    { "id": "A", "label": "Refuser la délivrance et conserver l'ordonnance — la spécialité du médecin est incompatible", "is_correct": true, "explanation": "Un cardiologue ne prescrit pas de traitement de substitution aux opioïdes. Cette incohérence de spécialité est un signal fort de falsification. Conserver le document est une procédure légale." },
    { "id": "B", "label": "Appeler le cabinet du Dr. Blanchard sur le numéro officiel (non celui de l'ordonnance) pour vérifier", "is_correct": true, "explanation": "La vérification directe auprès du prescripteur via un annuaire officiel est la meilleure protection. Ne jamais utiliser un numéro inscrit sur le document potentiellement falsifié." },
    { "id": "C", "label": "Délivrer le médicament — le QR code valide l'authenticité", "is_correct": false, "explanation": "Les QR codes peuvent être copiés depuis une vraie ordonnance et réutilisés. La validation technique ne suffit pas — l'analyse humaine des incohérences reste indispensable." },
    { "id": "D", "label": "Signaler la suspicion à l'Ordre des Pharmaciens et à l'Assurance Maladie", "is_correct": true, "explanation": "Le signalement permet de protéger les autres officines de la même zone géographique et d'identifier les réseaux de fraude aux médicaments contrôlés." }
  ],
  "red_flags": [
    "Spécialité du médecin incompatible avec le médicament prescrit",
    "Ordonnance datée un dimanche",
    "Typographie légèrement différente du standard habituel",
    "Patient inconnu de l'officine pour un médicament à fort potentiel d'abus",
    "Le QR code ne garantit pas l'authenticité si copié d'un vrai document"
  ]
}
"""
        });

        // ── C8 — Vishing Médical — Arnaque CPAM (password_quiz) ─────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C8Id,
            TenantId     = DemoTenantId,
            ModuleId     = MedModuleId,
            Type         = "interactive",
            ContentType  = "password_quiz",
            Category     = "Vishing",
            Title        = "Vishing Médical — Arnaque CPAM",
            Instructions = "Vous recevez un appel téléphonique d'une prétendue conseillère CPAM. Trois situations vont vous être présentées — choisissez la bonne réaction à chaque fois.",
            Difficulty   = 1,
            Points       = 125,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "intro": "Vous recevez un appel téléphonique. L'interlocuteur se présente comme 'Madame Lefebvre, conseillère CPAM' et vous dit avoir besoin de vérifier vos informations suite à une anomalie sur votre dossier. Trois situations vont vous être présentées — choisissez la bonne réaction à chaque fois.",
  "rounds": [
    {
      "id": "round1",
      "question": "Mme Lefebvre vous demande de confirmer votre numéro de sécurité sociale 'pour vérifier votre identité'. Que faites-vous ?",
      "choices": [
        { "id": "A", "label": "Donner votre numéro — c'est la CPAM, ils l'ont déjà", "is_correct": false, "explanation": "La CPAM possède déjà votre numéro de sécu et ne vous le demandera jamais par téléphone pour 'vérification'. Cette demande est un signal d'arnaque." },
        { "id": "B", "label": "Refuser et raccrocher — rappeler la CPAM sur le 3646 (numéro officiel)", "is_correct": true, "explanation": "Correct. Raccrocher et rappeler sur le numéro officiel (3646) vous permet de vérifier si l'appel était légitime. La vraie CPAM comprendra toujours cette prudence." },
        { "id": "C", "label": "Demander à l'interlocuteur de vous envoyer un courrier officiel", "is_correct": true, "explanation": "Bonne réaction. Un organisme officiel peut toujours formaliser sa demande par écrit. Un escroc refusera ou insistera pour régler ça 'maintenant par téléphone'." },
        { "id": "D", "label": "Donner les 7 premiers chiffres seulement pour 'prouver' votre identité", "is_correct": false, "explanation": "Même partiel, communiquer son numéro de sécu par téléphone à un inconnu est dangereux. Ces informations servent à construire une usurpation d'identité progressive." }
      ]
    },
    {
      "id": "round2",
      "question": "Elle vous annonce que vous avez droit à un remboursement de 87€ non perçu, et qu'elle a besoin de votre RIB pour virer la somme. Que faites-vous ?",
      "choices": [
        { "id": "A", "label": "Donner votre RIB — un remboursement de la CPAM c'est normal", "is_correct": false, "explanation": "La CPAM dispose déjà de votre RIB et vire directement sans vous le redemander. Une demande de RIB par téléphone pour un remboursement inattendu est une arnaque classique." },
        { "id": "B", "label": "Vérifier sur votre espace ameli.fr si un remboursement est effectivement prévu", "is_correct": true, "explanation": "Parfait. Ameli.fr affiche tous vos remboursements en cours. Si rien n'y figure, l'appel est frauduleux. Ne jamais agir sur la base d'un appel non sollicité." },
        { "id": "C", "label": "Accepter mais donner un RIB d'un compte vide", "is_correct": false, "explanation": "L'objectif n'est pas forcément de prélever votre compte — votre RIB combiné à d'autres infos peut servir à des fraudes plus élaborées. Refuser est la seule bonne réponse." },
        { "id": "D", "label": "Raccrocher et signaler l'appel sur signal-spam.fr ou cybermalveillance.gouv.fr", "is_correct": true, "explanation": "Signaler permet aux autorités de cartographier les campagnes de vishing médical et de protéger d'autres citoyens, notamment les personnes âgées plus vulnérables." }
      ]
    },
    {
      "id": "round3",
      "question": "Elle insiste, devient pressante et vous dit que sans votre coopération votre carte Vitale sera suspendue dès demain. Que faites-vous ?",
      "choices": [
        { "id": "A", "label": "Coopérer pour éviter la suspension — les soins sont trop importants", "is_correct": false, "explanation": "La CPAM ne suspend jamais une carte Vitale sans courrier officiel préalable. Cette menace urgente est le mécanisme de manipulation central du vishing — elle vise à court-circuiter votre réflexion." },
        { "id": "B", "label": "Raccrocher immédiatement — la menace confirme que c'est une arnaque", "is_correct": true, "explanation": "Exact. La pression et les menaces sont des signaux d'alarme absolus. Un organisme officiel ne menace jamais par téléphone. Raccrocher est la seule réaction appropriée." },
        { "id": "C", "label": "Demander son numéro de matricule et le nom de son responsable", "is_correct": false, "explanation": "L'escroc vous donnera des informations inventées. Engager la conversation prolonge l'exposition au risque de manipulation — mieux vaut raccrocher sans négocier." },
        { "id": "D", "label": "Prévenir un proche (conjoint, enfant) ou un collègue avant de rappeler la CPAM", "is_correct": true, "explanation": "Excellent réflexe. Un regard extérieur permet de désamorcer la manipulation émotionnelle. Les escrocs ciblent l'isolement — partager l'information brise leur stratégie." }
      ]
    }
  ]
}
"""
        });

        // ── C9 — La Messagerie Médicale Piégée (mailbox) ────────────────────
        await UpsertAsync(db, new Challenge
        {
            Id           = C9Id,
            TenantId     = DemoTenantId,
            ModuleId     = MedModuleId,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "La Messagerie Médicale Piégée",
            Instructions = "Voici votre messagerie professionnelle médicale. Cochez uniquement les emails que vous considérez dangereux, suspects ou non conformes.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 4,
            Status       = "published",
            CreatedBy    = AdminId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "instructions": "Voici votre messagerie professionnelle médicale. Cochez uniquement les emails que vous considérez dangereux, suspects ou non conformes.",
  "emails": [
    {
      "id": "med_mail_1",
      "from_name": "CPAM Île-de-France",
      "from_address": "noreply@cpam-idf-securite.net",
      "subject": "⚠️ Votre conventionnement menacé — action requise sous 48h",
      "preview": "Votre numéro RPPS présente une anomalie. Sans régularisation...",
      "sent_at": "Aujourd'hui 07h52",
      "is_dangerous": true,
      "body": "Madame, Monsieur,\n\nNous avons détecté une anomalie sur votre numéro RPPS. Votre conventionnement avec l'Assurance Maladie risque d'être suspendu sous 48 heures.\n\nRégularisez immédiatement votre situation :\n→ http://cpam-regularisation-rpps.net/verifier\n\nVous devrez fournir :\n• Votre numéro RPPS complet\n• Votre mot de passe Amelipro\n• Votre numéro de carte CPS\n\nService Conventionnement — CPAM Île-de-France",
      "red_flags": [
        "Domaine frauduleux (cpam-idf-securite.net ≠ ameli.fr)",
        "Lien vers domaine non officiel (cpam-regularisation-rpps.net)",
        "Demande du mot de passe Amelipro — jamais demandé par email",
        "Demande du numéro de carte CPS — jamais par email",
        "Urgence artificielle (48h, suspension de conventionnement)"
      ]
    },
    {
      "id": "med_mail_2",
      "from_name": "Dr. Sophie Renard",
      "from_address": "s.renard@chu-bordeaux.fr",
      "subject": "Transfert patient — M. Dupont Jean, 67 ans",
      "preview": "Bonjour, suite à notre échange téléphonique de ce matin...",
      "sent_at": "Hier 16h30",
      "is_dangerous": false,
      "body": "Bonjour,\n\nComme convenu ce matin par téléphone, je vous adresse le dossier de transfert de M. Jean Dupont, 67 ans, pour prise en charge cardiologique.\n\nBilan joint en pièce jointe via la messagerie sécurisée MSSanté.\n\nCordialement,\nDr. Sophie Renard\nService Cardiologie — CHU Bordeaux",
      "red_flags": []
    },
    {
      "id": "med_mail_3",
      "from_name": "MedEquip Solutions",
      "from_address": "facturation@medequip-solutions-fr.com",
      "subject": "Facture impayée — mise en demeure avant huissier",
      "preview": "Malgré nos relances, votre facture de 3 840€ reste impayée...",
      "sent_at": "Aujourd'hui 09h15",
      "is_dangerous": true,
      "body": "Madame, Monsieur,\n\nMalgré nos relances des 15 et 22 mars, votre facture n°2026-0847 d'un montant de 3 840 € TTC reste impayée.\n\nSans règlement sous 72h, nous transmettrons votre dossier à notre service contentieux et un huissier sera mandaté.\n\nPayez maintenant par carte : http://medequip-paiement-securise.xyz/facture\n\nService Recouvrement — MedEquip Solutions",
      "red_flags": [
        "Domaine de paiement suspect (.xyz = red flag)",
        "Menace d'huissier pour créer une pression émotionnelle",
        "Facture dont vous n'avez aucune trace préalable",
        "Délai très court (72h) pour un règlement financier important",
        "Aucun IBAN bancaire classique — paiement uniquement par lien"
      ]
    },
    {
      "id": "med_mail_4",
      "from_name": "Secrétariat — Cabinet Lamartine",
      "from_address": "secretariat@cabinet-lamartine.fr",
      "subject": "Rappel RDV — Mme Martin Isabelle — 14h30",
      "preview": "Bonjour, ce message confirme le rendez-vous de demain...",
      "sent_at": "Hier 17h45",
      "is_dangerous": false,
      "body": "Bonjour,\n\nCe message confirme le rendez-vous de Mme Isabelle Martin pour demain mardi à 14h30.\n\nEn cas d'empêchement, merci de nous prévenir au 01 42 36 85 10.\n\nCordialement,\nLe secrétariat du Cabinet Lamartine",
      "red_flags": []
    },
    {
      "id": "med_mail_5",
      "from_name": "Ordre National des Médecins",
      "from_address": "verification@ordre-medecins-inscription.fr",
      "subject": "Renouvellement inscription au Tableau — document requis",
      "preview": "Votre inscription au Tableau de l'Ordre expire le 30 avril...",
      "sent_at": "Aujourd'hui 08h20",
      "is_dangerous": true,
      "body": "Cher Confrère, Chère Consœur,\n\nVotre inscription au Tableau de l'Ordre National des Médecins expire le 30 avril 2026.\n\nPour renouveler votre inscription, connectez-vous via le lien ci-dessous et fournissez :\n• Scan de votre pièce d'identité\n• Scan de votre diplôme de Docteur en Médecine\n• Coordonnées bancaires (pour le règlement de la cotisation)\n\n→ http://ordre-renouvellement-medecin.com/connexion\n\nL'Ordre National des Médecins",
      "red_flags": [
        "Domaine frauduleux (ordre-medecins-inscription.fr ≠ conseil-national.medecin.fr)",
        "Le vrai domaine de l'Ordre est conseil-national.medecin.fr",
        "Demande de coordonnées bancaires par email — jamais légitime",
        "Demande de scan de diplôme par email — procédure non officielle",
        "Lien vers domaine .com non officiel"
      ]
    },
    {
      "id": "med_mail_6",
      "from_name": "Laboratoire BioAnalyse 75",
      "from_address": "resultats@bioanalyse75.fr",
      "subject": "Résultats disponibles — Patient Ref. 2026-4471",
      "preview": "Les résultats biologiques sont disponibles sur votre espace...",
      "sent_at": "Aujourd'hui 11h00",
      "is_dangerous": false,
      "body": "Bonjour,\n\nLes résultats biologiques du patient Réf. 2026-4471 sont disponibles sur votre espace médecin sécurisé MSSanté.\n\nConnectez-vous sur votre messagerie MSSanté habituelle pour les consulter.\n\nCordialement,\nLaboratoire BioAnalyse Paris 75\n📞 01 45 67 89 12",
      "red_flags": []
    },
    {
      "id": "med_mail_7",
      "from_name": "Microsoft 365 Santé",
      "from_address": "security-alert@microsoft365-sante-pro.com",
      "subject": "Connexion suspecte sur votre messagerie professionnelle",
      "preview": "Une tentative de connexion depuis Kiev (Ukraine) a été détectée...",
      "sent_at": "Aujourd'hui 06h33",
      "is_dangerous": true,
      "body": "Alerte de sécurité — Microsoft 365\n\nUne connexion suspecte depuis Kiev, Ukraine a été détectée sur votre compte professionnel de messagerie médicale.\n\nSi ce n'est pas vous, sécurisez votre compte immédiatement :\n→ http://ms365-sante-verification.com/secure\n\nSans action dans l'heure, votre accès sera bloqué et vos données médicales inaccessibles.\n\nMicrosoft Security Team",
      "red_flags": [
        "Domaine non officiel (microsoft365-sante-pro.com ≠ microsoft.com)",
        "Lien vers domaine tiers (ms365-sante-verification.com)",
        "Urgence extrême (1 heure) pour provoquer une action irréfléchie",
        "Mention des données médicales pour amplifier la peur",
        "Microsoft ne communique pas via ce type de domaine"
      ]
    },
    {
      "id": "med_mail_8",
      "from_name": "Formation Continue — FMC Pro",
      "from_address": "inscription@fmc-pro-sante.fr",
      "subject": "Votre DPC 2026 — places limitées",
      "preview": "Rappel : il vous reste 12 heures de DPC à valider avant le 31 décembre...",
      "sent_at": "Hier 14h12",
      "is_dangerous": false,
      "body": "Bonjour,\n\nRappel : il vous reste des heures de Développement Professionnel Continu (DPC) à valider avant le 31 décembre 2026.\n\nConsultez notre catalogue de formations éligibles DPC sur notre site.\n\nInscription et informations : www.fmc-pro-sante.fr\n\nCordialement,\nL'équipe FMC Pro Santé",
      "red_flags": []
    },
    {
      "id": "med_mail_9",
      "from_name": "Agence Régionale de Santé",
      "from_address": "contact-ars@agence-sante-urgence.fr",
      "subject": "URGENT — Signalement obligatoire épidémie locale",
      "preview": "Dans le cadre d'une alerte sanitaire, vous êtes tenu de déclarer...",
      "sent_at": "Aujourd'hui 10h05",
      "is_dangerous": true,
      "body": "Madame, Monsieur,\n\nDans le cadre d'une alerte sanitaire locale, vous êtes tenu de remplir immédiatement le formulaire de signalement obligatoire.\n\nCe formulaire requiert :\n• Votre identifiant Amelipro\n• La liste de vos patients à risque avec leurs coordonnées\n• Votre numéro RPPS\n\n→ Accéder au formulaire : http://ars-signalement-urgence.fr/formulaire\n\nToute absence de réponse dans les 2 heures sera signalée à votre Ordre.\n\nL'Agence Régionale de Santé",
      "red_flags": [
        "Domaine frauduleux (agence-sante-urgence.fr ≠ ars.sante.fr)",
        "Demande de liste de patients avec coordonnées — violation RGPD grave",
        "Demande des identifiants Amelipro par email — jamais légitime",
        "Délai 2 heures et menace de l'Ordre = manipulation émotionnelle",
        "L'ARS communique via ars.sante.fr uniquement",
        "Divulguer des données patients = infraction pénale"
      ]
    },
    {
      "id": "med_mail_10",
      "from_name": "Pharmacie Centrale — Dr. Vidal",
      "from_address": "vidal.pharmacie@wanadoo.fr",
      "subject": "Question sur ordonnance patient M. Roux",
      "preview": "Bonjour docteur, je me permets de vous contacter...",
      "sent_at": "Hier 09h45",
      "is_dangerous": false,
      "body": "Bonjour Docteur,\n\nJe me permets de vous contacter au sujet d'une ordonnance de M. Bernard Roux présentée ce matin.\n\nLa posologie du Metformine indiquée me semble inhabituelle — pourriez-vous confirmer qu'il s'agit bien de 1000mg 2x/jour ?\n\nMerci d'avance,\nDr. Amina Vidal\nPharmacienne — Pharmacie Centrale",
      "red_flags": []
    }
  ]
}
"""
        });
    }

    private static async Task AssignToAllDemoUsersAsync(AppDbContext db, DateTime now)
    {
        var demoUsers = await db.Users
            .Where(u => u.TenantId == DemoTenantId)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in demoUsers)
        {
            var exists = await db.Assignments
                .AnyAsync(a => a.UserId == userId && a.PathId == MedPathId);
            if (!exists)
            {
                db.Assignments.Add(new Assignment
                {
                    Id         = Guid.NewGuid(),
                    TenantId   = DemoTenantId,
                    UserId     = userId,
                    PathId     = MedPathId,
                    Status     = Assignment.Statuses.Assigned,
                    AssignedBy = AdminId,
                    AssignedAt = now,
                    UpdatedAt  = now
                });
            }
        }
        await db.SaveChangesAsync();
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
            existing.SortOrder    = c.SortOrder;
        }
        await db.SaveChangesAsync();
    }
}
