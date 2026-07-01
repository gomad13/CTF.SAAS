using CTF.Api.Models;

namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Catalogue — Parcours 04 : "Sensibilisation cyber — Les fondamentaux".
/// Niveau débutant, secteur cyber-général, 1 module, 8 challenges.
/// Scénarios universels : phishing M365, vishing IT, tailgating, USB, 2FA, mot de passe partagé, scareware, email RH.
/// </summary>
internal static class Parcours04_CyberFondamentaux
{
    private static readonly Guid PathId    = Guid.Parse("c0000004-0000-0000-0000-000000000000");
    private static readonly Guid ModuleId  = Guid.Parse("c0000004-0001-0000-0000-000000000000");

    private static readonly Guid C1Id = Guid.Parse("c0000004-0001-0001-0000-000000000000");
    private static readonly Guid C2Id = Guid.Parse("c0000004-0001-0002-0000-000000000000");
    private static readonly Guid C3Id = Guid.Parse("c0000004-0001-0003-0000-000000000000");
    private static readonly Guid C4Id = Guid.Parse("c0000004-0001-0004-0000-000000000000");
    private static readonly Guid C5Id = Guid.Parse("c0000004-0001-0005-0000-000000000000");
    private static readonly Guid C6Id = Guid.Parse("c0000004-0001-0006-0000-000000000000");
    private static readonly Guid C7Id = Guid.Parse("c0000004-0001-0007-0000-000000000000");
    private static readonly Guid C8Id = Guid.Parse("c0000004-0001-0008-0000-000000000000");

