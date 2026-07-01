using CTF.Api.Models;

namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Parcours catalogue — Gestion des mots de passe et authentification.
/// 1 module, 7 challenges, niveau intermédiaire, secteur cyber-general.
/// </summary>
public static class Parcours06_MotsDePasse
{
    private static readonly Guid PathId   = Guid.Parse("c0000006-0000-0000-0000-000000000000");
    private static readonly Guid ModuleId = Guid.Parse("c0000006-0001-0000-0000-000000000000");

    private static readonly Guid C1Id = Guid.Parse("c0000006-0001-0001-0000-000000000000");
    private static readonly Guid C2Id = Guid.Parse("c0000006-0001-0002-0000-000000000000");
    private static readonly Guid C3Id = Guid.Parse("c0000006-0001-0003-0000-000000000000");
    private static readonly Guid C4Id = Guid.Parse("c0000006-0001-0004-0000-000000000000");
    private static readonly Guid C5Id = Guid.Parse("c0000006-0001-0005-0000-000000000000");
    private static readonly Guid C6Id = Guid.Parse("c0000006-0001-0006-0000-000000000000");
    private static readonly Guid C7Id = Guid.Parse("c0000006-0001-0007-0000-000000000000");

    public static async Task SeedAsync(AppDbContext db, DateTime now)
    {
        await CatalogSeedBase.UpsertPathAsync(db, new LearningPath
        {
            Id               = PathId,
            TenantId         = CatalogSeedBase.CatalogTenantId,
            Type             = "catalog",
            Title            = "Gestion des mots de passe et authentification",
            Description      = "Créer et gérer des mots de passe robustes, adopter un gestionnaire, activer le 2FA, identifier les attaques sur l'authentification (credential stuffing, MFA fatigue).",
            Level            = "intermediate",
            Status           = "published",
            Version          = 1,
            IsCatalog        = true,
            Sector           = "cyber-general",
            EstimatedMinutes = 24,
            Tags             = "mots-de-passe,2fa,mfa,gestionnaire,credential-stuffing,passkeys,intermediaire",
            CreatedBy        = CatalogSeedBase.CatalogAuthorId,
            CreatedAt        = now,
            PublishedAt      = now
        });

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = ModuleId,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Authentification forte & hygiène mot de passe",
            SortOrder = 1,
            CreatedAt = now
        });

        await SeedChallengesAsync(db, now);

        await CatalogSeedBase.EnsureDemoAccessAsync(db, PathId, now);
    }

    private static async Task SeedChallengesAsync(AppDbContext db, DateTime now)
    {
        // C1 — Password quiz : robustesse & gestionnaire
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C1Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "password_quiz",
            Category     = "Robustesse mot de passe",
            Title        = "Robustesse, entropie, phrase de passe",
            Instructions = "Bruno Castel, admin IT de Solveris, prépare une campagne interne sur les mots de passe. Trois questions à maîtriser avant de former les équipes.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "intro": "La politique Solveris impose 14 caractères minimum, 3 classes de caractères, et renouvellement uniquement en cas de compromission avérée (conforme recommandations ANSSI / NIST SP 800-63B).",
  "rounds": [
    {
      "id": "round1",
      "question": "Parmi ces quatre mots de passe, lequel est le plus robuste face à une attaque hors ligne par hashcat ?",
      "choices": [
        { "id": "A", "label": "Solveris2026!",                              "is_correct": false, "explanation": "Mauvais choix. Nom d'entreprise + année + symbole = pattern trivial. Les dictionnaires contextualisés le craquent en quelques secondes." },
        { "id": "B", "label": "P@ssw0rd!2026",                              "is_correct": false, "explanation": "Très mauvais. 'P@ssw0rd' est dans le top 100 des mots de passe les plus testés mondialement, substitutions comprises." },
        { "id": "C", "label": "cheval-batterie-agrafe-correct-7",           "is_correct": true,  "explanation": "Excellent. Phrase de passe (4+ mots aléatoires) = entropie élevée (~65 bits), facile à mémoriser, résistante aux attaques par dictionnaire. Conforme à la recommandation XKCD/NIST." },
        { "id": "D", "label": "Xkq7!m",                                     "is_correct": false, "explanation": "Trop court (6 caractères). Même avec tous les types de caractères, un brute-force hors ligne GPU le casse en moins de 2 heures." }
      ]
    },
    {
      "id": "round2",
      "question": "Un collaborateur refuse d'utiliser un gestionnaire de mots de passe en disant : 'si la base est piratée, tout est perdu, c'est plus risqué'. Quelle est la réponse correcte ?",
      "choices": [
        { "id": "A", "label": "Il a raison, mieux vaut mémoriser ses mots de passe ou les noter sur papier", "is_correct": false, "explanation": "Faux. Sans gestionnaire, les utilisateurs réutilisent le même mot de passe partout — risque infiniment plus grave qu'une base chiffrée." },
        { "id": "B", "label": "Un gestionnaire (Bitwarden, 1Password, KeePass) chiffre la base avec un mot de passe maître en AES-256 — seule une compromission de CE mot de passe maître expose les données, ce qui est gérable avec 2FA", "is_correct": true, "explanation": "Exact. La base est chiffrée localement. Même en cas de fuite de l'hébergement (ex : LastPass 2022), un mot de passe maître fort + 2FA rend la base inexploitable. Bénéfice net très supérieur au risque." },
        { "id": "C", "label": "Il suffit d'utiliser le même mot de passe fort partout, c'est aussi sécurisé qu'un gestionnaire", "is_correct": false, "explanation": "Faux. Une seule fuite (parmi les centaines par an) compromet tous les comptes. Le SSO n'est pas équivalent à la réutilisation." }
      ]
    },
    {
      "id": "round3",
      "question": "Concernant la fréquence de renouvellement des mots de passe en entreprise, quelle pratique est recommandée par l'ANSSI et le NIST depuis 2017 ?",
      "choices": [
        { "id": "A", "label": "Changer obligatoirement le mot de passe tous les 30 jours, même sans incident", "is_correct": false, "explanation": "Abandonné depuis 2017. Le changement forcé périodique pousse les utilisateurs vers des patterns faibles (Été2026!, Automne2026!)." },
        { "id": "B", "label": "Ne jamais changer un mot de passe fort tant qu'aucune compromission n'est avérée", "is_correct": true, "explanation": "Recommandation officielle ANSSI / NIST SP 800-63B. Un mot de passe fort et unique ne doit être changé qu'en cas de fuite avérée (notification HaveIBeenPwned, incident interne, alerte fournisseur)." },
        { "id": "C", "label": "Changer tous les 3 mois suffit, c'est le standard historique", "is_correct": false, "explanation": "Ancien standard PCI-DSS abandonné. Les versions récentes alignent sur NIST : renouvellement uniquement sur événement déclencheur." }
      ]
    }
  ]
}
"""
        });

        // C2 — Reset password phishing (mailbox)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C2Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Phishing d'identifiants",
            Title        = "Inbox Solveris : quels reset password sont piégés ?",
            Instructions = "Cinq emails de 'réinitialisation de mot de passe' arrivent chez des collaborateurs Solveris. Cochez uniquement ceux qui sont réellement malveillants.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "emails": [
    {
      "id": "mail_1",
      "from_name": "Microsoft 365 Security",
      "from_address": "security-alert@m365-solveris-reset.com",
      "subject": "Mot de passe expiré — action requise sous 2h",
      "preview": "Votre mot de passe Microsoft 365 a expiré. Réinitialisez immédiatement...",
      "sent_at": "Aujourd'hui 08:14",
      "is_dangerous": true,
      "body": "Cher utilisateur Solveris,\n\nVotre mot de passe Microsoft 365 a expiré. Pour éviter la suspension de votre compte, réinitialisez-le dans les 2 prochaines heures :\n\n→ https://m365-solveris-reset.com/login?user=b.castel\n\nAprès expiration, un ticket IT sera requis.\n\nMicrosoft 365 Security",
      "red_flags": [
        "Domaine m365-solveris-reset.com n'appartient ni à Microsoft ni à Solveris",
        "Urgence artificielle (2h)",
        "Microsoft utilise login.microsoftonline.com exclusivement",
        "Pré-remplissage du nom d'utilisateur pour feindre la personnalisation",
        "Aucune annonce préalable d'une politique d'expiration chez Solveris"
      ]
    },
    {
      "id": "mail_2",
      "from_name": "GitHub",
      "from_address": "noreply@github.com",
      "subject": "[GitHub] Sign in from new device",
      "preview": "A sign-in to your account was attempted from a new device in Paris, FR...",
      "sent_at": "Hier 22:40",
      "is_dangerous": false,
      "body": "Hey b-castel,\n\nWe noticed a sign-in to your account from a new device in Paris, FR using Firefox 124.\n\nIf this was you, you can safely ignore this email.\n\nIf this wasn't you, please review your account and reset your password: https://github.com/settings/security\n\nThanks,\nThe GitHub Team",
      "red_flags": []
    },
    {
      "id": "mail_3",
      "from_name": "Bitwarden",
      "from_address": "no-reply@bitwarden-account-secure.io",
      "subject": "Votre coffre-fort Bitwarden a été temporairement verrouillé",
      "preview": "Pour débloquer, confirmez votre mot de passe maître ici...",
      "sent_at": "Aujourd'hui 10:02",
      "is_dangerous": true,
      "body": "Bonjour,\n\nNotre système a détecté une activité inhabituelle. Votre coffre-fort Bitwarden a été temporairement verrouillé.\n\nConfirmez votre mot de passe maître pour rétablir l'accès :\n→ https://bitwarden-account-secure.io/unlock\n\nSupport Bitwarden",
      "red_flags": [
        "Bitwarden utilise bitwarden.com exclusivement",
        "Bitwarden ne demande JAMAIS le mot de passe maître par email (principe zero-knowledge)",
        "Verrouillage fictif pour créer la panique",
        "Objectif classique : capturer le master password qui ouvre TOUS les comptes"
      ]
    },
    {
      "id": "mail_4",
      "from_name": "Solveris IT Helpdesk",
      "from_address": "it@solveris.fr",
      "subject": "Maintenance planifiée Entra ID — 22/04 entre 22h et 00h",
      "preview": "Information : maintenance de l'annuaire. Aucune action requise...",
      "sent_at": "Hier 10:00",
      "is_dangerous": false,
      "body": "Bonjour,\n\nPour information : une maintenance planifiée de notre annuaire Entra ID (ex Azure AD) aura lieu mardi 22/04 entre 22h00 et 00h00.\n\nAucune action de votre part n'est requise. En cas d'indisponibilité au-delà de 00h30, ouvrez un ticket sur le portail habituel.\n\nBruno Castel\nSolveris IT",
      "red_flags": []
    },
    {
      "id": "mail_5",
      "from_name": "Okta",
      "from_address": "no-reply@okta-identity-check.net",
      "subject": "Validation MFA requise — nouvelle connexion détectée",
      "preview": "Approuvez cette connexion pour éviter le blocage de votre compte...",
      "sent_at": "Aujourd'hui 07:22",
      "is_dangerous": true,
      "body": "Nouvelle connexion à votre compte Okta.\n\nLieu : Singapour\nAppareil : Chrome 123 / Windows 11\n\nSi ce n'est pas vous, approuvez cette alerte pour la signaler :\n→ https://okta-identity-check.net/validate?token=8k2a\n\nOkta Security",
      "red_flags": [
        "Okta utilise okta.com, jamais okta-identity-check.net",
        "Paradoxe logique : on demande de cliquer pour 'signaler' (classique phishing)",
        "Géolocalisation exotique pour provoquer la peur",
        "Token dans l'URL pour tracker la victime"
      ]
    }
  ]
}
"""
        });

        // C3 — MFA fatigue attack (ceo_fraud-like interaction)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C3Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Attaque MFA fatigue",
            Title        = "MFA fatigue : 23 notifications en 4 minutes",
            Instructions = "Il est 23h47. Bruno, admin IT Solveris, reçoit sur son smartphone une avalanche de notifications push 'Approuver la connexion ?' pour son compte administrateur Entra ID. Quelle est la bonne réaction ?",
            Difficulty   = 3,
            Points       = 225,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Microsoft Authenticator",
    "from_address": "push-notification@microsoft.com",
    "to": "b.castel@solveris.fr",
    "subject": "23 demandes d'approbation reçues en 4 minutes",
    "sent_at": "Aujourd'hui 23:47",
    "body": "[Notification Push — Microsoft Authenticator]\n\nTentative de connexion à votre compte :\nb.castel@solveris.fr (Admin global)\n\nLieu : Francfort, Allemagne\nApplication : Microsoft Teams (Web)\n\n[ APPROUVER ]    [ REFUSER ]\n\n--- Rappel historique ---\n23:43 — Demande refusée\n23:43 — Demande refusée\n23:44 — Demande refusée\n23:44 — Demande refusée\n23:45 — Demande refusée\n... 18 notifications supplémentaires en 4 minutes ...\n23:47 — Nouvelle demande en cours"
  },
  "choices": [
    { "id": "approve",       "label": "Finir par approuver pour arrêter le spam de notifications",                                          "icon": "bank",  "is_correct": false, "explanation": "C'est exactement le but de l'attaquant. Cette attaque (MFA fatigue / MFA bombing) compte sur l'épuisement de la victime. Un seul clic 'Approuver' et l'attaquant a un compte admin." },
    { "id": "refuse_report", "label": "Refuser chaque demande, activer 'Number Matching' / Authenticator avec nombre, et appeler le RSSI",  "icon": "flag",  "is_correct": true,  "explanation": "Parfait. Le 'Number Matching' impose de saisir le chiffre affiché à l'écran de connexion, rendant l'approbation aveugle impossible. À défaut, refuser + alerter immédiatement." },
    { "id": "rotate_pwd",    "label": "Changer immédiatement le mot de passe du compte visé (depuis un autre device de confiance)",         "icon": "phone", "is_correct": true,  "explanation": "Bonne action complémentaire. Si l'attaquant a ton mot de passe (fuite, keylogger, stealer), la rotation casse la chaîne. Idéalement, forcer aussi la révocation de toutes les sessions actives via Entra ID." },
    { "id": "ignore",        "label": "Ignorer les notifications et aller dormir, ça finira par s'arrêter",                                  "icon": "x",     "is_correct": false, "explanation": "Mauvaise idée. L'attaquant a un mot de passe valide et continuera. Sans action (refus + rotation + signalement), le compte reste en risque actif." }
  ],
  "red_flags": [
    "Volume anormal de demandes push (23 en 4 min) = signature MFA fatigue",
    "Horaire inhabituel (23h47) pour une connexion légitime",
    "Géolocalisation étrangère (Francfort) sans déplacement déclaré",
    "Compte à privilèges (admin global) = cible prioritaire des attaquants",
    "L'attaquant possède déjà un mot de passe valide — il manque juste le 2FA",
    "Sans 'Number Matching', un clic accidentel suffit à céder l'accès"
  ]
}
"""
        });

        // C4 — Credential stuffing après fuite (multichoice)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C4Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Credential stuffing",
            Title        = "La fuite cloud.vertex-services.com : et après ?",
            Instructions = "Bruno Castel reçoit ce matin une alerte : le service tiers cloud.vertex-services.com, utilisé par 40 % des collaborateurs Solveris, a été piraté — 2,3 millions d'identifiants exposés. Choisissez toutes les actions correctes.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 4,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Fuite annoncée : cloud.vertex-services.com",
    "context": "Le 21 avril 2026, un dump de 2,3 millions d'identifiants (email + hash bcrypt + certains en clair) est mis en vente sur un forum du dark web. Parmi les comptes, 340 emails en @solveris.fr sont identifiés via HaveIBeenPwned. Bruno doit réagir en tant qu'admin IT."
  },
  "question": "Quelles actions Bruno doit-il entreprendre dans les 24 premières heures ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Forcer la rotation des mots de passe de tous les comptes Solveris concernés ET de tous les comptes SSO/Entra ID si réutilisation soupçonnée", "is_correct": true, "explanation": "Correct. Les attaquants lancent des attaques de credential stuffing (rejouer email+mot de passe sur des centaines d'autres services) dans les heures qui suivent une fuite. Bloquer vite = limiter la casse." },
    { "id": "B", "label": "Activer le blocage des tentatives de connexion avec mots de passe présents dans des breaches connus (politique 'Banned Password' Entra ID / équivalents)", "is_correct": true, "explanation": "Oui. Entra ID Password Protection et équivalents vérifient le mot de passe proposé contre des listes de fuites. Activation essentielle en cas d'incident externe." },
    { "id": "C", "label": "Ne rien faire : les hashs sont en bcrypt, donc il faut des années pour les casser", "is_correct": false, "explanation": "Faux et dangereux. Bcrypt ralentit mais ne bloque pas. Certains hashs cassent en heures si le mot de passe est faible. Et une partie du dump est en clair selon l'énoncé." },
    { "id": "D", "label": "Communiquer immédiatement à tous les collaborateurs : consignes de rotation, vérification sur HaveIBeenPwned, vigilance accrue sur phishing ciblé qui suivra", "is_correct": true, "explanation": "Oui. Après une fuite, une campagne de phishing ciblée arrive presque toujours dans les 72h (les attaquants exploitent la panique). Préparer les équipes = réduire drastiquement le taux de clic." },
    { "id": "E", "label": "Bloquer définitivement l'accès à cloud.vertex-services.com pour tous les collaborateurs Solveris", "is_correct": false, "explanation": "Réaction disproportionnée sans analyse. Le service reste peut-être nécessaire. La bonne approche : reset des mots de passe + imposition 2FA + audit du contrat fournisseur + éventuel plan de migration si la gouvernance sécurité du fournisseur est insuffisante." }
  ],
  "red_flags": [
    "340 emails @solveris.fr identifiés dans le dump = exposition massive",
    "Fenêtre de credential stuffing = 24-72h après fuite",
    "Risque de pivot via SSO si un mot de passe est réutilisé entre services",
    "Hashs bcrypt + certains en clair = mix particulièrement dangereux",
    "Phishing ciblé probable dans les 72h suivantes",
    "Obligation RGPD si données personnelles fuitées côté prestataire : analyse d'impact"
  ],
  "savoir_plus": "Outils de détection : HaveIBeenPwned (API gratuite), Microsoft Defender for Identity, les solutions SIEM + Threat Intel. Pour les entreprises, l'intégration automatique d'une liste de hashs leakés dans la politique de mot de passe (via API HIBP) est fortement recommandée. La rotation forcée est à coupler avec une obligation 2FA pour tous les comptes sensibles."
}
"""
        });

        // C5 — Passkeys vs SMS + SIM swapping (multichoice)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C5Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "2FA & passkeys",
            Title        = "SMS, TOTP, Passkey : hiérarchiser le 2FA",
            Instructions = "Bruno doit choisir la méthode 2FA à imposer chez Solveris. Identifiez les affirmations exactes sur les différentes options.",
            Difficulty   = 3,
            Points       = 175,
            SortOrder    = 5,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Mémo interne — Bruno Castel",
    "from_address": "b.castel@solveris.fr",
    "to": "codir@solveris.fr",
    "subject": "Choix de la méthode 2FA standard Solveris — arbitrage",
    "sent_at": "Aujourd'hui 15:30",
    "body": "CODIR,\n\nSuite à l'incident cloud.vertex-services.com et à l'augmentation des attaques de credential stuffing / SIM swapping, nous devons standardiser notre méthode de 2FA.\n\nTrois options sur la table :\n1) SMS (code reçu par message)\n2) TOTP (application type Google Authenticator / Authy)\n3) Passkeys FIDO2 (YubiKey, Windows Hello, Face ID)\n\nMerci d'arbitrer d'ici fin de semaine.\n\nBruno"
  },
  "question": "Parmi ces affirmations sur les méthodes 2FA, lesquelles sont exactes ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Le 2FA par SMS est vulnérable au SIM swapping : l'attaquant convainc l'opérateur de basculer le numéro sur sa propre carte SIM, puis intercepte tous les codes", "is_correct": true, "explanation": "Exact. Le SIM swapping est documenté massivement depuis 2019 (piratages de comptes Twitter, Coinbase, etc.). Le NIST a officiellement déconseillé le SMS comme second facteur dès 2017 (SP 800-63B)." },
    { "id": "B", "label": "Le TOTP (Authenticator) est plus sûr que le SMS car le code est généré localement, mais reste phishable : si l'utilisateur tape son code sur un faux site, l'attaquant peut le rejouer en temps réel", "is_correct": true, "explanation": "Correct. Les frameworks comme Evilginx2 réalisent des attaques AiTM (Adversary in the Middle) qui capturent à la fois le mot de passe ET le code TOTP + le cookie de session. TOTP > SMS mais reste vulnérable au phishing avancé." },
    { "id": "C", "label": "Les Passkeys (FIDO2 / WebAuthn) sont résistantes au phishing par design : la clé vérifie cryptographiquement le domaine demandeur, donc un faux site ne reçoit jamais la signature", "is_correct": true, "explanation": "Exact. C'est le gros avantage FIDO2. La signature est liée à l'origine (domaine + TLS). Même si l'utilisateur clique sur un lien de phishing, la clé refuse de répondre car le domaine ne correspond pas à celui enregistré." },
    { "id": "D", "label": "Le SMS est parfaitement sûr tant que le numéro est celui du collaborateur, car seul son téléphone peut le recevoir", "is_correct": false, "explanation": "Faux. Au-delà du SIM swapping, le protocole SS7 utilisé par les opérateurs mobiles est lui-même exploitable (interception sans intervention de l'opérateur)." },
    { "id": "E", "label": "Un gestionnaire de mots de passe remplace le 2FA, donc pas besoin d'activer MFA si on utilise un vault", "is_correct": false, "explanation": "Faux. Un gestionnaire protège les mots de passe mais ne remplace pas un second facteur. Si le mot de passe maître fuit sans 2FA sur le vault, tout est exposé. 2FA sur le vault + 2FA sur les services critiques sont complémentaires." }
  ],
  "red_flags": [
    "SMS : vulnérable au SIM swapping et à SS7",
    "TOTP : vulnérable aux attaques AiTM (Evilginx, Modlishka)",
    "SMS déconseillé officiellement par NIST SP 800-63B depuis 2017",
    "Passkeys FIDO2 = seule méthode résistante au phishing par construction",
    "Un 2FA mal choisi donne un faux sentiment de sécurité"
  ],
  "savoir_plus": "Ordre de préférence recommandé pour 2FA entreprise en 2026 : (1) Passkeys FIDO2 hardware (YubiKey) sur comptes admin et sensibles, (2) Passkeys plateforme (Windows Hello, Face ID) sur comptes utilisateurs, (3) TOTP en fallback, (4) SMS uniquement en dernier recours et pour des comptes non critiques. Microsoft et Google proposent désormais des modes 'passwordless only' qui reposent entièrement sur passkeys + appareil de confiance."
}
"""
        });

        // C6 — Reset password phishing + prétexte (phishing_ai)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C6Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Phishing d'identifiants",
            Title        = "Le reset qui vient juste après la fuite",
            Instructions = "Trois jours après l'annonce de la fuite cloud.vertex-services.com, un collaborateur Solveris reçoit l'email ci-dessous. Expliquez pourquoi ce timing est suspect et ce que le collaborateur doit faire.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 6,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Vertex Services — Sécurité",
    "from_address": "security@vertex-services-incident.com",
    "to": "a.morel@solveris.fr",
    "subject": "ACTION REQUISE — Réinitialisez votre mot de passe Vertex suite à l'incident du 21/04",
    "sent_at": "Aujourd'hui 09:17",
    "body": "Bonjour,\n\nSuite à l'incident de sécurité annoncé le 21 avril sur cloud.vertex-services.com, votre compte est concerné.\n\nPour sécuriser votre accès, nous vous demandons de réinitialiser votre mot de passe dans les 24 heures :\n\n→ https://vertex-services-incident.com/reset-urgence?token=a5X2k\n\nNotre équipe sécurité est mobilisée. Merci de votre coopération.\n\nCordialement,\nVertex Security Team"
  },
  "question": "Expliquez pourquoi cet email est particulièrement dangereux dans ce contexte, les techniques d'ingénierie sociale utilisées, et le protocole exact que le collaborateur doit suivre.",
  "expected_elements": "Timing du phishing juste après une fuite réelle = exploitation psychologique du stress ambiant, crédibilité accrue car fait référence à un événement public vérifiable. Domaine vertex-services-incident.com NON officiel (le vrai est vertex-services.com ou cloud.vertex-services.com). Urgence fabriquée (24h) pour empêcher la réflexion. Token dans l'URL = tracking + capture d'identifiants. Protocole : ne PAS cliquer sur le lien, aller manuellement sur le site officiel de Vertex en tapant l'URL dans le navigateur, si réinitialisation effectivement requise la faire depuis le portail officiel, vérifier sur le compte officiel Twitter/LinkedIn de Vertex les communications officielles, signaler l'email à l'IT Solveris (Bruno Castel) pour analyse + éventuelle alerte générale, vérifier sur HaveIBeenPwned si l'email est dans le dump. Règle générale : après une vraie fuite, les faux emails de remédiation arrivent massivement — ne jamais cliquer, toujours passer par le portail officiel.",
  "min_chars": 150,
  "hint": "Le vrai danger de ce mail n'est pas technique mais psychologique : il surfe sur un événement réel pour paraître légitime."
}
"""
        });

        // C7 — Politique entreprise & post-it (free_text)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C7Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = ModuleId,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Politique interne",
            Title        = "Rédiger la politique mot de passe Solveris",
            Instructions = "Bruno vous demande de rédiger trois sections de la politique mot de passe interne. Réponses concrètes, applicables immédiatement.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 7,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "questions": [
    {
      "id": "q1",
      "question": "Un collaborateur de Solveris garde ses mots de passe sur un post-it collé sous son clavier, et d'autres dans un fichier Excel nommé 'passwords.xlsx' sur son bureau. Expliquez les risques concrets et la solution à mettre en place immédiatement.",
      "context": "pratique à risque — mots de passe stockés en clair",
      "expected_elements": "Risques post-it : accès physique par toute personne passant par le poste (collègues, agents d'entretien, visiteurs, photos par téléphone). Risques Excel : accès en cas de vol d'ordinateur, de malware qui exfiltre des documents, de partage involontaire, Excel non chiffré en réalité. Solution : déploiement d'un gestionnaire d'entreprise (Bitwarden Business, 1Password Business) avec SSO + 2FA, formation utilisateur, import assisté des mots de passe existants, suppression du post-it et du fichier Excel après migration, suppression sécurisée avec outil (pas juste corbeille). Bénéfices : génération automatique de mots de passe uniques forts, audit centralisé, partage sécurisé des comptes techniques partagés.",
      "min_chars": 120,
      "hint": "Les risques physiques + numériques, puis la solution en 3 étapes concrètes."
    },
    {
      "id": "q2",
      "question": "Rédigez la politique officielle Solveris sur la longueur, la complexité et le renouvellement des mots de passe, en l'alignant sur les recommandations NIST SP 800-63B / ANSSI 2024.",
      "context": "politique interne conforme aux standards 2024-2026",
      "expected_elements": "Longueur minimale 14 caractères (16+ pour comptes admin). Utilisation d'une phrase de passe recommandée (4+ mots aléatoires). Pas de renouvellement périodique obligatoire (abandon de la règle 90 jours) — renouvellement uniquement sur événement déclencheur (fuite, suspicion, changement de rôle). Interdiction de réutilisation sur plusieurs services. Blocage automatique des mots de passe présents dans les listes de breaches connues (HaveIBeenPwned / Entra Password Protection). 2FA obligatoire, idéalement passkeys FIDO2. Interdiction d'écriture en clair (post-it, fichiers non chiffrés, emails). Gestionnaire d'entreprise imposé.",
      "min_chars": 120,
      "hint": "Longueur, complexité, renouvellement, 2FA, gestionnaire : 5 blocs clairs."
    },
    {
      "id": "q3",
      "question": "Un collaborateur Solveris reçoit une alerte HaveIBeenPwned indiquant que son email perso a été trouvé dans 3 fuites distinctes depuis 2023. Il vous demande si ça concerne son compte Solveris. Que lui répondez-vous et que lui conseillez-vous de faire ?",
      "context": "alerte HIBP sur email personnel",
      "expected_elements": "Risque de contamination professionnelle si réutilisation du même mot de passe entre perso et pro (à vérifier en priorité). Même sans réutilisation, un attaquant peut tenter ingénierie sociale en se basant sur les infos fuitées (email, noms, historique). Conseils : vérifier tous les services personnels affectés et y changer le mot de passe, vérifier la politique de réutilisation sur le compte Solveris, activer 2FA sur tous les comptes personnels sensibles, utiliser un gestionnaire personnel (Bitwarden gratuit en usage perso), surveiller les tentatives de phishing ciblé dans les semaines qui viennent, ne pas cliquer sur les liens d'emails même s'ils semblent pertinents. Côté entreprise : si doute de réutilisation, demander rotation forcée du mot de passe Solveris.",
      "min_chars": 120,
      "hint": "Séparer perso/pro, vérifier la réutilisation, 2FA partout, vigilance phishing ciblé."
    }
  ]
}
"""
        });
    }
}
