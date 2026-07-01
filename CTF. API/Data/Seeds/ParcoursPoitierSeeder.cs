using CTF.Api.Models;

namespace CTF.Api.Data.Seeds;

/// <summary>
/// Seeder d'un parcours PRIVÉ pour le tenant "Poitier" (établissement de santé).
///
/// Parcours privé tenant : TenantId = Poitier, IsCatalog = false, Type = "custom".
/// D'après <c>ParcoursVisibilityService</c>, un tel parcours est visible par les
/// membres du tenant SANS aucune ligne d'assignation : la règle utilisateur inclut
/// directement « (parcours privés du tenant : p.TenantId == tenantId && !p.IsCatalog) ».
/// On n'utilise donc PAS le mécanisme catalogue (aucun TenantParcoursAccess).
///
/// Idempotent et rejouable : pour chaque entité, FindAsync(id) -> Add si absent / Update si présent.
/// GUIDs stables en dur.
///
/// Couvre TOUS les types de challenges interactifs (ContentType) du parcours de référence
/// Parcours01_SanteFondamentaux : mailbox, multichoice, phishing_ai, ceo_fraud, password_quiz, free_text.
/// </summary>
public static class ParcoursPoitierSeeder
{
    // ── Identité tenant + auteur ─────────────────────────────────────────────
    private static readonly Guid PoitierTenantId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid AuthorId        = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ── Parcours ─────────────────────────────────────────────────────────────
    private static readonly Guid PathId = Guid.Parse("d1000000-0000-0000-0000-000000000001");

    // ── Modules (5) ──────────────────────────────────────────────────────────
    private static readonly Guid Module1Id = Guid.Parse("d1000001-0000-0000-0000-000000000001"); // Accès & bonnes pratiques
    private static readonly Guid Module2Id = Guid.Parse("d1000002-0000-0000-0000-000000000001"); // Sécurité en santé
    private static readonly Guid Module3Id = Guid.Parse("d1000003-0000-0000-0000-000000000001"); // Jeu de rôle
    private static readonly Guid Module4Id = Guid.Parse("d1000004-0000-0000-0000-000000000001"); // Boîte mail
    private static readonly Guid Module5Id = Guid.Parse("d1000005-0000-0000-0000-000000000001"); // Arnaque & fraude

    // ── Challenges (GUIDs stables) ───────────────────────────────────────────
    // Module 1
    private static readonly Guid C101Id = Guid.Parse("d1000001-0001-0000-0000-000000000001"); // password_quiz
    private static readonly Guid C102Id = Guid.Parse("d1000001-0002-0000-0000-000000000001"); // multichoice
    private static readonly Guid C103Id = Guid.Parse("d1000001-0003-0000-0000-000000000001"); // free_text
    // Module 2
    private static readonly Guid C201Id = Guid.Parse("d1000002-0001-0000-0000-000000000001"); // multichoice
    private static readonly Guid C202Id = Guid.Parse("d1000002-0002-0000-0000-000000000001"); // free_text
    private static readonly Guid C203Id = Guid.Parse("d1000002-0003-0000-0000-000000000001"); // multichoice
    // Module 3
    private static readonly Guid C301Id = Guid.Parse("d1000003-0001-0000-0000-000000000001"); // ceo_fraud (jeu de rôle à choix)
    private static readonly Guid C302Id = Guid.Parse("d1000003-0002-0000-0000-000000000001"); // ceo_fraud
    // Module 4
    private static readonly Guid C401Id = Guid.Parse("d1000004-0001-0000-0000-000000000001"); // mailbox
    private static readonly Guid C402Id = Guid.Parse("d1000004-0002-0000-0000-000000000001"); // multichoice
    private static readonly Guid C403Id = Guid.Parse("d1000004-0003-0000-0000-000000000001"); // phishing_ai
    // Module 5
    private static readonly Guid C501Id = Guid.Parse("d1000005-0001-0000-0000-000000000001"); // ceo_fraud
    private static readonly Guid C502Id = Guid.Parse("d1000005-0002-0000-0000-000000000001"); // multichoice
    private static readonly Guid C503Id = Guid.Parse("d1000005-0003-0000-0000-000000000001"); // phishing_ai
    private static readonly Guid C504Id = Guid.Parse("d1000005-0004-0000-0000-000000000001"); // free_text

    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;

        // ════════════════════════════ PARCOURS ════════════════════════════════
        await UpsertPathAsync(db, new LearningPath
        {
            Id               = PathId,
            TenantId         = PoitierTenantId,
            Type             = "custom",          // parcours privé tenant (cf. ParcoursVisibilityService)
            Title            = "Parcours Sécurité en Santé — Poitier",
            Description      = "Parcours interne de sensibilisation à la cybersécurité pour les équipes médicales, paramédicales et administratives de l'établissement Poitier : accès et postes partagés, protection des données patients, reconnaissance du phishing, mises en situation, et lutte contre la fraude et les rançongiciels en milieu hospitalier.",
            Level            = "beginner",
            Status           = "published",
            Version          = 1,
            IsCatalog        = false,             // privé tenant — PAS de catalogue
            Sector           = "sante",
            EstimatedMinutes = 45,
            Tags             = "sante,hopital,phishing,mots-de-passe,rgpd,hds,fraude,rancongiciel,interne",
            CreatedBy        = AuthorId,
            CreatedAt        = now,
            PublishedAt      = now
        });

