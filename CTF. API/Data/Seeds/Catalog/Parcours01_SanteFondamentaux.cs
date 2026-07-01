using CTF.Api.Models;

namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Parcours catalogue — Secteur Santé, niveau initiation.
/// "Cybersécurité pour les professionnels de santé" : 1 module, 7 challenges progressifs
/// (mailbox, multichoice, phishing_ai, ceo_fraud, password_quiz, free_text).
/// </summary>
public static class Parcours01_SanteFondamentaux
{
    private static readonly Guid PathId    = Guid.Parse("c0000001-0000-0000-0000-000000000000");
    private static readonly Guid Module1Id = Guid.Parse("c0000001-0001-0000-0000-000000000000");

    private static readonly Guid C1Id = Guid.Parse("c0000001-0001-0001-0000-000000000000");
    private static readonly Guid C2Id = Guid.Parse("c0000001-0001-0002-0000-000000000000");
    private static readonly Guid C3Id = Guid.Parse("c0000001-0001-0003-0000-000000000000");
    private static readonly Guid C4Id = Guid.Parse("c0000001-0001-0004-0000-000000000000");
    private static readonly Guid C5Id = Guid.Parse("c0000001-0001-0005-0000-000000000000");
    private static readonly Guid C6Id = Guid.Parse("c0000001-0001-0006-0000-000000000000");
    private static readonly Guid C7Id = Guid.Parse("c0000001-0001-0007-0000-000000000000");