    public static async Task SeedAsync(AppDbContext db, DateTime now)
    {
        await CatalogSeedBase.UpsertPathAsync(db, new LearningPath
        {
            Id               = PathId,
            TenantId         = CatalogSeedBase.CatalogTenantId,
            Type             = "catalog",
            Title            = "Sensibilisation cyber — Les fondamentaux",
            Description      = "Un parcours d'initiation universelle : phishing, mots de passe, ingénierie sociale, télétravail sécurisé. Adapté à tous les secteurs et à tous les niveaux.",
            Level            = "beginner",
            Status           = "published",
            Version          = 1,
            IsCatalog        = true,
            Sector           = "cyber-general",
            EstimatedMinutes = 24,
            Tags             = "fondamentaux,phishing,mots-de-passe,ingenierie-sociale,teletravail,initiation",
            CreatedBy        = CatalogSeedBase.CatalogAuthorId,
            CreatedAt        = now,
            PublishedAt      = now
        });

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = ModuleId,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Les réflexes du quotidien",
            SortOrder = 1,
            CreatedAt = now
        });

        await SeedChallengesAsync(db, now);

        await CatalogSeedBase.EnsureDemoAccessAsync(db, PathId, now);
    }

    private static async Task SeedChallengesAsync(AppDbContext db, DateTime now)
    {
        // ── C1 — Boîte de réception : repérez les pièges (mailbox) ───────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C1Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "Votre boîte de réception du lundi matin",
            Instructions = "Vous venez d'arriver chez Lumenta Consulting à Saint-Auray. Ouvrez votre boîte mail et cochez uniquement les emails qui représentent un danger réel.",
            Difficulty   = 1,
            Points       = 100,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "emails": [
    {
      "id": "fond_mail_1",
      "from_name": "Microsoft 365",
      "from_address": "security@microsoft-365-login-alert.com",
      "subject": "Votre session Microsoft 365 expire dans 1 heure",
      "preview": "Votre mot de passe doit être revalidé pour éviter la déconnexion...",
      "sent_at": "Aujourd'hui 08h04",
      "is_dangerous": true,
      "body": "Bonjour,\n\nVotre session Microsoft 365 va expirer dans 1 heure.\n\nPour éviter la déconnexion et la perte d'accès à vos documents, merci de revalider votre mot de passe :\n\n→ http://microsoft-365-login-alert.com/reauth?u=lumenta\n\nSans action, votre compte sera bloqué.\n\nL'équipe Microsoft 365",
      "red_flags": [
        "Domaine expéditeur frauduleux (microsoft-365-login-alert.com ≠ microsoft.com / login.microsoftonline.com)",
        "Urgence artificielle (1 heure) pour provoquer une réaction impulsive",
        "Lien de revalidation vers un domaine tiers inconnu",
        "Microsoft n'envoie jamais ce type d'email depuis ce domaine",
        "Le mot de passe ne se 'revalide' jamais via un lien email"
      ]
    },
    {
      "id": "fond_mail_2",
      "from_name": "Alex Fontaine",
      "from_address": "alex.fontaine@lumenta.fr",
      "subject": "CR réunion projet Saint-Auray Phase 2",
      "preview": "Bonjour, voici le compte-rendu de notre réunion...",
      "sent_at": "Hier 17h20",
      "is_dangerous": false,
      "body": "Bonjour,\n\nPetit CR rapide de la réunion d'hier sur la Phase 2 du projet Saint-Auray :\n- validation des specs fonctionnelles,\n- point budget prévu vendredi à 10h,\n- livrable Figma mis à jour sur le portail interne.\n\nBonne journée,\nAlex",
      "red_flags": []
    },
    {
      "id": "fond_mail_3",
      "from_name": "DHL Express",
      "from_address": "tracking@dhl-livraison-express.net",
      "subject": "Colis en attente — frais de dédouanement 1,95€",
      "preview": "Votre colis est bloqué en douane. Réglez 1,95€...",
      "sent_at": "Aujourd'hui 07h48",
      "is_dangerous": true,
      "body": "Bonjour,\n\nVotre colis DHL réf. FR-882019 est actuellement bloqué en douane française.\n\nPour autoriser la livraison, merci de régler les frais de douane de 1,95 € :\n\n→ http://dhl-livraison-express.net/paiement\n\nSans action dans les 48h, le colis retournera à l'expéditeur.\n\nDHL Express",
      "red_flags": [
        "Domaine suspect (dhl-livraison-express.net ≠ dhl.com)",
        "Montant faible (1,95€) pour tromper la vigilance mais capturer la carte",
        "Demande de paiement par lien email",
        "DHL ne demande jamais de paiement par email avec ce type d'urgence",
        "Contexte opportuniste (on attend tous un colis un jour ou l'autre)"
      ]
    },
    {
      "id": "fond_mail_4",
      "from_name": "RH — Lumenta Consulting",
      "from_address": "rh@lumenta.fr",
      "subject": "Rappel : entretien annuel à planifier avant fin du mois",
      "preview": "Bonjour, merci de planifier votre entretien annuel...",
      "sent_at": "Hier 14h10",
      "is_dangerous": false,
      "body": "Bonjour,\n\nPetit rappel : merci de planifier votre entretien annuel avec votre manager avant la fin du mois via le portail RH habituel.\n\nLien portail : https://rh.lumenta.fr/entretien\n\nBonne journée,\nLe service RH",
      "red_flags": []
    },
    {
      "id": "fond_mail_5",
      "from_name": "Support IT Central",
      "from_address": "support.it@lumenta-assistance.fr",
      "subject": "Migration mailbox — action requise avant 12h",
      "preview": "Votre mailbox sera migrée ce midi, confirmez votre mot de passe...",
      "sent_at": "Aujourd'hui 09h02",
      "is_dangerous": true,
      "body": "Bonjour,\n\nDans le cadre de la migration technique de nos mailboxes vers le nouveau serveur, nous devons revalider votre mot de passe avant 12h aujourd'hui.\n\nMerci de cliquer sur le lien et de saisir votre identifiant et votre mot de passe actuels :\n\n→ http://lumenta-assistance.fr/migration\n\nSans action, votre mailbox sera inaccessible après 12h.\n\nCordialement,\nSupport IT Central",
      "red_flags": [
        "Domaine non officiel (lumenta-assistance.fr alors que l'IT interne est sur lumenta.fr)",
        "Demande explicite du mot de passe — jamais légitime, même en interne",
        "Urgence artificielle (avant 12h) typique du phishing",
        "Lien vers un domaine jamais annoncé par la DSI",
        "Se fait passer pour 'Support IT Central' (vrai nom de service = crédibilité volée)"
      ]
    },
    {
      "id": "fond_mail_6",
      "from_name": "Comité d'Entreprise",
      "from_address": "cse@lumenta.fr",
      "subject": "Billetterie printemps — ouverture des réservations",
      "preview": "Bonjour, l'ouverture des réservations est prévue vendredi...",
      "sent_at": "Hier 16h00",
      "is_dangerous": false,
      "body": "Bonjour à toutes et tous,\n\nL'ouverture des réservations de la billetterie printemps (cinéma, spectacles, parcs) est prévue vendredi à 14h sur l'espace CSE interne.\n\nÀ bientôt,\nLe CSE",
      "red_flags": []
    }
  ]
}
"""
        });

        // ── C2 — Vishing : faux support IT au téléphone (multichoice) ─────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C2Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Ingénierie Sociale",
            Title        = "Faux support IT au téléphone",
            Instructions = "Vous recevez un appel d'un prétendu technicien du 'Support IT Central' de Lumenta. Choisissez toutes les bonnes réactions.",
            Difficulty   = 1,
            Points       = 125,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Transcription d'appel",
    "from_address": "—",
    "to": "vous",
    "subject": "Appel téléphonique entrant — Support IT Central",
    "sent_at": "Aujourd'hui 10h47",
    "body": "Transcription :\n\n\"Bonjour, ici Kevin du Support IT Central de Lumenta. Nous avons détecté une infection sur votre poste depuis ce matin, on est en train de nettoyer tous les postes du site de Saint-Auray. Pour que je puisse intervenir, pouvez-vous me donner votre identifiant Windows et m'installer un petit logiciel d'accès à distance que je vais vous envoyer par mail ? Il faut faire vite, on a déjà 12 postes à traiter d'ici midi. Ah, et surtout, ne prévenez pas votre manager tout de suite, on fait ça en silencieux pour ne pas alarmer la direction.\""
  },
  "question": "Quelles sont les bonnes réactions face à cet appel ?",
  "choices": [
    { "id": "A", "label": "Donner l'identifiant et installer l'outil, Kevin semble bien connaître le contexte Lumenta",                                     "is_correct": false, "explanation": "Piège classique. L'identifiant ne se communique jamais par téléphone, et installer un outil d'accès à distance venu d'un appel non sollicité donne les clés de votre poste à un inconnu." },
    { "id": "B", "label": "Raccrocher, et rappeler l'IT Central via le numéro officiel affiché sur l'intranet pour vérifier",                               "is_correct": true,  "explanation": "Exactement la bonne réaction : toujours vérifier via un canal indépendant et connu (numéro intranet, ticket). Si l'appel était légitime, l'IT comprendra et apprécera la prudence." },
    { "id": "C", "label": "Signaler l'appel au Responsable Sécurité et à votre manager, même si 'Kevin' a demandé de ne rien dire",                        "is_correct": true,  "explanation": "La demande de discrétion est un signal d'alarme majeur : jamais un service IT légitime ne demande de contourner la hiérarchie. Le signalement permet de prévenir les autres collègues qui peuvent recevoir le même appel." },
    { "id": "D", "label": "Demander à 'Kevin' son matricule interne, son manager, et le numéro de ticket déjà ouvert sur votre poste",                      "is_correct": true,  "explanation": "Bonne pratique : un vrai technicien interne dispose d'un ticket et peut donner son manager. Une hésitation ou une improvisation sur ces éléments achève de confirmer que l'appel est frauduleux." },
    { "id": "E", "label": "Installer juste l'outil d'accès à distance mais ne pas donner l'identifiant",                                                     "is_correct": false, "explanation": "Installer l'outil suffit : il ouvre souvent lui-même une session ou une porte dérobée qui ne nécessite plus votre identifiant. Ne rien exécuter tant que la demande n'est pas confirmée par un canal officiel." }
  ],
  "red_flags": [
    "Appel non sollicité avec prétexte d'urgence",
    "Demande d'identifiant par téléphone — jamais légitime",
    "Demande d'installation d'un outil d'accès à distance depuis un email inconnu",
    "Injonction au secret vis-à-vis du manager",
    "Impossibilité de donner un numéro de ticket ou un matricule vérifiable",
    "Le prétendu service a un nom qui ressemble au vrai (Support IT Central)"
  ],
  "savoir_plus": "Le vishing (voice phishing) combine souvent avec un email : l'attaquant envoie un faux email 'préparatoire' puis rappelle par téléphone pour renforcer la crédibilité. Règle absolue : toujours vérifier par un canal différent avant d'agir."
}
"""
        });

        // ── C3 — Tailgating dans les bureaux (phishing_ai) ────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C3Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Sécurité Physique",
            Title        = "Tailgating à l'entrée des bureaux",
            Instructions = "Un inconnu tente d'entrer derrière vous au badge d'accès Lumenta. Expliquez pourquoi c'est un risque et comment vous réagiriez.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Situation à l'accueil Lumenta",
    "from_address": "—",
    "to": "vous",
    "subject": "Matin, entrée des bureaux de Saint-Auray",
    "sent_at": "Aujourd'hui 08h47",
    "body": "Scène :\n\nVous arrivez aux bureaux de Lumenta Consulting à Saint-Auray. Vous badgez à l'entrée principale.\n\nJuste derrière vous, un homme en costume, les bras chargés de cartons et d'un café, vous sourit : \"Pardon, j'ai oublié mon badge à la maison, je suis Paul, nouveau au marketing, je commence aujourd'hui. Pouvez-vous me tenir la porte ? Mon manager est déjà au 3e.\"\n\nIl a l'air pressé, crédible, sympathique. Derrière vous, trois collègues arrivent aussi et l'un d'eux lui lance : \"salut !\" sans vraiment le regarder."
  },
  "question": "Expliquez pourquoi laisser entrer 'Paul' est une faille de sécurité sérieuse pour Lumenta Consulting, et décrivez précisément comment vous réagiriez sans être désagréable.",
  "expected_elements": "Risques : vol de matériel, vol de documents, installation d'un keylogger ou d'un point d'accès Wi-Fi pirate, photographies d'écrans, reconnaissance du bâtiment pour une attaque ultérieure, accès aux espaces sensibles. Un 'nouveau' crédible joue sur la gêne sociale, les bras chargés, le sourire du collègue (tailgating + social proof). Bonne réaction : refuser poliment, l'orienter vers l'accueil, prévenir l'accueil ou la sécurité en interne, vérifier avec le manager cité. Ne pas accuser, juste appliquer la procédure. Signaler ensuite au responsable sécurité ou à la hiérarchie. Règle d'or : une personne = un badge. Pas d'exception même pour les costumes, les bras chargés ou les nouveaux.",
  "min_chars": 180,
  "hint": "Pensez : ce qu'il peut faire une fois à l'intérieur, et quelle attitude concrète vous adoptez sans être agressif."
}
"""
        });

        // ── C4 — Clé USB trouvée (multichoice) ────────────────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C4Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Sécurité Physique",
            Title        = "Clé USB trouvée sur le parking",
            Instructions = "Vous trouvez une clé USB étiquetée 'Salaires 2026' sur le parking de Lumenta. Que faites-vous ?",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 4,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Une clé USB 'Salaires 2026' sur le parking",
    "context": "Vous arrivez en retard. Sur le parking de Lumenta Consulting à Saint-Auray, vous remarquez une clé USB posée près de votre place, bien visible. Une étiquette manuscrite indique : 'Salaires 2026 — CONFIDENTIEL'.\n\nLa curiosité est forte. La clé semble avoir été oubliée par quelqu'un. Plusieurs idées vous traversent l'esprit : la brancher pour identifier le propriétaire, la donner à la DRH, la jeter, l'emporter chez vous pour vérifier tranquillement.",
    "icon": "alert"
  },
  "question": "Quelles sont les bonnes réactions dans cette situation ?",
  "choices": [
    { "id": "A", "label": "Brancher immédiatement la clé sur votre poste de travail pour voir à qui elle appartient",                                              "is_correct": false, "explanation": "Très dangereux. Les clés USB 'perdues' sont une technique d'attaque classique (USB drop). Elles peuvent contenir des malwares qui s'exécutent automatiquement ou se faire passer pour un clavier virtuel (BadUSB) et lancer des commandes." },
    { "id": "B", "label": "Remettre la clé à l'accueil ou au service Sécurité / IT sans la brancher",                                                               "is_correct": true,  "explanation": "Bonne réaction. L'IT peut l'analyser dans un environnement contrôlé (sandbox, poste d'analyse dédié) pour identifier le propriétaire sans risque. Ne jamais brancher sur un poste de production." },
    { "id": "C", "label": "Signaler la découverte à votre manager ou au Responsable Sécurité : l'étiquette 'Salaires' suggère un piège ciblé",                     "is_correct": true,  "explanation": "Pertinent. Une étiquette 'Salaires confidentiel' est un appât parfait pour déclencher la curiosité. Le signalement permet aussi de vérifier si d'autres clés similaires traînent sur le site (attaque coordonnée)." },
    { "id": "D", "label": "L'emporter chez vous pour la regarder tranquillement sur votre ordinateur personnel",                                                   "is_correct": false, "explanation": "Encore pire. On expose alors son poste personnel, qui sert souvent à se reconnecter à l'entreprise (télétravail, webmail). Une compromission personnelle peut rebondir sur Lumenta." },
    { "id": "E", "label": "La laisser là où elle est — quelqu'un reviendra la chercher",                                                                            "is_correct": false, "explanation": "Non optimal : un autre collègue risque de la brancher et de se faire piéger. La bonne approche est de la remettre à l'IT/sécurité." }
  ],
  "red_flags": [
    "Clé USB 'perdue' dans un lieu fréquenté = technique d'attaque documentée",
    "Étiquette aguicheuse ('Salaires', 'Confidentiel', 'RH') conçue pour déclencher la curiosité",
    "BadUSB : une clé peut simuler un clavier et exécuter des commandes invisibles",
    "Auto-exécution possible sur des environnements mal durcis",
    "Rebond possible depuis un poste personnel vers le poste pro"
  ],
  "savoir_plus": "L'ANSSI recommande de désactiver l'auto-run et de configurer les politiques USB en entreprise. Pour l'analyse d'une clé trouvée, utiliser un poste dédié (air gap) avec un système Linux live et visualiser les fichiers sans les ouvrir."
}
"""
        });

        // ── C5 — 2FA demandé au téléphone (ceo_fraud) ─────────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C5Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Ingénierie Sociale",
            Title        = "Le code 2FA qu'on vous demande au téléphone",
            Instructions = "Vous recevez un SMS avec un code à 6 chiffres. Quelques secondes après, le téléphone sonne. Choisissez la ou les bonnes réactions.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 5,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Appel téléphonique — prétendu Support IT",
    "from_address": "—",
    "to": "vous",
    "subject": "Code 2FA à communiquer ?",
    "sent_at": "Aujourd'hui 11h12",
    "body": "Vous venez de recevoir un SMS :\n\"Lumenta : votre code de connexion est 479281. Ne partagez jamais ce code.\"\n\n30 secondes plus tard, votre téléphone sonne. Voix calme au bout du fil :\n\"Bonjour, je suis Malik du Support IT Central de Lumenta. Nous testons actuellement le nouveau système d'authentification à deux facteurs suite à l'incident de la semaine dernière. Vous venez de recevoir un SMS avec un code à 6 chiffres. Pouvez-vous me le lire s'il vous plaît, c'est pour valider votre compte et éviter que votre session soit bloquée ce midi.\""
  },
  "choices": [
    { "id": "share",  "label": "Communiquer le code 479281, Malik en a besoin pour le test",                                          "icon": "bank",  "is_correct": false, "explanation": "Catastrophique. Un code 2FA ne se communique JAMAIS, même à l'IT interne. En le donnant, vous autorisez directement l'attaquant à se connecter sur votre compte — le SMS a été déclenché par sa tentative, pas par un test." },
    { "id": "hangup", "label": "Refuser, raccrocher, et rappeler l'IT Central via le numéro officiel de l'intranet",                   "icon": "phone", "is_correct": true,  "explanation": "Bonne pratique. Un service IT légitime n'a JAMAIS besoin de votre code 2FA. Raccrocher et vérifier via le canal officiel coupe immédiatement la tentative d'attaque." },
    { "id": "report", "label": "Signaler l'appel au RSSI et changer immédiatement le mot de passe du compte concerné",                "icon": "flag",  "is_correct": true,  "explanation": "Indispensable. Si l'attaquant demande le 2FA, c'est qu'il a déjà votre identifiant et votre mot de passe. Il faut changer le mot de passe, analyser comment il a pu fuiter (phishing, réutilisation, data breach) et activer les alertes de connexion." },
    { "id": "nothing","label": "Ne rien faire et reprendre son travail, le SMS finira par expirer",                                    "icon": "x",     "is_correct": false, "explanation": "Insuffisant. L'attaquant a vos identifiants, il recommencera. Sans signalement et changement de mot de passe, la compromission finira par aboutir." }
  ],
  "red_flags": [
    "Demande d'un code 2FA — jamais légitime, d'où qu'elle vienne",
    "SMS juste avant l'appel = signe que l'attaquant vient de tenter une connexion",
    "Prétexte de 'test' ou de 'validation' pour faire baisser la garde",
    "Appel non sollicité et inconnu qui connaît votre prénom / entreprise",
    "Le SMS Lumenta dit explicitement 'Ne partagez jamais ce code'",
    "L'attaquant connaît déjà votre identifiant + mot de passe, d'où l'urgence du reset"
  ]
}
"""
        });

        // ── C6 — Mot de passe partagé dans Teams (password_quiz) ──────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C6Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "password_quiz",
            Category     = "Authentification",
            Title        = "Mots de passe : les bonnes pratiques du quotidien",
            Instructions = "Trois mini-scénarios autour des mots de passe au bureau. Plusieurs réponses peuvent être correctes.",
            Difficulty   = 1,
            Points       = 125,
            SortOrder    = 6,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "intro": "Trois situations concrètes chez Lumenta Consulting. Identifiez la ou les bonnes pratiques à adopter.",
  "rounds": [
    {
      "id": "round1",
      "question": "Alex Fontaine vous écrit dans Teams : 'Tu peux me redonner le mot de passe du compte Canva de l'équipe, j'ai perdu mon post-it.' Que faites-vous ?",
      "choices": [
        { "id": "A", "label": "Lui répondre dans Teams : 'C'est Lumenta2026!' — c'est entre nous",                                  "is_correct": false, "explanation": "Partager un mot de passe en clair dans Teams est une très mauvaise pratique : les conversations sont stockées, indexées, parfois visibles par l'admin, et une compromission du compte d'Alex exposerait tout." },
        { "id": "B", "label": "Lui proposer de passer par un gestionnaire de mots de passe partagé (coffre équipe)",                   "is_correct": true,  "explanation": "Bonne pratique. Les gestionnaires de mots de passe (1Password, Bitwarden, Keeper...) permettent de partager des identifiants sans jamais exposer le mot de passe en clair, avec traçabilité." },
        { "id": "C", "label": "Lui dire qu'il faut solliciter l'IT pour réinitialiser le mot de passe équipe",                         "is_correct": true,  "explanation": "Correct aussi : l'IT peut gérer le reset et le partage via l'outil central. Cela force aussi à s'assurer que la demande vient bien du vrai Alex et non d'un compte compromis." },
        { "id": "D", "label": "Lui envoyer le mot de passe par SMS, c'est plus privé que Teams",                                       "is_correct": false, "explanation": "Faux. Les SMS ne sont pas chiffrés de bout en bout et peuvent être interceptés. De plus, le problème de fond reste : un mot de passe ne se partage jamais en clair." }
      ]
    },
    {
      "id": "round2",
      "question": "Vous devez choisir un mot de passe pour votre session Lumenta. Quelles sont les bonnes pratiques ?",
      "choices": [
        { "id": "A", "label": "Utiliser une phrase-passe longue de 4 à 5 mots non liés, avec symboles (ex. 'Givre!MangueHorloge_Saumon42')",    "is_correct": true,  "explanation": "Excellent. Les phrases-passes longues sont à la fois difficiles à casser par force brute et plus faciles à retenir que des suites aléatoires." },
        { "id": "B", "label": "Réutiliser le même mot de passe qu'ailleurs (Netflix, Amazon) pour simplifier la vie",                             "is_correct": false, "explanation": "Risque majeur : une fuite sur un service externe (credential stuffing) permet à l'attaquant de tester le même mot de passe sur votre compte Lumenta. C'est l'une des principales causes d'intrusion en entreprise." },
        { "id": "C", "label": "Utiliser un gestionnaire de mots de passe pour générer un mot de passe unique et complexe par service",            "is_correct": true,  "explanation": "Bonne pratique universelle. Un seul mot de passe maître à retenir, et le gestionnaire produit des secrets uniques pour chaque service." },
        { "id": "D", "label": "Noter le mot de passe sur un post-it collé à l'écran pour ne pas l'oublier",                                        "is_correct": false, "explanation": "Risque physique : visiteurs, collègues, femmes et hommes de ménage, visio-conférences... Tout le monde peut lire le post-it. Un gestionnaire est largement préférable." }
      ]
    },
    {
      "id": "round3",
      "question": "Lumenta impose désormais la 2FA. Quelles méthodes sont considérées comme les plus robustes ?",
      "choices": [
        { "id": "A", "label": "Application d'authentification (Microsoft Authenticator, Google Authenticator, Authy) avec code TOTP",           "is_correct": true,  "explanation": "Très bon niveau de sécurité : les codes sont générés localement et ne circulent pas sur le réseau. Beaucoup plus fiable que le SMS." },
        { "id": "B", "label": "Clé de sécurité physique FIDO2 (YubiKey, Titan Key)",                                                             "is_correct": true,  "explanation": "Le top de la 2FA. Résistant au phishing, à l'interception et au SIM swap. Recommandé pour les comptes admin et à privilèges." },
        { "id": "C", "label": "Réception d'un code par SMS sur votre téléphone personnel",                                                       "is_correct": false, "explanation": "Accepté mais le plus faible des 2FA : SIM swap, interception SS7, phishing vocal. À éviter si possible pour les comptes sensibles." },
        { "id": "D", "label": "Une seule question secrète comme 'le nom de votre premier animal'",                                                 "is_correct": false, "explanation": "Ce n'est pas du 2FA : c'est une donnée statique, souvent devinable à partir des réseaux sociaux. Ne pas confondre avec un facteur d'authentification." }
      ]
    }
  ]
}
"""
        });

        // ── C7 — Scareware (phishing_ai) ──────────────────────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C7Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Arnaque Web",
            Title        = "Pop-up alarmiste sur votre ordinateur",
            Instructions = "Un pop-up s'affiche en plein écran sur votre poste pendant votre navigation. Expliquez ce que c'est et ce que vous devez faire.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 7,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Pop-up écran",
    "from_address": "—",
    "to": "vous",
    "subject": "Fenêtre qui occupe tout l'écran en plein travail",
    "sent_at": "Aujourd'hui 15h31",
    "body": "Vous consultez un article pro sur un site d'actualités sectoriel à Saint-Auray. Soudain, votre écran passe en rouge. Une fenêtre occupe tout l'espace, impossible de la fermer avec la croix :\n\n\"⚠️ ATTENTION — 4 VIRUS CRITIQUES DÉTECTÉS SUR VOTRE ORDINATEUR ⚠️\nVos données bancaires, photos et mots de passe vont être volés dans 60 secondes.\nAppelez immédiatement le Support Microsoft certifié au +33 1 89 xx xx xx.\nNe redémarrez pas votre ordinateur, vous perdriez toutes vos données.\"\n\nUne voix synthétique crie la même chose. Un compte à rebours s'affiche."
  },
  "question": "Expliquez ce qu'est réellement ce message (quel type d'attaque, qui est derrière, quel est l'objectif), puis détaillez précisément ce que vous devez faire et ne surtout PAS faire sur votre poste Lumenta Consulting.",
  "expected_elements": "Il s'agit d'un scareware (ou tech support scam) : un faux message d'alerte diffusé via une publicité malveillante ou un site compromis. Objectif : pousser à appeler un faux support qui va demander d'installer un logiciel d'accès à distance, puis extorquer de l'argent ou voler des données bancaires. Microsoft n'affiche jamais de numéro de téléphone dans un pop-up. Ne PAS appeler le numéro, ne PAS installer d'outil, ne PAS communiquer ses identifiants. À faire : fermer l'onglet / le navigateur (Alt+F4, gestionnaire des tâches si bloqué), ne pas cliquer dans la fenêtre, signaler à l'IT / RSSI, scanner le poste, changer les mots de passe si des données ont pu être saisies. Pour les collègues : sensibiliser, car le scareware cible souvent les personnes moins à l'aise avec l'informatique. Ne pas redémarrer par panique.",
  "min_chars": 200,
  "hint": "Pensez : qui est derrière, l'objectif réel, comment fermer la fenêtre sans cliquer, à qui signaler."
}
"""
        });

        // ── C8 — Email RH 'licenciement' (free_text) ──────────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C8Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Analyse Libre",
            Title        = "L'email RH qui fait peur",
            Instructions = "Vous recevez trois situations déstabilisantes dans votre vie pro. Analysez chacune, l'IA évaluera votre raisonnement.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 8,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "questions": [
    {
      "id": "q1",
      "question": "Vous recevez un email intitulé 'Convocation à un entretien préalable de licenciement — à ouvrir immédiatement' de la part de 'rh-confidentiel@lumenta-hr-portal.com', avec une pièce jointe 'Convocation.docm'. Que faites-vous et pourquoi ?",
      "context": "phishing émotionnel",
      "expected_elements": "Ne surtout pas ouvrir la pièce jointe : extension .docm = macros Office, vecteur fréquent de malware (loader, ransomware). Le domaine lumenta-hr-portal.com n'est pas le domaine officiel de Lumenta (lumenta.fr) = phishing. Un vrai service RH ne convoque jamais par email 'confidentiel' anonyme, encore moins pour un licenciement. Le sujet est conçu pour provoquer la panique et court-circuiter la réflexion. Bonne réaction : vérifier l'expéditeur en entier, ne pas cliquer, ne pas répondre, signaler via le bouton de signalement phishing ou à l'IT/RSSI, appeler la RH directement sur le numéro connu pour lever le doute.",
      "min_chars": 140,
      "hint": "Pensez : extension du fichier, domaine, ton du message, canal officiel RH."
    },
    {
      "id": "q2",
      "question": "Vous êtes en télétravail depuis un café à Saint-Auray. Vous avez besoin d'accéder au CRM de Lumenta mais seul le Wi-Fi 'Cafe_Saint_Auray_FREE' est disponible. Quels risques et quelle est la bonne conduite à tenir ?",
      "context": "télétravail sécurisé",
      "expected_elements": "Risques du Wi-Fi public : interception de trafic (même HTTPS si l'utilisateur accepte des certificats), faux point d'accès (evil twin), vol de cookies de session, capture d'identifiants. Bonne pratique : utiliser le VPN d'entreprise avant toute connexion à un service Lumenta. À défaut, utiliser le partage de connexion 4G/5G de son téléphone plutôt que le Wi-Fi public. Vérifier systématiquement le cadenas HTTPS et ne jamais accepter un certificat invalide. Éviter les actions sensibles (saisie de mot de passe, documents confidentiels) sur un réseau non maîtrisé. Verrouiller son écran en toutes circonstances et masquer l'écran avec un filtre de confidentialité.",
      "min_chars": 160,
      "hint": "Pensez : VPN, partage de connexion, HTTPS, filtre de confidentialité."
    },
    {
      "id": "q3",
      "question": "Votre collègue Alex Fontaine vous dit : 'Je travaille mieux avec mes outils perso, je télécharge tous les documents clients Lumenta sur mon Google Drive personnel.' Quels arguments lui opposez-vous concrètement ?",
      "context": "Shadow IT et fuite de données",
      "expected_elements": "Risques : perte de contrôle sur les données de l'entreprise (confidentialité, propriété intellectuelle), violation probable du RGPD si données personnelles des clients (Lumenta n'a pas d'autorisation de traitement sur Google Drive perso d'Alex), risque en cas de départ d'Alex (les documents restent chez lui), compte Google perso bien moins protégé qu'un tenant d'entreprise, absence d'audit, risque de fuite en cas de compromission du compte perso. Arguments à donner : utiliser les outils officiels (OneDrive / SharePoint Lumenta), demander à l'IT une solution si l'outil actuel est pénible, respecter la charte informatique signée à l'embauche, protéger Alex lui-même qui engage sa responsabilité personnelle et peut être sanctionné. Proposer de remonter le besoin fonctionnel plutôt que de contourner.",
      "min_chars": 180,
      "hint": "Pensez : RGPD, propriété des données, départ du salarié, charte informatique, shadow IT."
    }
  ]
}
"""
        });
    }
}