        // ════════════════════════════ MODULES ═════════════════════════════════
        await UpsertModuleAsync(db, new Module
        {
            Id        = Module1Id,
            TenantId  = PoitierTenantId,
            PathId    = PathId,
            Title     = "Accès & bonnes pratiques santé",
            SortOrder = 1,
            CreatedAt = now
        });
        await UpsertModuleAsync(db, new Module
        {
            Id        = Module2Id,
            TenantId  = PoitierTenantId,
            PathId    = PathId,
            Title     = "Sécurité en santé (postes partagés, données patients, continuité)",
            SortOrder = 2,
            CreatedAt = now
        });
        await UpsertModuleAsync(db, new Module
        {
            Id        = Module3Id,
            TenantId  = PoitierTenantId,
            PathId    = PathId,
            Title     = "Jeu de rôle en santé (mise en situation interactive)",
            SortOrder = 3,
            CreatedAt = now
        });
        await UpsertModuleAsync(db, new Module
        {
            Id        = Module4Id,
            TenantId  = PoitierTenantId,
            PathId    = PathId,
            Title     = "Boîte mail en santé (tri emails légitimes vs phishing)",
            SortOrder = 4,
            CreatedAt = now
        });
        await UpsertModuleAsync(db, new Module
        {
            Id        = Module5Id,
            TenantId  = PoitierTenantId,
            PathId    = PathId,
            Title     = "Arnaque & fraude en santé (fraude au président, faux fournisseurs, rançongiciels)",
            SortOrder = 5,
            CreatedAt = now
        });

        // ═══════════════ MODULE 1 — Accès & bonnes pratiques santé ═════════════