    public static async Task SeedAsync(AppDbContext db, DateTime now)
    {
        var path = new LearningPath
        {
            Id               = PathId,
            TenantId         = CatalogSeedBase.CatalogTenantId,
            Type             = "catalog",
            Title            = "Cybersécurité pour les professionnels de santé",
            Description      = "Les fondamentaux de la cybersécurité adaptés aux équipes médicales et paramédicales : email, mots de passe, protection des données patients au quotidien.",
            Level            = "beginner",
            Status           = "published",
            Version          = 1,
            IsCatalog        = true,
            Sector           = "sante",
            EstimatedMinutes = 24,
            Tags             = "sante,phishing,mots-de-passe,rgpd,initiation",
            CreatedBy        = CatalogSeedBase.CatalogAuthorId,
            CreatedAt        = now,
            PublishedAt      = now
        };
        await CatalogSeedBase.UpsertPathAsync(db, path);

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = Module1Id,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Réflexes cyber en cabinet et en établissement",
            SortOrder = 1,
            CreatedAt = now
        });

        // ── C1 — Tri de la boîte mail du cabinet (mailbox) ─────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C1Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "La boîte mail du cabinet du Dr Lefèvre",
            Instructions = "Vous êtes secrétaire médicale au cabinet du Dr Lefèvre. Cochez uniquement les emails que vous considérez comme dangereux ou suspects avant de les traiter.",
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
    { "id": "mail_1", "from_name": "Doctolib Sécurité", "from_address": "securite@doctolib-secu.fr", "subject": "Vérification urgente de votre compte praticien", "preview": "Votre compte Doctolib sera suspendu sous 24h si vous ne vérifiez pas...", "sent_at": "Aujourd'hui 08:41", "is_dangerous": true, "body": "Cher praticien,\n\nNous avons détecté une activité inhabituelle sur votre compte Doctolib. Pour éviter la suspension de votre agenda sous 24h, merci de vérifier votre identité :\n\n→ http://doctolib-secu.fr/verif-praticien\n\nMerci de votre réactivité.\n\nÉquipe Sécurité Doctolib", "red_flags": ["Domaine 'doctolib-secu.fr' au lieu du domaine officiel doctolib.fr", "Menace de suspension sous 24h (urgence artificielle)", "Lien hors domaine officiel", "Doctolib ne demande jamais de vérification par email externe"] },
    { "id": "mail_2", "from_name": "CPAM — Espace Pro", "from_address": "no-reply@ameli-pro-maj.fr", "subject": "Mise à jour obligatoire de votre carte CPS", "preview": "La mise à jour annuelle de votre carte CPS doit être effectuée avant le 30...", "sent_at": "Aujourd'hui 10:02", "is_dangerous": true, "body": "Bonjour Docteur,\n\nLa mise à jour annuelle de votre Carte de Professionnel de Santé (CPS) doit être réalisée avant le 30 de ce mois.\n\nCliquez ici pour synchroniser votre carte : http://ameli-pro-maj.fr/cps-sync\n\nL'équipe Ameli Pro", "red_flags": ["Domaine 'ameli-pro-maj.fr' (le vrai domaine est ameli.fr / amelipro.ameli.fr)", "La CPS se met à jour via le lecteur physique, jamais par un lien web", "Demande d'action urgente sur un outil métier critique"] },
    { "id": "mail_3", "from_name": "Laboratoire BioNordik", "from_address": "resultats@bionordik.fr", "subject": "Résultats d'analyses — Patient M. Perrin (dossier 48291)", "preview": "Bonjour Docteur, veuillez trouver en pièce jointe les résultats de M. Perrin...", "sent_at": "Hier 17:22", "is_dangerous": false, "body": "Bonjour Docteur Lefèvre,\n\nVeuillez trouver en pièce jointe (via notre portail sécurisé habituel) les résultats d'analyses de M. Perrin, dossier 48291.\n\nLien portail : https://portail.bionordik.fr/resultats/48291\n\nBonne journée,\nLaboratoire BioNordik", "red_flags": [] },
    { "id": "mail_4", "from_name": "Conseil National de l'Ordre", "from_address": "cotisation@ordre-medecins-france.net", "subject": "Rappel cotisation ordinale — dernier avertissement", "preview": "Votre cotisation 2026 est impayée. Régularisez avant radiation...", "sent_at": "Aujourd'hui 09:18", "is_dangerous": true, "body": "Docteur,\n\nVotre cotisation ordinale 2026 n'a pas été reçue. En l'absence de régularisation sous 72h, nous serons contraints d'engager une procédure de radiation temporaire.\n\nRégularisez immédiatement : http://ordre-medecins-france.net/pay\n\nConseil National de l'Ordre des Médecins", "red_flags": ["Domaine 'ordre-medecins-france.net' (le vrai domaine est conseil-national.medecin.fr)", "Menace de radiation (levier émotionnel fort sur un praticien)", "Demande de paiement via lien web — jamais la procédure de l'Ordre", "Ton comminatoire inhabituel"] },
    { "id": "mail_5", "from_name": "Clinique des Peupliers — Secrétariat", "from_address": "secretariat@clinique-peupliers.fr", "subject": "Planning astreintes — semaine 17", "preview": "Bonjour, vous trouverez ci-joint le planning des astreintes...", "sent_at": "Hier 14:05", "is_dangerous": false, "body": "Bonjour Dr Lefèvre,\n\nVeuillez trouver ci-joint le planning des astreintes de la semaine 17 pour le service de médecine interne.\n\nMerci de me confirmer votre disponibilité avant vendredi.\n\nCordialement,\nM. Girard — Secrétariat Clinique des Peupliers", "red_flags": [] },
    { "id": "mail_6", "from_name": "Assurance Maladie", "from_address": "securite@cpam-verification.com", "subject": "Remboursement suspendu : vérifiez vos coordonnées bancaires", "preview": "Un remboursement de 1 430,20 € est en attente. Confirmez votre RIB...", "sent_at": "Aujourd'hui 11:47", "is_dangerous": true, "body": "Bonjour,\n\nUn remboursement de 1 430,20 € vous est dû au titre des feuilles de soins électroniques de ce trimestre.\n\nPour percevoir ce montant, confirmez votre RIB :\n\n→ http://cpam-verification.com/rib\n\nCaisse Primaire d'Assurance Maladie", "red_flags": ["Domaine 'cpam-verification.com' (le vrai domaine est ameli.fr)", "Montant précis pour crédibiliser", "Demande de coordonnées bancaires par lien email — jamais pratiqué par la CPAM", "Aucune personnalisation du destinataire"] }
  ]
}
"""
        });

        // ── C2 — Phishing Doctolib lookalike (multichoice) ──────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C2Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Phishing",
            Title        = "Faux message Doctolib — décryptage",
            Instructions = "Le Dr Marchand reçoit ce message dans sa messagerie professionnelle. Identifiez tous les éléments qui prouvent qu'il s'agit d'un phishing.",
            Difficulty   = 1,
            Points       = 150,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Doctolib — Support Praticien",
    "from_address": "support@doctolib-secu.fr",
    "to": "dr.marchand@cabinet-marchand.fr",
    "subject": "⚠️ Accès praticien bloqué — réactivation requise",
    "sent_at": "Aujourd'hui 07:58",
    "body": "Bonjour Docteur Marchand,\n\nSuite à plusieurs tentatives de connexion échouées, l'accès à votre agenda Doctolib a été temporairement bloqué.\n\nPour éviter la perte de vos rendez-vous programmés, réactivez votre compte dans les 2 heures :\n\n→ http://doctolib-secu.fr/reactivation?token=8f72ka91\n\nInformations à confirmer :\n• Identifiant praticien\n• Mot de passe actuel\n• Numéro RPPS\n\nL'équipe Support Doctolib"
  },
  "question": "Parmi ces éléments, lesquels prouvent que cet email est un phishing ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Le domaine 'doctolib-secu.fr' n'est pas le domaine officiel de Doctolib (doctolib.fr)", "is_correct": true,  "explanation": "Le domaine officiel est doctolib.fr. Tout ajout comme '-secu', '-securite' ou '-verification' est un indicateur typique de lookalike." },
    { "id": "B", "label": "La demande de saisir le mot de passe actuel dans un formulaire web externe",          "is_correct": true,  "explanation": "Aucun éditeur sérieux (Doctolib inclus) ne demande jamais votre mot de passe par email. Pour réinitialiser, on passe par l'application elle-même." },
    { "id": "C", "label": "L'urgence artificielle de 2 heures et la menace sur les rendez-vous",                 "is_correct": true,  "explanation": "L'urgence extrême (2h) et la menace sur un enjeu métier fort (agenda patients) sont les leviers d'ingénierie sociale les plus utilisés contre les praticiens." },
    { "id": "D", "label": "L'email s'adresse nommément au Dr Marchand, donc il est forcément authentique",       "is_correct": false, "explanation": "Le nom du praticien est public (annuaire RPPS, Doctolib lui-même). Une personnalisation ne prouve rien sur la légitimité de l'expéditeur." }
  ],
  "red_flags": [
    "Domaine lookalike doctolib-secu.fr (pas doctolib.fr)",
    "Demande du mot de passe actuel — jamais légitime",
    "Urgence de 2 heures + menace sur l'agenda patients",
    "Demande du numéro RPPS (donnée ciblée pour usurpation professionnelle)",
    "Token aléatoire dans l'URL pour tracer la victime",
    "Emoji dans le sujet pour capter l'attention"
  ],
  "savoir_plus": "Les éditeurs de logiciels métier santé (Doctolib, Maiia, Keldoc) sont des cibles privilégiées du phishing car un accès praticien permet d'accéder à l'agenda et aux données des patients. En cas de doute, se reconnecter via l'URL tapée à la main dans le navigateur."
}
"""
        });

        // ── C3 — Email CPAM suspect (phishing_ai) ───────────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C3Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Phishing",
            Title        = "Faux Ameli Pro — analyse libre",
            Instructions = "Vous êtes infirmier coordinateur à la Clinique des Peupliers. Vous recevez cet email. Rédigez votre analyse et la conduite à tenir.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Ameli Pro",
    "from_address": "notification@ameli-pro-maj.fr",
    "subject": "Mise à jour de sécurité Ameli Pro — action sous 48h",
    "body": "Bonjour,\n\nDans le cadre du renforcement de la sécurité des comptes professionnels de santé, une mise à jour de votre espace Ameli Pro est obligatoire.\n\nConnectez-vous via le portail sécurisé ci-dessous dans les 48h pour éviter la suspension de vos téléservices (feuilles de soins, DMP, etc.) :\n\n→ https://ameli-pro-maj.fr/mise-a-jour\n\nIdentifiants requis : numéro RPPS + mot de passe Ameli Pro + code envoyé sur votre téléphone.\n\nCordialement,\nL'équipe Ameli Pro"
  },
  "question": "Expliquez pourquoi cet email est suspect, quels éléments le trahissent, et quelle est la procédure exacte à suivre dans votre établissement.",
  "expected_elements": "Identification du domaine lookalike 'ameli-pro-maj.fr' (le vrai domaine est ameli.fr / amelipro.ameli.fr). Demande d'identifiants professionnels + code SMS = tentative de contournement de MFA. Menace de suspension des téléservices = urgence artificielle. Ne pas cliquer. Ne pas transférer. Signaler au référent cyber / DSI / DPO de la clinique. Vérifier directement en se connectant via l'URL tapée manuellement. Signalement possible sur signal-spam.fr ou phishing-initiative.fr.",
  "min_chars": 120,
  "hint": "Regardez le domaine, ce qui est demandé, et réfléchissez à qui prévenir dans votre établissement."
}
"""
        });

        // ── C4 — Arnaque au président (ceo_fraud) ───────────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C4Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Ingénierie Sociale",
            Title        = "Virement urgent du Dr Rambaud",
            Instructions = "Vous êtes assistante de gestion au Cabinet du Dr Rambaud. Vous recevez cet email urgent. Choisissez la ou les bonnes réactions.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 4,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Dr Philippe Rambaud",
    "from_address": "p.rambaud@cabinet-rambaud-direction.com",
    "to": "gestion@cabinet-rambaud.fr",
    "subject": "Virement urgent — confidentiel",
    "sent_at": "Aujourd'hui 13:52",
    "body": "Bonjour,\n\nJe suis en déplacement pour un congrès, injoignable par téléphone jusqu'à ce soir.\n\nJ'ai besoin que vous effectuiez un virement urgent de 12 800 € pour finaliser l'achat d'un équipement d'échographie pour le cabinet. C'est strictement confidentiel — n'en parlez pas à mon associé, je lui expliquerai moi-même.\n\nRIB du fournisseur :\nIBAN : BE68 5390 0754 7034\nBénéficiaire : MedEquip Trading\n\nMerci d'agir rapidement, l'offre expire ce soir.\n\nDr Rambaud"
  },
  "choices": [
    { "id": "pay",     "label": "Effectuer le virement, le Dr Rambaud a besoin de l'équipement", "icon": "bank",  "is_correct": false, "explanation": "C'est exactement le piège de l'arnaque au président : urgence + confidentialité + indisponibilité téléphonique. Ne jamais virer sur simple demande email." },
    { "id": "report",  "label": "Signaler à l'associé du Dr Rambaud et au cabinet comptable",     "icon": "flag",  "is_correct": true,  "explanation": "Excellent réflexe. L'injonction de secret est précisément conçue pour empêcher la vérification croisée. Signaler brise le mécanisme." },
    { "id": "verify",  "label": "Appeler le Dr Rambaud sur son portable habituel avant tout",     "icon": "phone", "is_correct": true,  "explanation": "Parfait : vérifier par un canal indépendant (numéro connu, pas celui d'un email) est la règle d'or contre la fraude au président." },
    { "id": "nothing", "label": "Ne rien faire et supprimer l'email",                              "icon": "x",     "is_correct": false, "explanation": "Supprimer ne protège pas le cabinet : l'attaquant va relancer ou cibler un collègue. Il faut signaler pour alerter toute l'équipe." }
  ],
  "red_flags": [
    "Domaine 'cabinet-rambaud-direction.com' inventé (pas le domaine réel du cabinet)",
    "Demande de confidentialité vis-à-vis de l'associé = levier classique",
    "Indisponibilité téléphonique invoquée pour empêcher la vérification",
    "Urgence (offre expirant le soir même)",
    "IBAN étranger (Belgique) pour un fournisseur jamais référencé",
    "Demande sortant totalement de la procédure achats habituelle"
  ]
}
"""
        });

        // ── C5 — Quiz mot de passe (password_quiz) ──────────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C5Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "password_quiz",
            Category     = "Authentification",
            Title        = "Mots de passe à la Clinique des Peupliers",
            Instructions = "Trois questions rapides pour sécuriser vos accès aux logiciels métier (DPI, Ameli Pro, Doctolib).",
            Difficulty   = 1,
            Points       = 125,
            SortOrder    = 5,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "intro": "Vous travaillez à la Clinique des Peupliers et vous devez choisir/gérer les mots de passe de vos outils métier (DPI, portail Ameli Pro, messagerie sécurisée). La politique interne impose 12 caractères minimum, sans réutilisation entre outils.",
  "rounds": [
    {
      "id": "round1",
      "question": "Lequel de ces mots de passe est le plus adapté pour votre accès au DPI (dossier patient informatisé) ?",
      "choices": [
        { "id": "A", "label": "Peupliers2026!",        "is_correct": false, "explanation": "Nom de l'établissement + année + ! : c'est le premier pattern testé par les attaquants ciblant un établissement connu." },
        { "id": "B", "label": "Infirmiere75!",         "is_correct": false, "explanation": "Contient la profession et un code postal — informations triviales à deviner pour un attaquant qui a fait un minimum de reconnaissance." },
        { "id": "C", "label": "Brume-Citron-Ocre-48$", "is_correct": true,  "explanation": "Excellent : 4 mots sans lien + symbole + chiffre = haute entropie, facile à retenir, très difficile à cracker (passphrase recommandée par l'ANSSI)." },
        { "id": "D", "label": "Azerty123456",          "is_correct": false, "explanation": "Suite clavier + chiffres : mot de passe le plus testé au monde. Aucune entropie malgré la longueur." }
      ]
    },
    {
      "id": "round2",
      "question": "Vous devez gérer les mots de passe du DPI, d'Ameli Pro, de Doctolib et de votre messagerie. Quelle est la bonne pratique ?",
      "choices": [
        { "id": "A", "label": "Utiliser le même mot de passe partout, en ajoutant juste le nom du service à la fin",              "is_correct": false, "explanation": "Si un seul service fuit, l'attaquant devine immédiatement tous les autres. C'est la cause n°1 de compromission multi-comptes." },
        { "id": "B", "label": "Noter les mots de passe dans un carnet dans le tiroir du bureau",                                    "is_correct": false, "explanation": "Accessible à tout collègue, agent de nettoyage ou visiteur passant au poste. Le secret médical commence par la sécurité physique du poste." },
        { "id": "C", "label": "Utiliser un gestionnaire de mots de passe (Bitwarden, KeePass) agréé par la DSI de la clinique",    "is_correct": true,  "explanation": "Un gestionnaire génère des mots de passe uniques et longs par service, sans surcharge cognitive. C'est la méthode recommandée par l'ANSSI et par la plupart des DSI santé." },
        { "id": "D", "label": "Les coller sur un post-it sous le clavier, personne ne regarde là",                                  "is_correct": false, "explanation": "Un post-it sous le clavier est la première chose qu'on regarde. Cas documenté dans presque tous les audits de cabinet médical." }
      ]
    },
    {
      "id": "round3",
      "question": "Un collègue vous dit qu'il partage son identifiant Ameli Pro avec sa remplaçante 'pour aller plus vite'. Que lui répondez-vous ?",
      "choices": [
        { "id": "A", "label": "C'est pratique et ça évite une demande administrative, pas de problème",                                 "is_correct": false, "explanation": "Le partage d'identifiant praticien est formellement interdit : en cas d'incident, toute action est imputée au titulaire du compte." },
        { "id": "B", "label": "Il doit demander un compte nominatif pour la remplaçante via les procédures CPS/RPPS",                   "is_correct": true,  "explanation": "Bonne réponse : chaque professionnel de santé doit avoir ses propres identifiants nominatifs. La traçabilité médicale l'exige." },
        { "id": "C", "label": "Il suffit qu'ils signent un accord entre eux en cas de problème",                                        "is_correct": false, "explanation": "Aucun accord privé n'exonère de la réglementation : la CPS est strictement personnelle et engage la responsabilité de son titulaire." },
        { "id": "D", "label": "Il faut au moins changer le mot de passe après chaque remplacement",                                     "is_correct": false, "explanation": "Le partage reste interdit même avec rotation du mot de passe. Seul un compte nominatif par praticien est conforme." }
      ]
    }
  ]
}
"""
        });

        // ── C6 — Fiche clinique multi-choix RGPD (multichoice) ──────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C6Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Hygiène numérique",
            Title        = "Le poste de secrétariat du Dr Marchand",
            Instructions = "Analysez la scène décrite et identifiez les bonnes pratiques à mettre en œuvre immédiatement.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 6,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Note interne — Dr Marchand",
    "from_address": "direction@cabinet-marchand.fr",
    "to": "equipe@cabinet-marchand.fr",
    "subject": "Observations audit interne — poste secrétariat",
    "sent_at": "Aujourd'hui 11:15",
    "body": "Bonjour à tous,\n\nLors du passage rapide ce matin au poste de secrétariat, j'ai observé la situation suivante :\n\n• Le poste est resté déverrouillé pendant 40 minutes pendant la pause déjeuner.\n• Un patient assis dans la salle d'attente voyait parfaitement l'écran depuis le guichet.\n• Un post-it avec un identifiant DPI est collé sur l'écran.\n• Une clé USB avec 'Résultats labo BioNordik' est branchée en permanence.\n• Le navigateur a enregistré le mot de passe Ameli Pro.\n\nPouvez-vous me proposer les corrections ? Merci.\n\nDr Marchand"
  },
  "question": "Parmi ces actions, lesquelles sont correctes et immédiates à mettre en œuvre ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Configurer le verrouillage automatique de session après 5 min d'inactivité et apprendre le raccourci Windows+L",                          "is_correct": true,  "explanation": "Le verrouillage automatique est la barrière la plus simple contre l'accès non autorisé. L'ANSSI et la CNIL le recommandent explicitement pour les postes manipulant des données de santé." },
    { "id": "B", "label": "Orienter l'écran ou installer un filtre de confidentialité pour qu'aucun patient ne puisse lire les informations d'un autre patient",     "is_correct": true,  "explanation": "La confidentialité visuelle fait partie du secret médical : un patient qui voit le dossier d'un autre est une fuite de données immédiate." },
    { "id": "C", "label": "Retirer le post-it, changer le mot de passe concerné et utiliser un gestionnaire de mots de passe",                                        "is_correct": true,  "explanation": "Un mot de passe affiché est considéré comme compromis. Le changer et le stocker dans un gestionnaire est la seule conduite correcte." },
    { "id": "D", "label": "Laisser la clé USB branchée en permanence, c'est plus pratique pour accéder aux résultats",                                                "is_correct": false, "explanation": "Une clé USB laissée branchée contient des données patients accessibles à toute personne passant au poste. Elle doit être chiffrée, retirée à chaque absence, et idéalement remplacée par un partage sécurisé." }
  ],
  "red_flags": [
    "Poste déverrouillé hors présence = accès libre aux dossiers",
    "Écran visible par des patients = secret médical rompu",
    "Identifiants affichés en clair = compromission immédiate",
    "Clé USB non chiffrée avec données patients",
    "Mot de passe enregistré dans le navigateur sur poste partagé",
    "Aucune session nominative par utilisateur du poste"
  ],
  "savoir_plus": "La CNIL a publié un guide 'La sécurité des données personnelles' (édition 2024) qui détaille toutes ces pratiques. Pour les établissements de santé, ces mesures font aussi partie du référentiel HDS (Hébergement de Données de Santé)."
}
"""
        });

        // ── C7 — Analyse libre finale (free_text) ───────────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C7Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Analyse de situation",
            Title        = "Trois situations en cabinet — que faites-vous ?",
            Instructions = "Trois mises en situation courtes tirées du quotidien d'un cabinet médical et d'un laboratoire. Rédigez votre raisonnement, l'IA évaluera la pertinence.",
            Difficulty   = 3,
            Points       = 200,
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
      "question": "Une patiente vous appelle au secrétariat du cabinet du Dr Lefèvre et demande, très insistante, de lui communiquer par téléphone les résultats d'analyses qu'elle attend du Laboratoire BioNordik. Elle dit ne pas pouvoir se déplacer. Que faites-vous concrètement ?",
      "context": "secret médical et canal de communication",
      "expected_elements": "Refus poli de communiquer par téléphone non identifié. Vérification de l'identité impossible par téléphone simple. Proposer la messagerie sécurisée de santé (MSSanté), le portail patient du laboratoire, ou un rendez-vous avec le médecin. Toujours impliquer le praticien pour la restitution des résultats sensibles. Ne jamais transmettre par SMS ou email non sécurisé.",
      "min_chars": 100,
      "hint": "Qui a le droit d'entendre quoi, et par quel canal ?"
    },
    {
      "id": "q2",
      "question": "Vous êtes infirmier coordinateur à la Clinique des Peupliers. Une clé USB non identifiée est retrouvée sur le parking du personnel, étiquetée 'Planning astreintes 2026'. Un collègue veut la brancher sur le poste infirmier pour voir de quoi il s'agit. Que faites-vous et pourquoi ?",
      "context": "intrusion physique / clé USB malveillante",
      "expected_elements": "Ne surtout pas brancher la clé : technique connue de 'USB drop attack' pouvant installer un malware et ouvrir l'accès au SI. Remettre la clé à la DSI ou au RSSI. Sensibiliser le collègue. Ne jamais tester le contenu sur un poste de production. Les clés USB inconnues font partie des premiers vecteurs d'intrusion dans les établissements de santé.",
      "min_chars": 100,
      "hint": "Pourquoi une clé USB trouvée est une menace active ?"
    },
    {
      "id": "q3",
      "question": "Au laboratoire BioNordik, un technicien remarque qu'un confrère utilise son smartphone personnel pour photographier l'écran de résultats d'analyses 'pour les montrer au médecin traitant'. Quels sont les risques et quelle est la bonne pratique ?",
      "context": "exfiltration involontaire de données de santé",
      "expected_elements": "Données patient sorties du SI sécurisé vers un appareil personnel non maîtrisé. Risque de sauvegarde automatique dans un cloud personnel (iCloud, Google Photos). Violation potentielle du RGPD et du référentiel HDS. Bonne pratique : utiliser la messagerie sécurisée de santé (MSSanté) ou le portail d'échange du laboratoire pour transmettre les résultats de manière chiffrée et tracée. Sensibiliser l'équipe et signaler au DPO si une photo a déjà été prise.",
      "min_chars": 100,
      "hint": "Que se passe-t-il quand une donnée patient sort du SI sécurisé ?"
    }
  ]
}
"""
        });

        await CatalogSeedBase.EnsureDemoAccessAsync(db, PathId, now);
    }
}
