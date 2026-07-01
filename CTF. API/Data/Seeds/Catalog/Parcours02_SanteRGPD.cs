using CTF.Api.Models;

namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Parcours catalogue — Secteur Santé, niveau intermédiaire.
/// "Protection des données patients — RGPD &amp; HDS" : 2 modules, 8 challenges
/// (obligations légales + pratiques quotidiennes).
/// </summary>
public static class Parcours02_SanteRGPD
{
    private static readonly Guid PathId    = Guid.Parse("c0000002-0000-0000-0000-000000000000");
    private static readonly Guid Module1Id = Guid.Parse("c0000002-0001-0000-0000-000000000000");
    private static readonly Guid Module2Id = Guid.Parse("c0000002-0002-0000-0000-000000000000");

    // Module 1 — Obligations RGPD/HDS
    private static readonly Guid M1C1Id = Guid.Parse("c0000002-0001-0001-0000-000000000000");
    private static readonly Guid M1C2Id = Guid.Parse("c0000002-0001-0002-0000-000000000000");
    private static readonly Guid M1C3Id = Guid.Parse("c0000002-0001-0003-0000-000000000000");
    private static readonly Guid M1C4Id = Guid.Parse("c0000002-0001-0004-0000-000000000000");

    // Module 2 — Pratiques quotidiennes
    private static readonly Guid M2C1Id = Guid.Parse("c0000002-0002-0001-0000-000000000000");
    private static readonly Guid M2C2Id = Guid.Parse("c0000002-0002-0002-0000-000000000000");
    private static readonly Guid M2C3Id = Guid.Parse("c0000002-0002-0003-0000-000000000000");
    private static readonly Guid M2C4Id = Guid.Parse("c0000002-0002-0004-0000-000000000000");