        // C1.01 — Mots de passe & accès (password_quiz)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C101Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "password_quiz",
            Category     = "Authentification",
            Title        = "Vos accès au CHU Poitier",
            Instructions = "Trois questions rapides pour sécuriser vos accès aux logiciels métier de l'établissement (DPI, portail Ameli Pro, messagerie sécurisée de santé).",
            Difficulty   = 1,
            Points       = 125,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "intro": "Vous travaillez à l'hôpital Poitier et vous gérez les mots de passe de vos outils métier (DPI, portail Ameli Pro, messagerie MSSanté). La politique interne impose 12 caractères minimum, sans réutilisation entre outils.",
  "rounds": [
    {
      "id": "round1",
      "question": "Lequel de ces mots de passe est le plus adapté pour votre accès au DPI (dossier patient informatisé) ?",
      "choices": [
        { "id": "A", "label": "Poitier2026!",            "is_correct": false, "explanation": "Nom de l'établissement + année + ! : c'est le tout premier pattern testé par les attaquants ciblant un hôpital connu." },
        { "id": "B", "label": "Infirmiere86!",           "is_correct": false, "explanation": "Profession + département (86) : informations triviales à deviner après une reconnaissance minimale." },
        { "id": "C", "label": "Tilleul-Brume-Ocre-72$",  "is_correct": true,  "explanation": "Excellent : 4 mots sans lien + symbole + chiffre = haute entropie, facile à retenir, très difficile à cracker (passphrase recommandée par l'ANSSI)." },
        { "id": "D", "label": "Azerty123456",            "is_correct": false, "explanation": "Suite clavier + chiffres : le mot de passe le plus testé au monde. Aucune entropie malgré la longueur." }
      ]
    },
    {
      "id": "round2",
      "question": "Vous gérez les mots de passe du DPI, d'Ameli Pro, de la MSSanté et de votre messagerie interne. Quelle est la bonne pratique ?",
      "choices": [
        { "id": "A", "label": "Le même mot de passe partout en ajoutant le nom du service à la fin",                       "is_correct": false, "explanation": "Si un seul service fuit, l'attaquant devine tous les autres. Cause n°1 de compromission multi-comptes." },
        { "id": "B", "label": "Les noter dans un carnet dans le tiroir du poste de soins",                                 "is_correct": false, "explanation": "Accessible à tout collègue, agent ou visiteur. Le secret médical commence par la sécurité physique du poste." },
        { "id": "C", "label": "Utiliser un gestionnaire de mots de passe (Bitwarden, KeePass) agréé par la DSI de Poitier", "is_correct": true,  "explanation": "Un gestionnaire génère des mots de passe uniques et longs par service, sans surcharge mentale. Recommandé par l'ANSSI et la plupart des DSI santé." },
        { "id": "D", "label": "Les coller sur un post-it sous le clavier du poste de soins",                               "is_correct": false, "explanation": "Le post-it sous le clavier est la première cachette qu'on inspecte. Cas documenté dans presque tous les audits hospitaliers." }
      ]
    },
    {
      "id": "round3",
      "question": "Un collègue partage son identifiant Ameli Pro avec sa remplaçante 'pour aller plus vite'. Que lui répondez-vous ?",
      "choices": [
        { "id": "A", "label": "C'est pratique, ça évite une démarche administrative, pas de problème",                  "is_correct": false, "explanation": "Le partage d'identifiant praticien est formellement interdit : toute action est imputée au titulaire du compte." },
        { "id": "B", "label": "Elle doit demander un compte nominatif via les procédures CPS/RPPS de l'établissement", "is_correct": true,  "explanation": "Chaque professionnel doit avoir ses propres identifiants nominatifs. La traçabilité médicale l'exige." },
        { "id": "C", "label": "Un accord écrit entre eux suffit en cas de problème",                                    "is_correct": false, "explanation": "Aucun accord privé n'exonère de la réglementation : la CPS est strictement personnelle." },
        { "id": "D", "label": "Il suffit de changer le mot de passe après chaque remplacement",                        "is_correct": false, "explanation": "Le partage reste interdit même avec rotation. Seul un compte nominatif par praticien est conforme." }
      ]
    }
  ]
}
"""
        });

        // C1.02 — Hygiène du poste partagé (multichoice)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C102Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Hygiène numérique",
            Title        = "Le poste de soins partagé",
            Instructions = "Analysez la note d'audit interne ci-dessous et identifiez les bonnes pratiques à mettre en œuvre immédiatement.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Note interne — DSI Poitier",
    "from_address": "dsi@hopital-poitier.fr",
    "to": "service-medecine@hopital-poitier.fr",
    "subject": "Observations audit — poste de soins partagé",
    "sent_at": "Aujourd'hui 11:15",
    "body": "Bonjour à tous,\n\nLors de l'audit de ce matin au poste de soins du 3e étage, j'ai relevé :\n\n• Le poste est resté déverrouillé pendant toute la pause déjeuner.\n• Un patient en fauteuil dans le couloir voyait l'écran du DPI.\n• Un post-it avec l'identifiant DPI est collé sur l'écran.\n• Une clé USB 'Comptes-rendus' est branchée en permanence.\n• Le navigateur a enregistré le mot de passe Ameli Pro.\n\nMerci de me proposer les corrections.\n\nDSI Poitier",
    "savoir_plus": ""
  },
  "question": "Parmi ces actions, lesquelles sont correctes et immédiates ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Activer le verrouillage automatique après 5 min d'inactivité et utiliser Windows+L à chaque départ",          "is_correct": true,  "explanation": "Le verrouillage automatique est la barrière la plus simple contre l'accès non autorisé aux dossiers patients. Recommandé par l'ANSSI et la CNIL." },
    { "id": "B", "label": "Orienter l'écran ou poser un filtre de confidentialité pour qu'aucun patient ne lise le dossier d'un autre", "is_correct": true,  "explanation": "La confidentialité visuelle relève du secret médical : un patient qui voit le dossier d'un autre est une fuite de données." },
    { "id": "C", "label": "Retirer le post-it, changer le mot de passe affiché et le stocker dans le gestionnaire de la DSI",          "is_correct": true,  "explanation": "Un mot de passe affiché est compromis. Le changer et le stocker dans un gestionnaire est la seule conduite correcte." },
    { "id": "D", "label": "Laisser la clé USB branchée en permanence, c'est plus pratique pour les comptes-rendus",                    "is_correct": false, "explanation": "Une clé USB laissée branchée expose des données patients à toute personne au poste. Elle doit être chiffrée, retirée, ou remplacée par un partage sécurisé." }
  ],
  "red_flags": [
    "Poste déverrouillé hors présence = accès libre aux dossiers",
    "Écran visible par des patients = secret médical rompu",
    "Identifiants affichés en clair = compromission immédiate",
    "Clé USB non chiffrée avec données patients",
    "Mot de passe enregistré dans le navigateur sur poste partagé"
  ],
  "savoir_plus": "Ces mesures figurent dans le guide CNIL 'La sécurité des données personnelles' et dans le référentiel HDS (Hébergement de Données de Santé) applicable aux établissements."
}
"""
        });

        // C1.03 — Analyse libre : verrouillage & secret médical (free_text)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C103Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Analyse de situation",
            Title        = "Verrouillage et secret médical — votre raisonnement",
            Instructions = "Deux mises en situation courtes du quotidien hospitalier. Rédigez votre raisonnement, l'IA évaluera la pertinence.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "questions": [
    {
      "id": "q1",
      "question": "Vous êtes appelé en urgence auprès d'un patient et vous laissez votre session DPI ouverte au poste de soins, dans un couloir fréquenté. Pourquoi est-ce un risque et que devez-vous faire à l'avenir ?",
      "context": "verrouillage de session / accès non autorisé",
      "expected_elements": "Une session ouverte donne accès à l'ensemble des dossiers patients à toute personne de passage (collègue, patient, visiteur, prestataire). Risque de consultation non tracée, modification, fuite, atteinte au secret médical. Réflexe : verrouiller systématiquement (Windows+L) même pour une courte absence, activer le verrouillage automatique, ne jamais partager sa session.",
      "min_chars": 100,
      "hint": "Qui peut accéder à quoi quand vous quittez le poste ?"
    },
    {
      "id": "q2",
      "question": "Un brancardier vous demande de lui dicter le numéro de chambre et le diagnostic d'un patient devant d'autres patients dans le couloir. Comment réagissez-vous et pourquoi ?",
      "context": "confidentialité orale / secret médical",
      "expected_elements": "Le secret médical couvre aussi les échanges oraux. Ne pas divulguer de diagnostic à portée de voix de tiers. Se déplacer à l'écart, transmettre l'information strictement nécessaire et seulement aux personnes habilitées, via les canaux internes prévus. Sensibiliser le collègue. Le principe du besoin d'en connaître s'applique.",
      "min_chars": 100,
      "hint": "Le secret médical s'arrête-t-il à l'écran ?"
    }
  ]
}
"""
        });

        // ═══════ MODULE 2 — Sécurité en santé (postes, données, continuité) ════

        // C2.01 — Données patients & RGPD/HDS (multichoice)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C201Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Protection des données",
            Title        = "Données patients : ce qui est permis",
            Instructions = "Sélectionnez toutes les pratiques conformes au RGPD et au référentiel HDS pour la manipulation des données patients.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "question": "Parmi ces pratiques de manipulation des données patients, lesquelles sont conformes ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Transmettre un compte-rendu à un confrère via la messagerie sécurisée de santé (MSSanté)",       "is_correct": true,  "explanation": "La MSSanté est le canal chiffré et tracé prévu pour l'échange de données de santé entre professionnels. C'est la bonne pratique de référence." },
    { "id": "B", "label": "Envoyer une photo de l'écran de résultats depuis son smartphone personnel via WhatsApp",        "is_correct": false, "explanation": "Données patient sorties du SI vers un appareil non maîtrisé, avec sauvegarde cloud possible. Violation du RGPD et du HDS." },
    { "id": "C", "label": "Appliquer le principe du 'besoin d'en connaître' : n'accéder qu'aux dossiers nécessaires à sa prise en charge", "is_correct": true,  "explanation": "Le besoin d'en connaître limite les accès au strict nécessaire. Consulter un dossier sans lien avec son activité est une faute, même par curiosité." },
    { "id": "D", "label": "Stocker une copie d'un dossier patient sur une clé USB personnelle non chiffrée pour travailler chez soi", "is_correct": false, "explanation": "Support non chiffré et hors du SI : en cas de perte ou de vol, fuite de données de santé. Strictement à proscrire." }
  ],
  "red_flags": [
    "Appareils personnels non maîtrisés (BYOD non encadré)",
    "Canaux non chiffrés (WhatsApp, email perso, SMS)",
    "Accès à des dossiers hors besoin d'en connaître",
    "Supports amovibles non chiffrés"
  ],
  "savoir_plus": "Le RGPD impose la minimisation et la sécurité des traitements ; le référentiel HDS encadre l'hébergement des données de santé. La consultation d'un dossier sans motif légitime est tracée et sanctionnable."
}
"""
        });

        // C2.02 — Continuité d'activité / incident (free_text)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C202Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Continuité d'activité",
            Title        = "Le DPI est inaccessible — que faites-vous ?",
            Instructions = "Mise en situation de continuité d'activité. Rédigez votre conduite à tenir, l'IA évaluera la pertinence.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "questions": [
    {
      "id": "q1",
      "question": "Un matin, le DPI et plusieurs applications métier de l'hôpital Poitier sont totalement inaccessibles, des messages d'erreur inhabituels apparaissent et un écran affiche une demande de rançon. Quelle est votre conduite à tenir immédiate en tant que soignant ?",
      "context": "incident de sécurité / rançongiciel / continuité des soins",
      "expected_elements": "Ne pas éteindre ni tenter de réparer soi-même ; ne pas payer ; ne rien brancher. Alerter immédiatement la DSI / le RSSI et la hiérarchie via la procédure d'incident. Débrancher du réseau le poste suspect si la consigne le prévoit. Basculer sur les procédures dégradées papier prévues par le plan de continuité (PCA) pour assurer la continuité des soins. Tracer les actions sur support papier. Ne pas communiquer à l'extérieur sans consigne.",
      "min_chars": 120,
      "hint": "Priorité : continuité des soins + alerte, sans aggraver l'incident."
    }
  ]
}
"""
        });

        // C2.03 — Postes partagés & sessions nominatives (multichoice)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C203Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Hygiène numérique",
            Title        = "Sessions nominatives sur poste partagé",
            Instructions = "Plusieurs soignants se relaient sur un même poste. Identifiez les pratiques correctes.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "question": "Sur un poste partagé entre plusieurs soignants, quelles pratiques sont correctes ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Chaque soignant ouvre et ferme SA propre session nominative à chaque relève",                 "is_correct": true,  "explanation": "La session nominative garantit la traçabilité : chaque accès au dossier est rattaché à la bonne personne. C'est une exigence de sécurité et de droit." },
    { "id": "B", "label": "Utiliser une session générique 'soins3etage' partagée par toute l'équipe pour gagner du temps", "is_correct": false, "explanation": "Une session partagée détruit la traçabilité : impossible de savoir qui a consulté quoi. À proscrire pour les accès aux données de santé." },
    { "id": "C", "label": "Verrouiller le poste (Windows+L) dès qu'on s'éloigne, même quelques minutes",                "is_correct": true,  "explanation": "Le verrouillage empêche qu'un autre agisse sous votre identité ou consulte des dossiers en votre absence." },
    { "id": "D", "label": "Laisser la session du collègue précédent ouverte pour ne pas avoir à se reconnecter",          "is_correct": false, "explanation": "Travailler sous la session d'autrui fausse la traçabilité et engage la responsabilité du titulaire. Toujours ouvrir sa propre session." }
  ],
  "red_flags": [
    "Comptes génériques partagés",
    "Sessions laissées ouvertes entre deux utilisateurs",
    "Absence de traçabilité nominative des accès"
  ],
  "savoir_plus": "La traçabilité nominative des accès aux données de santé est une obligation. Les comptes génériques sont l'un des écarts les plus fréquemment relevés en audit hospitalier."
}
"""
        });

        // ═══════ MODULE 3 — Jeu de rôle en santé (mise en situation) ═══════════

        // C3.01 — Faux technicien informatique (ceo_fraud = scénario à choix)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C301Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module3Id,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Ingénierie Sociale",
            Title        = "Le faux technicien au téléphone",
            Instructions = "Vous êtes au standard du service. Vous recevez ce message. Choisissez la ou les bonnes réactions.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Support Informatique",
    "from_address": "support-it@hopital-poitier-assistance.com",
    "to": "service-medecine@hopital-poitier.fr",
    "subject": "Maintenance urgente DPI — confirmation requise",
    "sent_at": "Aujourd'hui 09:30",
    "body": "Bonjour,\n\nDans le cadre d'une maintenance urgente du DPI, notre technicien va vous appeler dans quelques minutes. Pour accélérer l'intervention, merci de lui communiquer votre identifiant et votre mot de passe DPI lorsqu'il vous le demandera, afin qu'il puisse tester votre accès à distance.\n\nMerci de votre coopération.\n\nSupport Informatique"
  },
  "choices": [
    { "id": "give",    "label": "Communiquer mon identifiant et mot de passe au technicien quand il appelle", "icon": "key",   "is_correct": false, "explanation": "La DSI ne demande JAMAIS votre mot de passe : un vrai technicien n'en a pas besoin. C'est une tentative d'usurpation classique." },
    { "id": "verify",  "label": "Raccrocher / ne rien donner et rappeler la DSI via le numéro interne officiel", "icon": "phone", "is_correct": true,  "explanation": "Parfait : vérifier par le canal interne connu (et non celui fourni par l'email) est la règle d'or contre l'ingénierie sociale." },
    { "id": "report",  "label": "Signaler le message et l'appel à la DSI / au RSSI pour alerter les collègues",   "icon": "flag",  "is_correct": true,  "explanation": "Excellent : signaler permet d'alerter toute l'équipe, car l'attaquant cible souvent plusieurs personnes." },
    { "id": "nothing", "label": "Ne rien faire, supprimer l'email et attendre l'appel",                          "icon": "x",     "is_correct": false, "explanation": "Supprimer ne protège pas le service : l'attaquant rappellera ou ciblera un collègue. Il faut signaler." }
  ],
  "red_flags": [
    "Domaine 'hopital-poitier-assistance.com' inventé (pas le domaine interne)",
    "Demande du mot de passe — jamais légitime de la part d'un support",
    "Prétexte de maintenance urgente pour créer la pression",
    "Appel téléphonique annoncé pour contourner la vigilance email",
    "Aucune référence à un ticket ou une procédure interne connue"
  ]
}
"""
        });

        // C3.02 — Le visiteur pressé (ceo_fraud = scénario à choix)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C302Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module3Id,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Ingénierie Sociale",
            Title        = "Le 'prestataire' sans badge",
            Instructions = "Mise en situation dans les locaux. Vous recevez ce mot juste avant. Choisissez la ou les bonnes réactions.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Direction Logistique",
    "from_address": "logistique@hopital-poitier.fr",
    "to": "service-medecine@hopital-poitier.fr",
    "subject": "Passage prestataire — biomédical",
    "sent_at": "Aujourd'hui 14:05",
    "body": "Bonjour,\n\nUn prestataire va passer aujourd'hui dans le service pour 'vérifier des équipements'. Il se présente sans badge visiteur, dit avoir oublié son ordre de mission, et demande à accéder seul à la salle informatique et à un poste déverrouillé pour 'faire un test rapide'.\n\nMerci de faciliter son intervention.\n\nLogistique"
  },
  "choices": [
    { "id": "open",   "label": "Le laisser accéder seul à la salle informatique et lui prêter un poste déverrouillé", "icon": "door",  "is_correct": false, "explanation": "Accès non vérifié à des zones sensibles : c'est exactement le scénario d'intrusion physique (tailgating). Jamais sans contrôle." },
    { "id": "badge",  "label": "Demander son ordre de mission, vérifier son identité et exiger un badge visiteur",   "icon": "id",    "is_correct": true,  "explanation": "Le contrôle d'accès physique fait partie de la sécurité : pas d'ordre de mission ni de badge = pas d'accès aux zones sensibles." },
    { "id": "escort", "label": "Prévenir l'accueil/sécurité et ne jamais le laisser seul dans une zone sensible",     "icon": "shield","is_correct": true,  "explanation": "Accompagner et faire vérifier par la sécurité empêche tout accès non autorisé au SI ou aux données." },
    { "id": "trust",  "label": "Lui faire confiance car l'email vient de la logistique interne",                      "icon": "mail",  "is_correct": false, "explanation": "Un email peut être usurpé et ne dispense pas du contrôle physique. L'absence de badge et d'ordre de mission reste bloquante." }
  ],
  "red_flags": [
    "Absence de badge visiteur et d'ordre de mission",
    "Demande d'accès SEUL à une zone sensible (salle informatique)",
    "Demande d'un poste déverrouillé",
    "Urgence et 'oubli' invoqués pour contourner les contrôles",
    "Tailgating : entrer dans le sillage du personnel"
  ]
}
"""
        });

        // ═══════ MODULE 4 — Boîte mail en santé (tri légitimes vs phishing) ════

        // C4.01 — Tri de la boîte mail (mailbox)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C401Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module4Id,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "La boîte mail du secrétariat de Poitier",
            Instructions = "Vous êtes secrétaire médical(e) à l'hôpital Poitier. Cochez uniquement les emails que vous considérez comme dangereux ou suspects.",
            Difficulty   = 1,
            Points       = 100,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "emails": [
    { "id": "mail_1", "from_name": "Doctolib Sécurité", "from_address": "securite@doctolib-verif.fr", "subject": "Vérification urgente de votre compte praticien", "preview": "Votre agenda Doctolib sera suspendu sous 24h si vous ne vérifiez pas...", "sent_at": "Aujourd'hui 08:41", "is_dangerous": true, "body": "Cher praticien,\n\nNous avons détecté une activité inhabituelle. Pour éviter la suspension de votre agenda sous 24h, vérifiez votre identité :\n\n→ http://doctolib-verif.fr/verif-praticien\n\nÉquipe Sécurité Doctolib", "red_flags": ["Domaine 'doctolib-verif.fr' au lieu de doctolib.fr", "Menace de suspension sous 24h (urgence artificielle)", "Lien hors domaine officiel", "Doctolib ne demande jamais de vérification par email externe"] },
    { "id": "mail_2", "from_name": "DSI Poitier", "from_address": "dsi@hopital-poitier.fr", "subject": "Maintenance planifiée du DPI dimanche 6h-8h", "preview": "Bonjour, une maintenance du DPI est programmée dimanche...", "sent_at": "Hier 16:10", "is_dangerous": false, "body": "Bonjour,\n\nUne maintenance planifiée du DPI aura lieu dimanche de 6h à 8h. Aucun accès durant ce créneau. Aucune action de votre part n'est requise.\n\nDSI Poitier", "red_flags": [] },
    { "id": "mail_3", "from_name": "Laboratoire BioPoitou", "from_address": "resultats@biopoitou.fr", "subject": "Résultats d'analyses — Patient M. Perrin (dossier 48291)", "preview": "Bonjour Docteur, résultats disponibles sur le portail sécurisé...", "sent_at": "Hier 17:22", "is_dangerous": false, "body": "Bonjour Docteur,\n\nLes résultats de M. Perrin (dossier 48291) sont disponibles sur notre portail sécurisé habituel :\n\nhttps://portail.biopoitou.fr/resultats/48291\n\nLaboratoire BioPoitou", "red_flags": [] },
    { "id": "mail_4", "from_name": "CPAM — Espace Pro", "from_address": "no-reply@ameli-pro-maj.fr", "subject": "Mise à jour obligatoire de votre carte CPS", "preview": "La mise à jour annuelle de votre carte CPS doit être effectuée avant le 30...", "sent_at": "Aujourd'hui 10:02", "is_dangerous": true, "body": "Bonjour,\n\nLa mise à jour annuelle de votre carte CPS doit être réalisée avant le 30.\n\nCliquez ici : http://ameli-pro-maj.fr/cps-sync\n\nAmeli Pro", "red_flags": ["Domaine 'ameli-pro-maj.fr' (le vrai domaine est ameli.fr)", "La CPS se met à jour via le lecteur physique, jamais par un lien web", "Urgence sur un outil métier critique"] },
    { "id": "mail_5", "from_name": "Direction des Soins — Poitier", "from_address": "direction-soins@hopital-poitier.fr", "subject": "Planning des astreintes — semaine 17", "preview": "Bonjour, vous trouverez le planning des astreintes...", "sent_at": "Hier 14:05", "is_dangerous": false, "body": "Bonjour,\n\nVeuillez trouver le planning des astreintes de la semaine 17. Merci de confirmer votre disponibilité avant vendredi.\n\nDirection des Soins", "red_flags": [] },
    { "id": "mail_6", "from_name": "Assurance Maladie", "from_address": "securite@cpam-verification.com", "subject": "Remboursement suspendu : vérifiez vos coordonnées bancaires", "preview": "Un remboursement de 1 430,20 € est en attente. Confirmez votre RIB...", "sent_at": "Aujourd'hui 11:47", "is_dangerous": true, "body": "Bonjour,\n\nUn remboursement de 1 430,20 € vous est dû. Pour le percevoir, confirmez votre RIB :\n\n→ http://cpam-verification.com/rib\n\nCPAM", "red_flags": ["Domaine 'cpam-verification.com' (le vrai domaine est ameli.fr)", "Montant précis pour crédibiliser", "Demande de coordonnées bancaires par lien email — jamais pratiqué par la CPAM", "Aucune personnalisation du destinataire"] }
  ]
}
"""
        });

        // C4.02 — Décryptage d'un phishing (multichoice)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C402Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module4Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Phishing",
            Title        = "Faux message Doctolib — décryptage",
            Instructions = "Un praticien de Poitier reçoit ce message. Identifiez tous les éléments qui prouvent qu'il s'agit d'un phishing.",
            Difficulty   = 1,
            Points       = 150,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Doctolib — Support Praticien",
    "from_address": "support@doctolib-verif.fr",
    "to": "dr.bernard@hopital-poitier.fr",
    "subject": "⚠️ Accès praticien bloqué — réactivation requise",
    "sent_at": "Aujourd'hui 07:58",
    "body": "Bonjour Docteur,\n\nSuite à plusieurs tentatives de connexion échouées, l'accès à votre agenda a été bloqué.\n\nPour éviter la perte de vos rendez-vous, réactivez votre compte dans les 2 heures :\n\n→ http://doctolib-verif.fr/reactivation?token=8f72ka91\n\nÀ confirmer :\n• Identifiant praticien\n• Mot de passe actuel\n• Numéro RPPS\n\nSupport Doctolib"
  },
  "question": "Quels éléments prouvent que cet email est un phishing ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Le domaine 'doctolib-verif.fr' n'est pas le domaine officiel (doctolib.fr)",        "is_correct": true,  "explanation": "Le domaine officiel est doctolib.fr. Tout ajout comme '-verif' ou '-secu' est un lookalike typique." },
    { "id": "B", "label": "La demande de saisir le mot de passe actuel dans un formulaire web externe",         "is_correct": true,  "explanation": "Aucun éditeur sérieux ne demande votre mot de passe par email. La réinitialisation se fait dans l'application." },
    { "id": "C", "label": "L'urgence de 2 heures et la menace sur les rendez-vous patients",                    "is_correct": true,  "explanation": "Urgence extrême + menace sur un enjeu métier fort = leviers d'ingénierie sociale les plus utilisés." },
    { "id": "D", "label": "L'email s'adresse nommément au Docteur, donc il est forcément authentique",          "is_correct": false, "explanation": "Le nom du praticien est public (annuaire RPPS). La personnalisation ne prouve rien." }
  ],
  "red_flags": [
    "Domaine lookalike doctolib-verif.fr (pas doctolib.fr)",
    "Demande du mot de passe actuel — jamais légitime",
    "Urgence de 2 heures + menace sur l'agenda patients",
    "Demande du numéro RPPS (donnée ciblée pour usurpation)",
    "Token aléatoire dans l'URL pour tracer la victime",
    "Emoji dans le sujet pour capter l'attention"
  ],
  "savoir_plus": "Les éditeurs métier santé (Doctolib, Maiia, Keldoc) sont des cibles privilégiées : un accès praticien ouvre l'agenda et les données patients. En cas de doute, se reconnecter via l'URL tapée à la main."
}
"""
        });

        // C4.03 — Faux Ameli Pro, analyse libre (phishing_ai)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C403Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module4Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Phishing",
            Title        = "Faux Ameli Pro — analyse libre",
            Instructions = "Vous êtes infirmier(ère) coordinateur(trice) à l'hôpital Poitier. Vous recevez cet email. Rédigez votre analyse et la conduite à tenir.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Ameli Pro",
    "from_address": "notification@ameli-pro-maj.fr",
    "subject": "Mise à jour de sécurité Ameli Pro — action sous 48h",
    "body": "Bonjour,\n\nDans le cadre du renforcement de la sécurité des comptes professionnels de santé, une mise à jour de votre espace Ameli Pro est obligatoire.\n\nConnectez-vous via le portail sécurisé ci-dessous dans les 48h pour éviter la suspension de vos téléservices :\n\n→ https://ameli-pro-maj.fr/mise-a-jour\n\nIdentifiants requis : numéro RPPS + mot de passe Ameli Pro + code reçu par SMS.\n\nL'équipe Ameli Pro"
  },
  "question": "Expliquez pourquoi cet email est suspect, quels éléments le trahissent, et quelle est la procédure exacte à suivre dans votre établissement.",
  "expected_elements": "Identification du domaine lookalike 'ameli-pro-maj.fr' (le vrai domaine est ameli.fr / amelipro.ameli.fr). Demande d'identifiants professionnels + code SMS = tentative de contournement de l'authentification multifacteur (MFA). Menace de suspension des téléservices = urgence artificielle. Ne pas cliquer, ne pas transférer. Signaler au RSSI / DSI / DPO de l'hôpital Poitier. Vérifier en se connectant via l'URL tapée manuellement. Signalement possible sur signal-spam.fr ou phishing-initiative.fr.",
  "min_chars": 120,
  "hint": "Regardez le domaine, ce qui est demandé, et réfléchissez à qui prévenir dans l'établissement."
}
"""
        });

        // ═══ MODULE 5 — Arnaque & fraude en santé (président, fournisseurs, rançon) ═══

        // C5.01 — Fraude au président / virement urgent (ceo_fraud)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C501Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module5Id,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Ingénierie Sociale",
            Title        = "Virement urgent du directeur",
            Instructions = "Vous êtes au service comptabilité de l'hôpital Poitier. Vous recevez cet email urgent. Choisissez la ou les bonnes réactions.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Directeur — Hôpital Poitier",
    "from_address": "direction@hopital-poitier-direction.com",
    "to": "comptabilite@hopital-poitier.fr",
    "subject": "Virement urgent — confidentiel",
    "sent_at": "Aujourd'hui 13:52",
    "body": "Bonjour,\n\nJe suis en déplacement, injoignable par téléphone jusqu'à ce soir.\n\nJ'ai besoin que vous effectuiez un virement urgent de 38 500 € pour finaliser l'achat d'un équipement d'imagerie. C'est strictement confidentiel — n'en parlez pas au DAF, je lui expliquerai.\n\nIBAN : BE68 5390 0754 7034\nBénéficiaire : MedImaging Trading\n\nMerci d'agir vite, l'offre expire ce soir.\n\nLa Direction"
  },
  "choices": [
    { "id": "pay",     "label": "Effectuer le virement, la Direction en a besoin",                 "icon": "bank",  "is_correct": false, "explanation": "C'est exactement le piège de la fraude au président : urgence + confidentialité + injoignabilité. Ne jamais virer sur simple email." },
    { "id": "report",  "label": "Signaler au DAF et au responsable hiérarchique",                  "icon": "flag",  "is_correct": true,  "explanation": "L'injonction de secret vise à empêcher la vérification croisée. Signaler brise le mécanisme." },
    { "id": "verify",  "label": "Appeler le directeur sur son numéro interne connu avant tout",     "icon": "phone", "is_correct": true,  "explanation": "Vérifier par un canal indépendant (numéro connu, pas celui de l'email) est la règle d'or." },
    { "id": "nothing", "label": "Ne rien faire et supprimer l'email",                              "icon": "x",     "is_correct": false, "explanation": "Supprimer ne protège pas l'établissement : l'attaquant relancera ou ciblera un collègue. Il faut signaler." }
  ],
  "red_flags": [
    "Domaine 'hopital-poitier-direction.com' inventé (pas le domaine réel)",
    "Demande de confidentialité vis-à-vis du DAF = levier classique",
    "Injoignabilité téléphonique invoquée pour empêcher la vérification",
    "Urgence (offre expirant le soir même)",
    "IBAN étranger (Belgique) pour un fournisseur jamais référencé",
    "Procédure d'achat habituelle totalement court-circuitée"
  ]
}
"""
        });

        // C5.02 — Faux fournisseur / changement de RIB (multichoice)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C502Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module5Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Fraude",
            Title        = "Changement de RIB d'un fournisseur",
            Instructions = "Un fournisseur habituel demande de modifier ses coordonnées bancaires. Identifiez les bonnes pratiques.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "MedFournitures — Comptabilité",
    "from_address": "compta@medfournitures-update.com",
    "to": "comptabilite@hopital-poitier.fr",
    "subject": "Changement de coordonnées bancaires — à appliquer",
    "sent_at": "Aujourd'hui 10:20",
    "body": "Bonjour,\n\nSuite à un changement de banque, merci de mettre à jour notre RIB pour les prochaines factures :\n\nNouvel IBAN : FR76 3000 1000 9999 0000 0000 199\n\nMerci d'appliquer ce changement dès aujourd'hui pour éviter tout retard de règlement.\n\nMedFournitures"
  },
  "question": "Quelles sont les bonnes pratiques face à cette demande de changement de RIB ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Vérifier par un canal connu (téléphone du contact habituel) avant tout changement",          "is_correct": true,  "explanation": "La fraude au faux fournisseur repose sur un email usurpé. Le contre-appel via un numéro connu est la parade essentielle." },
    { "id": "B", "label": "Appliquer immédiatement le nouvel IBAN pour éviter un retard de paiement",                  "is_correct": false, "explanation": "Appliquer sans vérifier, c'est virer les futures factures sur le compte de l'escroc. Jamais sans contrôle." },
    { "id": "C", "label": "Repérer le domaine 'medfournitures-update.com' différent du domaine habituel du fournisseur", "is_correct": true,  "explanation": "Un domaine légèrement modifié est un signal fort de fraude au changement de RIB." },
    { "id": "D", "label": "Soumettre le changement à la procédure interne de validation à double contrôle",            "is_correct": true,  "explanation": "Un changement de RIB doit suivre une procédure formelle avec double validation, jamais sur le seul email." }
  ],
  "red_flags": [
    "Domaine modifié 'medfournitures-update.com'",
    "Urgence pour appliquer le changement le jour même",
    "Changement de coordonnées bancaires par simple email",
    "Pas de justificatif officiel ni de contact direct vérifié"
  ],
  "savoir_plus": "La fraude au faux fournisseur (ou fraude au changement de RIB) cible massivement les services comptables. Tout changement d'IBAN doit être confirmé par contre-appel et validé en double contrôle."
}
"""
        });

        // C5.03 — Rançongiciel : email piégé (phishing_ai)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C503Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module5Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Rançongiciel",
            Title        = "La pièce jointe 'Facture' — analyse libre",
            Instructions = "Vous recevez cet email avec une pièce jointe. Rédigez votre analyse du risque et la conduite à tenir.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Service Facturation",
    "from_address": "facturation@medfournitures-fr.com",
    "subject": "Facture impayée n°2026-4471 — relance",
    "body": "Bonjour,\n\nVotre facture n°2026-4471 demeure impayée. Veuillez consulter le détail dans le document joint et procéder au règlement sous 48h pour éviter des pénalités.\n\nPièce jointe : Facture_2026-4471.zip (contient un fichier 'Facture.exe')\n\nCordialement,\nService Facturation"
  },
  "question": "Expliquez pourquoi cet email est dangereux (risque de rançongiciel), ce qui le trahit, et la conduite exacte à tenir.",
  "expected_elements": "Pièce jointe .zip contenant un exécutable 'Facture.exe' = vecteur classique de rançongiciel ; une facture n'est jamais un .exe. Domaine expéditeur à vérifier ('medfournitures-fr.com'). Urgence et menace de pénalités = pression. Ne PAS ouvrir la pièce jointe, ne pas activer les macros, ne pas cliquer. Signaler immédiatement à la DSI / RSSI. Ne pas transférer. En cas d'ouverture accidentelle : déconnecter le poste du réseau et alerter sans éteindre. Un rançongiciel peut chiffrer le SI et bloquer les soins.",
  "min_chars": 120,
  "hint": "Pourquoi une 'facture' au format .exe dans un .zip est-elle un piège ?"
}
"""
        });

        // C5.04 — Synthèse : trois fraudes (free_text)
        await UpsertChallengeAsync(db, new Challenge
        {
            Id           = C504Id,
            TenantId     = PoitierTenantId,
            ModuleId     = Module5Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Analyse de situation",
            Title        = "Trois fraudes en milieu hospitalier — que faites-vous ?",
            Instructions = "Trois mises en situation tirées du quotidien de l'hôpital. Rédigez votre raisonnement, l'IA évaluera la pertinence.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 4,
            Status       = "published",
            CreatedBy    = AuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "questions": [
    {
      "id": "q1",
      "question": "Le service comptabilité reçoit un appel d'une personne se présentant comme votre banque, affirmant qu'une fraude est en cours et qu'il faut 'sécuriser' les comptes en validant des virements qu'elle va vous dicter. Que faites-vous ?",
      "context": "fraude au faux conseiller bancaire",
      "expected_elements": "Ne jamais valider de virement ni communiquer de code dicté au téléphone. Une vraie banque ne demande jamais cela. Raccrocher et rappeler la banque via le numéro officiel connu. Signaler en interne (DAF, RSSI). Sensibiliser l'équipe. Vérification par canal indépendant systématique.",
      "min_chars": 100,
      "hint": "Qui appelle vraiment, et par quel canal vérifier ?"
    },
    {
      "id": "q2",
      "question": "Un agent ouvre par erreur une pièce jointe piégée et son poste affiche soudain que des fichiers sont 'chiffrés' avec une demande de rançon. Quelles sont les premières actions ?",
      "context": "rançongiciel en cours",
      "expected_elements": "Isoler le poste : le déconnecter du réseau (câble / Wi-Fi) SANS l'éteindre pour préserver les traces. Ne pas payer la rançon. Alerter immédiatement la DSI / le RSSI et la hiérarchie via la procédure d'incident. Basculer sur les procédures dégradées (plan de continuité) pour les soins. Ne pas supprimer de fichiers. Tracer les actions.",
      "min_chars": 100,
      "hint": "Limiter la propagation tout en préservant les preuves."
    },
    {
      "id": "q3",
      "question": "Vous recevez un email du 'directeur' demandant l'achat urgent et confidentiel de cartes cadeaux pour 'récompenser une équipe', à envoyer par photo des codes. Comment réagissez-vous ?",
      "context": "fraude au président / arnaque aux cartes cadeaux",
      "expected_elements": "Reconnaître l'arnaque aux cartes cadeaux (variante de la fraude au président) : urgence, confidentialité, demande inhabituelle, paiement non traçable. Ne rien acheter, ne pas envoyer de codes. Vérifier directement auprès du directeur via un canal connu. Signaler au RSSI/DAF. Sensibiliser. Aucune dépense hors procédure d'achat.",
      "min_chars": 100,
      "hint": "Pourquoi des cartes cadeaux et la confidentialité sont-elles des signaux d'alerte ?"
    }
  ]
}
"""
        });
    }

    // ════════════════════════ Helpers idempotents ════════════════════════════
    // FindAsync(id) -> Add si absent / Update des champs si présent. Pattern identique
    // à CatalogSeedBase, mais local (parcours privé tenant, pas de mécanisme catalogue).

    private static async Task UpsertPathAsync(AppDbContext db, LearningPath p)
    {
        var existing = await db.Paths.FindAsync(p.Id);
        if (existing is null)
        {
            db.Paths.Add(p);
        }
        else
        {
            existing.TenantId         = p.TenantId;
            existing.Type             = p.Type;
            existing.Title            = p.Title;
            existing.Description      = p.Description;
            existing.Level            = p.Level;
            existing.Status           = p.Status;
            existing.Version          = p.Version;
            existing.IsCatalog        = p.IsCatalog;
            existing.Sector           = p.Sector;
            existing.EstimatedMinutes = p.EstimatedMinutes;
            existing.Tags             = p.Tags;
            existing.PublishedAt      = p.PublishedAt;
        }
        await db.SaveChangesAsync();
    }

    private static async Task UpsertModuleAsync(AppDbContext db, Module m)
    {
        var existing = await db.Modules.FindAsync(m.Id);
        if (existing is null)
        {
            db.Modules.Add(m);
        }
        else
        {
            existing.TenantId  = m.TenantId;
            existing.PathId    = m.PathId;
            existing.Title     = m.Title;
            existing.SortOrder = m.SortOrder;
        }
        await db.SaveChangesAsync();
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
            existing.TenantId     = c.TenantId;
            existing.ModuleId     = c.ModuleId;
            existing.Type         = c.Type;
            existing.Title        = c.Title;
            existing.Instructions = c.Instructions;
            existing.Category     = c.Category;
            existing.Difficulty   = c.Difficulty;
            existing.Points       = c.Points;
            existing.SortOrder    = c.SortOrder;
            existing.Status       = c.Status;
            existing.ContentType  = c.ContentType;
            existing.ContentJson  = c.ContentJson;
            existing.CorrectAnswer = c.CorrectAnswer;
        }
        await db.SaveChangesAsync();
    }
}