    public static async Task SeedAsync(AppDbContext db, DateTime now)
    {
        var path = new LearningPath
        {
            Id               = PathId,
            TenantId         = CatalogSeedBase.CatalogTenantId,
            Type             = "catalog",
            Title            = "Protection des données patients — RGPD & HDS",
            Description      = "Maîtriser les obligations légales (RGPD, certification HDS) et les réflexes pratiques pour protéger le secret médical dans un environnement numérique.",
            Level            = "intermediate",
            Status           = "published",
            Version          = 1,
            IsCatalog        = true,
            Sector           = "sante",
            EstimatedMinutes = 30,
            Tags             = "sante,rgpd,hds,donnees-patient,secret-medical,dpo",
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
            Title     = "Obligations RGPD / HDS",
            SortOrder = 1,
            CreatedAt = now
        });

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = Module2Id,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Pratiques quotidiennes",
            SortOrder = 2,
            CreatedAt = now
        });

        // ═══════════════════════════════════════════════════════════════════
        //  MODULE 1 — Obligations RGPD / HDS
        // ═══════════════════════════════════════════════════════════════════

        // ── M1-C1 — Violation à notifier à la CNIL (multichoice) ───────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M1C1Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "RGPD — Violation",
            Title        = "Fuite de données à l'Hôpital Saint-Jean-de-Dieu",
            Instructions = "Lisez le scénario et identifiez toutes les obligations légales à déclencher.",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Dr Fontaine — DPO Hôpital Saint-Jean-de-Dieu",
    "from_address": "dpo@hopital-stjeandedieu.fr",
    "to": "direction@hopital-stjeandedieu.fr",
    "subject": "Signalement incident — export DPI exposé",
    "sent_at": "Aujourd'hui 08:30",
    "body": "Bonjour,\n\nCe matin, lors d'un audit de routine, nous avons constaté qu'un export du DPI contenant les noms, dates de naissance, numéros de sécurité sociale et motifs d'hospitalisation de 4 200 patients a été déposé pendant 36 heures sur un espace cloud mal configuré, accessible par URL directe sans authentification.\n\nL'accès a été coupé il y a 30 minutes. Les logs d'accès montrent au moins 3 téléchargements externes.\n\nJe vous sollicite pour les suites à donner.\n\nDr Fontaine, DPO"
  },
  "question": "Quelles actions êtes-vous légalement obligés de déclencher ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Notifier la CNIL dans les 72 heures suivant la découverte, via le téléservice dédié",                                  "is_correct": true,  "explanation": "Article 33 RGPD : notification obligatoire dans les 72h à l'autorité de contrôle dès qu'une violation est susceptible d'engendrer un risque pour les personnes. Ici, données de santé + NIR = risque élevé évident." },
    { "id": "B", "label": "Informer individuellement les 4 200 patients concernés dans les meilleurs délais",                                     "is_correct": true,  "explanation": "Article 34 RGPD : communication aux personnes concernées obligatoire quand le risque est élevé — ce qui est le cas pour données de santé + NIR + téléchargements avérés." },
    { "id": "C", "label": "Consigner la violation dans le registre interne tenu par le DPO, même si elle n'était pas à notifier",                 "is_correct": true,  "explanation": "Article 33 §5 RGPD : toutes les violations doivent être documentées, y compris celles non notifiées. Le registre doit être tenu à disposition de la CNIL." },
    { "id": "D", "label": "Gérer l'incident en interne sans communication externe pour préserver la réputation de l'hôpital",                      "is_correct": false, "explanation": "La dissimulation est une infraction additionnelle au RGPD (amende possible : 10 M€ ou 2% du CA mondial) et aggrave considérablement la situation en cas de contrôle ou de plainte patient." }
  ],
  "red_flags": [
    "Données de santé = catégorie particulière (art. 9 RGPD) = protection renforcée",
    "NIR (numéro de sécurité sociale) = données permettant une usurpation d'identité",
    "4 200 personnes = violation d'ampleur significative",
    "Téléchargements externes avérés = le risque s'est déjà matérialisé",
    "72h : délai légal non prolongeable sauf motif justifié",
    "Le DPO doit piloter l'instruction mais la notification engage le responsable de traitement"
  ],
  "savoir_plus": "La CNIL publie chaque année un bilan des violations de santé : le secteur est dans le top 3 des déclarations. Plusieurs CHU ont été sanctionnés financièrement. Le référentiel HDS impose en complément une notification à l'ANS (Agence du Numérique en Santé) et à l'hébergeur certifié."
}
"""
        });

        // ── M1-C2 — Consentement et bases légales (multichoice) ─────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M1C2Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "RGPD — Base légale",
            Title        = "Consentement ou obligation légale ?",
            Instructions = "La Clinique MontSerein réfléchit à la base légale de ses traitements. Aidez la DPO Dr Fontaine à répondre correctement.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Clinique MontSerein — Direction",
    "from_address": "direction@clinique-montserein.fr",
    "to": "dpo@clinique-montserein.fr",
    "subject": "Question : doit-on demander le consentement des patients pour leur dossier médical ?",
    "sent_at": "Aujourd'hui 10:12",
    "body": "Bonjour Dr Fontaine,\n\nNotre cabinet d'avocats nous a fait remarquer que nous n'avons pas de cases à cocher 'J'accepte le traitement de mes données' sur nos formulaires d'admission. Doit-on les ajouter ? Le consentement est-il bien la bonne base légale pour tenir un dossier patient ?\n\nMerci de votre éclairage rapide.\n\nLa Direction"
  },
  "question": "Parmi ces affirmations, lesquelles sont juridiquement correctes ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "La base légale principale du dossier patient est l'exécution d'une mission d'intérêt public / obligation légale, pas le consentement",  "is_correct": true,  "explanation": "Article 6.1.c et 6.1.e + article 9.2.h RGPD : la tenue du dossier patient repose sur l'obligation légale du professionnel de santé et l'exercice de la médecine. Le consentement n'est pas la base appropriée." },
    { "id": "B", "label": "Demander le consentement du patient pour son dossier le rendrait révocable à tout moment, ce qui est incompatible avec l'obligation de conservation",  "is_correct": true,  "explanation": "Exact : le consentement est librement révocable (art. 7 RGPD). Or, le Code de la santé publique impose la conservation du dossier pendant 20 ans. Incompatible." },
    { "id": "C", "label": "En revanche, un consentement spécifique reste requis pour les traitements annexes : recherche, communication à des partenaires, cookies du site",  "is_correct": true,  "explanation": "Exact : pour les finalités secondaires (recherche biomédicale, partage hors équipe de soins, cookies marketing), un consentement explicite et distinct est requis, en plus de l'information générale." },
    { "id": "D", "label": "Il faut ajouter une case 'J'accepte' sur le formulaire d'admission, sinon on est en infraction RGPD",                                     "is_correct": false, "explanation": "Non : ce serait juridiquement incorrect, et même dangereux (patient qui refuse = impossibilité de le soigner correctement). L'information du patient, oui. Le consentement au dossier, non." }
  ],
  "red_flags": [
    "Confusion fréquente consentement ≠ base légale unique",
    "Le RGPD prévoit 6 bases légales, le consentement n'en est qu'une",
    "Code de la santé publique > consentement pour la tenue du dossier",
    "Finalités secondaires = règles distinctes",
    "Le DPO doit documenter la base légale de chaque traitement dans le registre"
  ],
  "savoir_plus": "La CNIL a publié un référentiel 'Traitement de données relatives à la gestion des cabinets médicaux et paramédicaux' qui détaille précisément les bases légales. À connaître pour tout DPO santé."
}
"""
        });

        // ── M1-C3 — Sous-traitant et certification HDS (phishing_ai / libre) ─
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M1C3Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "HDS — Sous-traitance",
            Title        = "MediaSoft, nouveau DPI cloud — que vérifier ?",
            Instructions = "L'éditeur MediaSoft propose un nouveau DPI cloud à la Clinique MontSerein. Le Dr Fontaine (DPO) vous demande d'identifier les points à vérifier avant signature.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "questions": [
    {
      "id": "q1",
      "question": "Quels documents et garanties contractuelles devez-vous exiger de MediaSoft avant de leur confier les données patients de la Clinique MontSerein ?",
      "context": "certification HDS et contrat de sous-traitance",
      "expected_elements": "Certificat HDS (Hébergement de Données de Santé) en cours de validité, couvrant les activités concernées. Contrat de sous-traitance RGPD (art. 28) précisant les finalités, la durée, les mesures de sécurité, les sous-traitants ultérieurs. Localisation des données (UE/hors UE, transferts encadrés). Engagement sur la notification des violations. Clauses de réversibilité et de restitution des données. Droit d'audit. PIA (analyse d'impact) selon l'ampleur du traitement.",
      "min_chars": 150,
      "hint": "Pensez HDS, contrat art. 28, localisation, réversibilité."
    },
    {
      "id": "q2",
      "question": "MediaSoft répond qu'ils 'travaillent avec AWS US' pour 'des raisons de performance'. Quels risques identifiez-vous et quelle position recommander au DPO ?",
      "context": "transferts hors UE et Cloud Act",
      "expected_elements": "Risque de transferts de données vers les États-Unis, soumis au Cloud Act (accès possible par autorités US). Vérifier que le sous-traitant ultérieur (AWS) est bien référencé dans le contrat. Exiger une localisation UE (région européenne AWS) a minima. Privilégier un hébergeur HDS français ou européen souverain (OVHcloud, Outscale, Scaleway). Refuser si pas de garanties, ou a minima analyse d'impact documentée. Alerter le DPO et refuser la contractualisation sans engagement ferme sur la localisation.",
      "min_chars": 150,
      "hint": "Cloud Act, souveraineté, localisation UE, sous-traitants ultérieurs."
    }
  ]
}
"""
        });

        // ── M1-C4 — CEO fraud DPO (ceo_fraud) ──────────────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M1C4Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Ingénierie Sociale",
            Title        = "Le faux DPO Dr Fontaine",
            Instructions = "Vous êtes responsable archives médicales à la Clinique MontSerein. Vous recevez ce mail. Choisissez la ou les bonnes réactions.",
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
    "from_name": "Dr Fontaine — DPO",
    "from_address": "fontaine.dpo@clinique-montserein-dpo.fr",
    "to": "archives@clinique-montserein.fr",
    "subject": "Demande urgente CNIL — extract dossier patient Aubry",
    "sent_at": "Aujourd'hui 15:47",
    "body": "Bonjour,\n\nLa CNIL vient de me notifier une demande d'accès urgente concernant M. Aubry Pierre (dossier patient n°ZX-48192). Je dois leur fournir l'extract complet avant 18h ce soir.\n\nMerci de m'envoyer directement par email (en pièce jointe, au format zip) l'ensemble du dossier médical incluant antécédents, comptes-rendus et résultats d'imagerie.\n\nC'est confidentiel et urgent, ne transférez à personne d'autre, je gère seul l'échange avec la CNIL.\n\nDr Fontaine, DPO"
  },
  "choices": [
    { "id": "pay",     "label": "Zipper le dossier complet et l'envoyer par email comme demandé",            "icon": "bank",  "is_correct": false, "explanation": "C'est exactement le piège : exfiltration complète d'un dossier patient par email non sécurisé. Même vers un 'DPO', cette méthode n'est jamais conforme (ni RGPD, ni HDS)." },
    { "id": "verify",  "label": "Appeler le Dr Fontaine sur son poste interne habituel pour confirmer",       "icon": "phone", "is_correct": true,  "explanation": "Parfait : vérification par canal indépendant. Le domaine 'clinique-montserein-dpo.fr' est lookalike (le vrai domaine est clinique-montserein.fr). Un appel désamorce immédiatement l'attaque." },
    { "id": "report",  "label": "Signaler à la DSI et au vrai DPO avant toute action",                         "icon": "flag",  "is_correct": true,  "explanation": "Excellent : un email prétendant venir du DPO et demandant une exfiltration de dossier est un indicateur fort d'attaque ciblée. Le signalement protège toute la clinique." },
    { "id": "nothing", "label": "Ignorer sans rien dire, l'email part automatiquement au spam plus tard",      "icon": "x",     "is_correct": false, "explanation": "Ne pas signaler laisse l'attaquant relancer, cibler un collègue, ou affiner son scénario. Le signalement est la seule réaction utile à l'échelle de l'établissement." }
  ],
  "red_flags": [
    "Domaine lookalike 'clinique-montserein-dpo.fr' (vrai : clinique-montserein.fr)",
    "Usurpation du DPO : levier d'autorité crédible",
    "Urgence artificielle liée à la CNIL (qui ne fonctionne pas par demandes email)",
    "Demande d'exfiltration par email — jamais conforme HDS",
    "Injonction de confidentialité pour empêcher la vérification croisée",
    "Format zip : tentative d'échapper aux filtres DLP"
  ]
}
"""
        });

        // ═══════════════════════════════════════════════════════════════════
        //  MODULE 2 — Pratiques quotidiennes
        // ═══════════════════════════════════════════════════════════════════

        // ── M2-C1 — Téléconsultation et secret médical (multichoice) ───────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M2C1Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Téléconsultation",
            Title        = "Téléconsultation du Docteur Aubry",
            Instructions = "Le Docteur Aubry téléconsulte depuis chez lui. Identifiez les bonnes pratiques pour préserver le secret médical.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Note d'organisation — Docteur Aubry",
    "from_address": "dr.aubry@cabinet-aubry.fr",
    "to": "secretariat@cabinet-aubry.fr",
    "subject": "Configuration de mes téléconsultations du jeudi",
    "sent_at": "Aujourd'hui 09:28",
    "body": "Bonjour,\n\nPour info sur ma configuration de téléconsultation depuis la maison le jeudi :\n\n• J'utilise mon ordinateur personnel, sur lequel ma famille a aussi ses sessions.\n• Je me connecte au DPI via le WiFi maison, partagé avec les enfants et leurs consoles.\n• Je laisse souvent la porte de mon bureau ouverte, ma conjointe entre parfois pour parler.\n• Je réutilise mon mot de passe Doctolib (celui d'un site de réservation de ski).\n\nDites-moi si ça pose un souci.\n\nDr Aubry"
  },
  "question": "Parmi ces pratiques, lesquelles doivent être corrigées immédiatement ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Utiliser un poste dédié à l'usage professionnel, avec session nominative et chiffrement du disque",            "is_correct": true,  "explanation": "Un poste partagé avec la famille n'offre aucune garantie de confidentialité et expose les données patients à des accès non autorisés. Indispensable de dédier un poste ou a minima une session chiffrée dédiée." },
    { "id": "B", "label": "Passer par un VPN et un canal sécurisé pour accéder au DPI, plutôt que le WiFi domestique en clair",            "is_correct": true,  "explanation": "Un VPN d'entreprise (ou la solution éditeur) chiffre la connexion de bout en bout, quelle que soit la fiabilité du WiFi domestique. C'est indispensable pour respecter le référentiel HDS." },
    { "id": "C", "label": "Assurer une confidentialité sonore et visuelle : pièce fermée, casque, personne d'autre dans la pièce",          "is_correct": true,  "explanation": "Le secret médical couvre aussi ce qu'on entend et voit. Une téléconsultation dans un espace partagé est une violation immédiate du secret, même si la personne présente est un proche." },
    { "id": "D", "label": "Réutiliser son mot de passe Doctolib depuis un site personnel — ça simplifie la mémorisation",                   "is_correct": false, "explanation": "Réutiliser un mot de passe entre un usage personnel et un usage professionnel santé est une des causes majeures de compromission. Un gestionnaire de mots de passe professionnel est obligatoire." }
  ],
  "red_flags": [
    "Poste personnel partagé avec la famille",
    "Réseau WiFi non maîtrisé sans VPN",
    "Espace non isolé pendant la consultation",
    "Mot de passe réutilisé entre site personnel et outil pro",
    "Absence de chiffrement du disque",
    "Aucune politique BYOD encadrée"
  ],
  "savoir_plus": "L'ANS (Agence du Numérique en Santé) publie un guide 'Téléconsultation en toute sécurité' qui détaille l'équipement, le réseau, l'environnement physique et les mesures techniques attendues."
}
"""
        });

        // ── M2-C2 — Mailbox données patient (mailbox) ──────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M2C2Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "Messagerie DPO — Dr Fontaine",
            Instructions = "Vous remplacez le Dr Fontaine (DPO) cette semaine. Cochez uniquement les emails dangereux, suspects ou non conformes à transférer au RSSI.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "emails": [
    { "id": "mail_1", "from_name": "MediaSoft Support", "from_address": "support@mediasoft-dpi-maj.com", "subject": "Mise à jour de sécurité critique — redémarrage du DPI requis", "preview": "Un patch de sécurité critique doit être appliqué manuellement ce soir...", "sent_at": "Aujourd'hui 08:05", "is_dangerous": true, "body": "Bonjour,\n\nUne vulnérabilité critique affecte votre DPI MediaSoft. Téléchargez et exécutez le patch immédiatement :\n\n→ http://mediasoft-dpi-maj.com/patch-urgent.exe\n\nLe patch doit être appliqué manuellement avant ce soir 22h.\n\nSupport MediaSoft", "red_flags": ["Domaine 'mediasoft-dpi-maj.com' (lookalike du vrai éditeur)", "Exécutable .exe téléchargé depuis un lien email", "Un éditeur HDS ne pousse jamais de patch ainsi, il passe par le processus éditeur officiel"] },
    { "id": "mail_2", "from_name": "Me. Lacour — Cabinet d'avocats", "from_address": "contact@lacour-avocats.fr", "subject": "Demande d'accès dossier patient — M. Dupuis (procédure civile)", "preview": "Suite à la procédure en cours, nous sollicitons l'extraction complète...", "sent_at": "Aujourd'hui 09:48", "is_dangerous": true, "body": "Maître,\n\nJe représente M. Dupuis dans une procédure civile. Merci de me transmettre directement par retour de mail le dossier médical complet de mon client.\n\nIl s'agit d'une demande urgente.\n\nCordialement,\nMe. Lacour", "red_flags": ["Un dossier patient ne se transmet jamais par email simple", "Demande d'avocat = passer par la procédure formelle (mandat signé, MSSanté, remise en main propre)", "Urgence artificielle injustifiée"] },
    { "id": "mail_3", "from_name": "CNIL — Service des contrôles", "from_address": "controles@cnil.fr", "subject": "Contrôle sur pièces — Clinique MontSerein", "preview": "Madame, Monsieur, dans le cadre de nos missions de contrôle...", "sent_at": "Hier 15:10", "is_dangerous": false, "body": "Madame, Monsieur,\n\nDans le cadre de nos missions, nous souhaitons planifier un contrôle sur pièces portant sur vos traitements de données patient. Un courrier officiel a été envoyé ce jour au siège.\n\nCordialement,\nLa CNIL — Service des contrôles", "red_flags": [] },
    { "id": "mail_4", "from_name": "Laboratoire BioNordik — Portail DPO", "from_address": "dpo@bionordik.fr", "subject": "Mise à jour du contrat de sous-traitance RGPD — signature demandée", "preview": "Bonjour, veuillez trouver la nouvelle version du contrat art. 28...", "sent_at": "Hier 11:20", "is_dangerous": false, "body": "Bonjour,\n\nDans le cadre de notre partenariat, nous vous transmettons la nouvelle version du contrat de sous-traitance (art. 28 RGPD) via notre portail DPO sécurisé (lien personnel envoyé séparément).\n\nMerci de nous retourner la signature électronique sous 30 jours.\n\nDPO Laboratoire BioNordik", "red_flags": [] },
    { "id": "mail_5", "from_name": "Service Patient — Mme Durand", "from_address": "durand.elise76@gmail.com", "subject": "Demande d'effacement RGPD — droit à l'oubli", "preview": "Bonjour, je demande la suppression totale de mon dossier médical...", "sent_at": "Aujourd'hui 10:35", "is_dangerous": false, "body": "Bonjour,\n\nConformément au RGPD, je souhaite exercer mon droit à l'effacement et demande la suppression totale de mon dossier médical.\n\nCordialement,\nÉlise Durand", "red_flags": [] },
    { "id": "mail_6", "from_name": "URSSAF Pro", "from_address": "cotisations@urssaf-pro-regul.fr", "subject": "Régularisation cotisations — dossier à valider", "preview": "Un solde de 2 840,15 € est en attente. Cliquez pour régulariser...", "sent_at": "Aujourd'hui 07:42", "is_dangerous": true, "body": "Madame, Monsieur,\n\nUn solde de 2 840,15 € est en attente sur votre compte URSSAF Pro. Régularisez avant la fin de semaine :\n\n→ http://urssaf-pro-regul.fr/pay\n\nÀ défaut, majoration automatique.\n\nURSSAF Pro", "red_flags": ["Domaine 'urssaf-pro-regul.fr' (vrai domaine : urssaf.fr)", "Demande de paiement par lien email — l'URSSAF ne fonctionne jamais ainsi", "Montant précis pour crédibiliser", "Urgence artificielle"] }
  ]
}
"""
        });

        // ── M2-C3 — Carte CPS et accès pro (phishing_ai) ────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M2C3Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Phishing",
            Title        = "Faux renouvellement carte CPS",
            Instructions = "Le Docteur Aubry vous transfère ce mail et vous demande votre avis. Rédigez l'analyse que vous lui enverriez.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "ASIP Santé — Renouvellement CPS",
    "from_address": "renouvellement@ameli-pro-maj.fr",
    "subject": "Renouvellement obligatoire de votre carte CPS — délai 7 jours",
    "body": "Docteur,\n\nVotre Carte de Professionnel de Santé (CPS) arrive à expiration. Pour éviter la suspension immédiate de vos téléservices (feuilles de soins, DMP, e-prescription), confirmez vos informations de renouvellement sous 7 jours :\n\n→ http://ameli-pro-maj.fr/cps-renouvellement\n\nInformations demandées :\n• Numéro RPPS complet\n• Mot de passe Ameli Pro\n• Code CPS actuel\n• Scan recto-verso de votre CPS\n• Numéro de sécurité sociale personnel\n\nSans action dans le délai, votre CPS sera désactivée.\n\nCordialement,\nÉquipe ASIP Santé"
  },
  "question": "Rédigez votre analyse complète pour le Dr Aubry : pourquoi cet email est un phishing, quelles données ciblées, quelles conséquences si le Dr Aubry cède, et quelle conduite à tenir.",
  "expected_elements": "Domaine lookalike 'ameli-pro-maj.fr' (la vraie entité est l'Agence du Numérique en Santé, ex-ASIP Santé, qui ne fonctionne pas ainsi). Collecte massive : RPPS + mot de passe + code CPS + scan CPS + NIR personnel = jackpot pour usurpation d'identité professionnelle. Risques : détournement de feuilles de soins, prescriptions frauduleuses, accès au DMP de patients, responsabilité civile et pénale du praticien. Conduite : ne cliquer sur rien, ne rien saisir, ne pas répondre. Signaler à la DSI/RSSI. Signalement à signal-spam.fr et phishing-initiative.fr. Si des informations ont déjà été saisies : changer immédiatement le mot de passe Ameli Pro, contacter le support CPS et le DPO. Les renouvellements CPS passent par le lecteur physique et les canaux officiels ASIP/ANS, jamais par un formulaire web externe.",
  "min_chars": 180,
  "hint": "Pensez : domaine, données collectées, impact métier si compromis, qui alerter."
}
"""
        });

        // ── M2-C4 — Analyse libre : 2 situations HDS (free_text) ────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M2C4Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Analyse de situation",
            Title        = "Pseudonymisation et dossier partagé",
            Instructions = "Deux situations concrètes à trancher. Rédigez votre raisonnement, l'IA évaluera la pertinence et la conformité.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 4,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "questions": [
    {
      "id": "q1",
      "question": "Un chercheur de l'Hôpital Saint-Jean-de-Dieu demande un export de 5 000 dossiers patients pour une étude rétrospective sur le diabète. Il propose de 'juste retirer les noms et prénoms'. Est-ce suffisant ? Expliquez la différence entre anonymisation et pseudonymisation, et ce que vous exigez concrètement.",
      "context": "anonymisation vs pseudonymisation",
      "expected_elements": "Retirer seulement nom/prénom n'est PAS de l'anonymisation : avec la date de naissance, le code postal et le NIR, la ré-identification est triviale. Pseudonymisation : remplacement des identifiants directs par un code, avec table de correspondance gardée séparément — les données restent 'personnelles' au sens RGPD. Anonymisation : suppression irréversible du lien avec la personne, sortie du champ du RGPD. Pour une étude : pseudonymisation + PIA + autorisation CNIL/CESREES si nécessaire, convention de recherche, engagement de confidentialité, cadre loi Jardé ou MR (méthodologie de référence). Refuser un simple export 'nom retiré'.",
      "min_chars": 150,
      "hint": "Définition précise des 2 notions et ce que vous exigez vraiment."
    },
    {
      "id": "q2",
      "question": "Un médecin libéral partage un dossier patient via Google Drive personnel avec un confrère pour un avis spécialisé rapide. Il vous dit 'c'est entre médecins, on a le droit, c'est le secret partagé'. Quels sont les problèmes et quelle procédure conforme lui recommander ?",
      "context": "secret partagé et outils grand public",
      "expected_elements": "Le secret partagé autorise l'échange entre professionnels participant à la prise en charge, mais pas via n'importe quel outil. Google Drive personnel : hébergeur non HDS, soumis au Cloud Act, aucune garantie contractuelle, aucune traçabilité. Violation du cadre HDS + article 28 RGPD (pas de contrat). Alternative conforme : MSSanté (messagerie sécurisée de santé, interopérable entre professionnels), portail DMP, ou solution de télé-expertise agréée. Action immédiate : retirer le partage, documenter l'incident, évaluer avec le DPO s'il s'agit d'une violation à notifier, sensibiliser. Sanction disciplinaire ordinale possible en cas de récidive.",
      "min_chars": 150,
      "hint": "Secret partagé oui, mais par quel canal et sous quelles garanties ?"
    }
  ]
}
"""
        });

        await CatalogSeedBase.EnsureDemoAccessAsync(db, PathId, now);
    }
}
