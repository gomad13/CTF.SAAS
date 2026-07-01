using CTF.Api.Models;

namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Parcours catalogue — Sécurité des emails et communications professionnelles.
/// 2 modules, 8 challenges, niveau intermédiaire, secteur cyber-general.
/// </summary>
public static class Parcours05_EmailsSecurise
{
    private static readonly Guid PathId    = Guid.Parse("c0000005-0000-0000-0000-000000000000");
    private static readonly Guid Module1Id = Guid.Parse("c0000005-0001-0000-0000-000000000000");
    private static readonly Guid Module2Id = Guid.Parse("c0000005-0002-0000-0000-000000000000");

    // Module 1 — Détection de fraudes email
    private static readonly Guid C11Id = Guid.Parse("c0000005-0001-0001-0000-000000000000");
    private static readonly Guid C12Id = Guid.Parse("c0000005-0001-0002-0000-000000000000");
    private static readonly Guid C13Id = Guid.Parse("c0000005-0001-0003-0000-000000000000");
    private static readonly Guid C14Id = Guid.Parse("c0000005-0001-0004-0000-000000000000");

    // Module 2 — Canaux internes & pièces jointes
    private static readonly Guid C21Id = Guid.Parse("c0000005-0002-0001-0000-000000000000");
    private static readonly Guid C22Id = Guid.Parse("c0000005-0002-0002-0000-000000000000");
    private static readonly Guid C23Id = Guid.Parse("c0000005-0002-0003-0000-000000000000");
    private static readonly Guid C24Id = Guid.Parse("c0000005-0002-0004-0000-000000000000");

    public static async Task SeedAsync(AppDbContext db, DateTime now)
    {
        await CatalogSeedBase.UpsertPathAsync(db, new LearningPath
        {
            Id               = PathId,
            TenantId         = CatalogSeedBase.CatalogTenantId,
            Type             = "catalog",
            Title            = "Sécurité des emails et communications professionnelles",
            Description      = "Détecter les emails frauduleux, comprendre SPF/DKIM/DMARC, sécuriser messagerie et canaux internes (Slack, Teams). Un parcours pratique pour limiter la surface d'attaque email.",
            Level            = "intermediate",
            Status           = "published",
            Version          = 1,
            IsCatalog        = true,
            Sector           = "cyber-general",
            EstimatedMinutes = 27,
            Tags             = "email,phishing,spoofing,spf-dkim-dmarc,teams,slack,intermediaire",
            CreatedBy        = CatalogSeedBase.CatalogAuthorId,
            CreatedAt        = now,
            PublishedAt      = now
        });

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = Module1Id,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Détection de fraudes email",
            SortOrder = 1,
            CreatedAt = now
        });

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = Module2Id,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Canaux internes & pièces jointes",
            SortOrder = 2,
            CreatedAt = now
        });

        await SeedModule1Async(db, now);
        await SeedModule2Async(db, now);

        await CatalogSeedBase.EnsureDemoAccessAsync(db, PathId, now);
    }

    // ────────────────────────────────────────────────────────────────────────
    // MODULE 1 — Détection de fraudes email
    // ────────────────────────────────────────────────────────────────────────
    private static async Task SeedModule1Async(AppDbContext db, DateTime now)
    {
        // C1.1 — Boîte piégée : homograph + reply-chain hijacking (mailbox)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C11Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "Boîte piégée : la matinée de Julie",
            Instructions = "Julie Rémi, comptable chez Alverna SAS, ouvre sa messagerie un lundi matin. Cochez uniquement les emails réellement dangereux. Attention aux fausses évidences.",
            Difficulty   = 2,
            Points       = 150,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "emails": [
    {
      "id": "mail_1",
      "from_name": "TramX Logistics — Facturation",
      "from_address": "facturation@trаmx-logistics.com",
      "subject": "Re: Facture FR-2026-0418 — mise à jour IBAN",
      "preview": "Bonjour Julie, suite à notre échange, merci de noter le nouveau RIB...",
      "sent_at": "Aujourd'hui 08:42",
      "is_dangerous": true,
      "body": "Bonjour Julie,\n\nSuite à notre échange de la semaine dernière concernant la facture FR-2026-0418 (14 280 € HT), merci de bien vouloir enregistrer notre nouveau RIB en remplacement de l'ancien :\n\nIBAN : LT12 3456 7890 1234 5678\nBIC : REVOLT21\nBénéficiaire : TramX Logistics SRL\n\nLe précédent compte a été clôturé suite à notre changement de banque. Merci de procéder au règlement sur ce nouvel IBAN avant le 30 du mois.\n\nCordialement,\nService Facturation\nTramX Logistics",
      "red_flags": [
        "Attaque homograph : le 'a' de 'trаmx' est un caractère cyrillique (U+0430), pas un 'a' latin",
        "Changement d'IBAN par email sans appel de confirmation téléphonique",
        "IBAN lituanien (LT) alors que TramX est un prestataire habituel connu",
        "Le 'Re:' simule une reprise de conversation jamais existante",
        "BIC Revolut typique des fraudes BEC (compte mule)",
        "Urgence modérée (fin de mois) pour forcer la validation"
      ]
    },
    {
      "id": "mail_2",
      "from_name": "Martin Keller — CTO",
      "from_address": "m.keller@alverna.fr",
      "subject": "Point budget IT Q2 — jeudi 10h",
      "preview": "Salut Julie, on fait le point budget Q2 jeudi matin ?",
      "sent_at": "Aujourd'hui 09:05",
      "is_dangerous": false,
      "body": "Salut Julie,\n\nComme convenu en comité de direction, on fait le point budget IT Q2 jeudi à 10h en salle Nebula. Peux-tu préparer la synthèse des engagements Cloud Azure + licences Microsoft ?\n\nMerci,\nMartin",
      "red_flags": []
    },
    {
      "id": "mail_3",
      "from_name": "DocuSign",
      "from_address": "dse@docusign-envoi-secure.net",
      "subject": "Vous avez reçu un document à signer : Contrat_NDA_Alverna.pdf",
      "preview": "Cliquez pour examiner et signer le document envoyé par Martin Keller...",
      "sent_at": "Aujourd'hui 09:31",
      "is_dangerous": true,
      "body": "Martin Keller vous a envoyé un document via DocuSign.\n\nDocument : Contrat_NDA_Alverna.pdf\nExpiration : 24h\n\n→ EXAMINER LE DOCUMENT : http://docusign-envoi-secure.net/review?doc=aX82k\n\nCe lien expirera automatiquement. Merci de traiter rapidement.\n\nLa sécurité de votre compte est notre priorité.\nDocuSign Inc.",
      "red_flags": [
        "Domaine non officiel (docusign-envoi-secure.net au lieu de docusign.com / docusign.net)",
        "URL en HTTP et non HTTPS",
        "Urgence fabriquée (expiration 24h)",
        "DocuSign légitime n'envoie jamais depuis un domaine tiers",
        "Pas de code d'accès unique (le vrai DocuSign en fournit toujours un)"
      ]
    },
    {
      "id": "mail_4",
      "from_name": "URSSAF — Déclarations",
      "from_address": "noreply@urssaf.fr",
      "subject": "Confirmation de votre déclaration DSN du mois",
      "preview": "Votre déclaration sociale nominative a bien été réceptionnée...",
      "sent_at": "Hier 17:12",
      "is_dangerous": false,
      "body": "Bonjour,\n\nVotre déclaration sociale nominative (DSN) pour le mois en cours a bien été réceptionnée le 15/04/2026 à 17:08.\n\nNuméro de réception : DSN-2026-04-8374921\nMontant déclaré : 48 720,00 €\n\nVous pouvez consulter le détail depuis votre espace entreprise sur www.urssaf.fr.\n\nCordialement,\nURSSAF",
      "red_flags": []
    },
    {
      "id": "mail_5",
      "from_name": "IT Support Alverna",
      "from_address": "it-support@alverna-helpdesk.com",
      "subject": "Mise à jour obligatoire de votre mot de passe Microsoft 365",
      "preview": "Votre mot de passe expire dans 12 heures. Action requise...",
      "sent_at": "Aujourd'hui 07:48",
      "is_dangerous": true,
      "body": "Bonjour,\n\nLa politique de sécurité d'Alverna SAS exige un renouvellement de votre mot de passe Microsoft 365 dans les 12 prochaines heures. Sans action, votre accès sera suspendu.\n\nMettez à jour maintenant :\n→ https://alverna-helpdesk.com/m365/reset?user=j.remi\n\nRappel : ce lien ne fonctionne qu'une seule fois pour votre compte.\n\nIT Support Alverna",
      "red_flags": [
        "Le vrai domaine IT d'Alverna serait @alverna.fr, pas @alverna-helpdesk.com",
        "Urgence artificielle (12h) typique du credential phishing",
        "Microsoft 365 utilise les portails officiels microsoft.com, jamais un domaine tiers",
        "Pré-remplissage du nom d'utilisateur dans l'URL = technique de ciblage",
        "Aucune annonce interne préalable d'une campagne de renouvellement"
      ]
    },
    {
      "id": "mail_6",
      "from_name": "Julie Rémi",
      "from_address": "j.remi@alverna.fr",
      "subject": "Auto-envoi : rappel passage Cabinet Vuillemin 17/04",
      "preview": "Pense à préparer le dossier Q1 pour le cabinet comptable externe...",
      "sent_at": "Il y a 3 jours",
      "is_dangerous": false,
      "body": "Rappel personnel : préparer dossier trimestriel Q1 pour Cabinet Vuillemin, rendez-vous mercredi 17 avril 14h. Emporter : clôtures Q1, états de rapprochement, registre dépenses R&D.",
      "red_flags": []
    }
  ]
}
"""
        });

        // C1.2 — Homograph attack analysé (multichoice)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C12Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Analyse de domaine",
            Title        = "L'œil nu ne suffit pas : attaque homograph",
            Instructions = "Le premier email reçu par Julie semble provenir de TramX Logistics, un prestataire connu. Pourtant quelque chose cloche. Identifiez tous les indices qui révèlent la tentative d'usurpation.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "TramX Logistics — Facturation",
    "from_address": "facturation@trаmx-logistics.com",
    "to": "j.remi@alverna.fr",
    "subject": "Re: Facture FR-2026-0418 — mise à jour IBAN",
    "sent_at": "Aujourd'hui 08:42",
    "body": "Bonjour Julie,\n\nSuite à notre échange de la semaine dernière concernant la facture FR-2026-0418 (14 280 € HT), merci de bien vouloir enregistrer notre nouveau RIB en remplacement de l'ancien :\n\nIBAN : LT12 3456 7890 1234 5678\nBIC : REVOLT21\nBénéficiaire : TramX Logistics SRL\n\nLe précédent compte a été clôturé suite à notre changement de banque. Merci de procéder au règlement sur ce nouvel IBAN avant le 30 du mois.\n\nCordialement,\nService Facturation\nTramX Logistics"
  },
  "question": "Quels indices prouvent que cet email est une fraude ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Le 'a' dans 'trаmx-logistics.com' est un caractère cyrillique (U+0430), pas un 'a' latin — le domaine est une imitation visuelle parfaite", "is_correct": true,  "explanation": "C'est une attaque IDN homograph. Copier-coller l'adresse dans un outil comme Punycode Converter révèle le vrai domaine 'xn--trmx-8cd-logistics.com', totalement distinct de tramx-logistics.com." },
    { "id": "B", "label": "Un changement d'IBAN pour un prestataire connu, annoncé uniquement par email, sans appel de confirmation", "is_correct": true, "explanation": "C'est le schéma type du BEC (Business Email Compromise). Tout changement de coordonnées bancaires doit être validé par un canal indépendant (appel au numéro habituel, jamais celui dans l'email)." },
    { "id": "C", "label": "L'IBAN lituanien (LT) pour un prestataire jusque-là français, avec un BIC Revolut", "is_correct": true, "explanation": "Les IBAN étrangers soudains et les BIC de néobanques (Revolut, Wise, N26) sont massivement utilisés par les fraudeurs comme comptes relais avant vidage rapide vers l'étranger." },
    { "id": "D", "label": "Le 'Re:' dans l'objet signifie que c'est bien une réponse à un email réel, donc l'email est légitime", "is_correct": false, "explanation": "Faux. N'importe qui peut écrire 'Re:' dans un sujet. C'est justement une technique classique pour simuler une conversation existante et faire baisser la garde." },
    { "id": "E", "label": "La présence d'un numéro de facture précis (FR-2026-0418) prouve que l'expéditeur connaît vraiment le dossier", "is_correct": false, "explanation": "Les attaquants reconstituent souvent ces références à partir de factures réelles interceptées (compromission préalable de la boîte mail du prestataire ou fuite publique)." }
  ],
  "red_flags": [
    "Caractère Unicode cyrillique dans le domaine (homograph IDN)",
    "Demande de modification d'IBAN par email uniquement",
    "IBAN étranger + BIC Revolut sur un prestataire historiquement local",
    "Simulation de 'Re:' pour feindre une conversation existante",
    "Urgence modérée (fin de mois) pour désactiver la vigilance",
    "Absence de signature détaillée et de contact téléphonique vérifiable"
  ],
  "savoir_plus": "Les attaques homograph exploitent des caractères visuellement identiques entre alphabets (cyrillique, grec, latin). Parade : dans Outlook, activer l'affichage 'Texte brut' des entêtes pour voir les encodages Punycode (xn--). En entreprise, l'ANSSI recommande d'activer DMARC en mode reject et de filtrer les domaines ressemblants (typosquatting) au niveau de la passerelle mail."
}
"""
        });

        // C1.3 — BEC multi-étapes / whaling CEO (ceo_fraud)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C13Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Business Email Compromise",
            Title        = "Whaling en trois étapes : le CTO compromis",
            Instructions = "Julie reçoit un email de Martin Keller (CTO) apparemment légitime, suivi d'un second email qui insiste. Choisissez toutes les réactions correctes face à cette séquence BEC.",
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
    "from_name": "Martin Keller — CTO Alverna",
    "from_address": "m.keller@alverna.fr",
    "to": "j.remi@alverna.fr",
    "subject": "Confidentiel — acompte prestataire cybersécurité",
    "sent_at": "Aujourd'hui 15:22",
    "body": "Julie,\n\nJe suis en négociation finale avec un prestataire de cybersécurité pour un audit post-incident. La signature doit se faire ce soir avant 18h, sinon on perd le créneau et on doit recommencer dans 6 semaines.\n\nMerci d'effectuer un acompte de 36 500 € aujourd'hui sur :\n\nIBAN : EE38 2200 2210 2014 5685\nBénéficiaire : Sentinel Audit OÜ\n\nJe gère les aspects légaux et contractuels, toi tu gères uniquement le virement. Reste discrète : je n'ai pas encore informé le reste du CODIR, l'incident est sensible.\n\nJe suis en visio dans 5 minutes avec le prestataire, envoie-moi juste la confirmation du virement par SMS au 07 84 ** ** 12 (mon perso, le pro est saturé).\n\nMartin\n— Envoyé depuis mon iPhone"
  },
  "choices": [
    { "id": "pay",     "label": "Effectuer le virement de 36 500 € immédiatement",                                                 "icon": "bank",  "is_correct": false, "explanation": "Erreur fatale. Un virement de 36 500 € sort automatiquement du seuil de double validation interne chez Alverna. Contourner la procédure au nom de l'urgence est la signature du BEC." },
    { "id": "call_pro","label": "Appeler Martin Keller sur son numéro pro habituel (pas celui indiqué dans l'email) pour confirmer","icon": "phone", "is_correct": true,  "explanation": "Exactement. Vérification par canal indépendant = règle d'or face au BEC. Le numéro 'perso' fourni dans l'email est probablement celui de l'attaquant ou un déroutage." },
    { "id": "report",  "label": "Signaler immédiatement au RSSI et au DAF, même si l'email semble authentique",                    "icon": "flag",  "is_correct": true,  "explanation": "Parfait. Le RSSI peut analyser les entêtes SPF/DKIM/DMARC et détecter un spoofing. Le DAF est en mesure de bloquer toute tentative via son autorité hiérarchique." },
    { "id": "reply",   "label": "Répondre à l'email pour demander plus de détails (contrat, devis, etc.)",                          "icon": "x",     "is_correct": false, "explanation": "Mauvaise réaction. Répondre confirme que l'adresse est active et que la cible est réceptive. Les attaquants utilisent ces réponses pour affiner leur ingénierie sociale." }
  ],
  "red_flags": [
    "Urgence extrême non justifiable (signature le soir même)",
    "Demande de discrétion vis-à-vis du reste du CODIR — rupture de procédure",
    "IBAN estonien (EE) pour un prestataire jamais mentionné en réunion",
    "Redirection vers un numéro personnel non répertorié",
    "Mention 'iPhone' pour excuser ton et fautes inhabituelles",
    "Contournement explicite du principe de double validation financière",
    "Pas de devis ni de bon de commande en pièce jointe"
  ]
}
"""
        });

        // C1.4 — Reply-chain hijacking (phishing_ai)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C14Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Conversation piratée",
            Title        = "Reply-chain hijacking : la conversation détournée",
            Instructions = "Un email arrive dans un vrai fil de discussion existant entre Julie et TramX Logistics. Expliquez comment cette technique fonctionne et ce qui trahit la compromission.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 4,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Sophie Delattre — TramX Logistics",
    "from_address": "s.delattre@tramx-logistics.com",
    "to": "j.remi@alverna.fr",
    "subject": "Re: Re: Re: Commande palettes avril — ajustement volumes",
    "sent_at": "Aujourd'hui 11:04",
    "body": "Bonjour Julie,\n\nCf. notre fil en dessous, suite à votre validation des volumes j'aimerais finaliser rapidement. J'ai préparé l'avenant signé en ligne via notre outil sécurisé, pouvez-vous valider avant midi pour qu'on respecte le créneau quai de vendredi ?\n\n→ Ouvrir l'avenant : https://tramx-documents-portal.com/view/av-2026-0418\n\nLe document expire à 13h00, merci de valider vite.\n\nBien à vous,\nSophie Delattre\n—\n> Le 15/04 à 14:22, Julie Rémi <j.remi@alverna.fr> a écrit :\n> Bonjour Sophie, je confirme les volumes ajustés pour la semaine 16, 22 palettes au lieu de 18, merci d'envoyer l'avenant quand vous pouvez...\n> Le 14/04 à 09:11, Sophie Delattre a écrit :\n> Bonjour Julie, suite à notre échange de la semaine dernière, je vous propose d'ajuster...\n"
  },
  "question": "Expliquez ce qu'est le reply-chain hijacking, pourquoi cet email est dangereux malgré la conversation légitime visible en dessous, et ce que Julie doit vérifier concrètement avant d'agir.",
  "expected_elements": "Reply-chain hijacking = compromission préalable d'un compte mail légitime (ici celui de Sophie Delattre) permettant à l'attaquant de s'insérer dans une vraie conversation existante. Vérifier le domaine réel du lien (tramx-documents-portal.com n'est PAS le domaine habituel). Ne pas cliquer sur le lien. Contacter Sophie par téléphone sur son numéro habituel. Analyser les entêtes SPF/DKIM/DMARC. Prévenir le RSSI. Signaler à TramX une possible compromission de boîte mail. Urgence artificielle (13h) classique du phishing. Ne jamais faire confiance à un fil de conversation comme preuve de légitimité.",
  "min_chars": 150,
  "hint": "Un vrai fil de discussion en dessous ne prouve rien sur l'expéditeur actuel. Concentrez-vous sur ce qui est ajouté au-dessus, pas sur l'historique."
}
"""
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // MODULE 2 — Canaux internes & pièces jointes
    // ────────────────────────────────────────────────────────────────────────
    private static async Task SeedModule2Async(AppDbContext db, DateTime now)
    {
        // C2.1 — Pièce jointe .iso / .lnk malveillante (multichoice)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C21Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Pièces jointes",
            Title        = "Le bon de livraison en .iso",
            Instructions = "Julie reçoit un email d'apparence anodine avec une pièce jointe inattendue. Identifiez tous les éléments qui doivent déclencher l'alarme.",
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
    "from_name": "TramX Expéditions",
    "from_address": "expedition@tramx-logistics.com",
    "to": "j.remi@alverna.fr",
    "subject": "Bon de livraison BL-2026-0488 — signature requise",
    "sent_at": "Aujourd'hui 10:17",
    "body": "Bonjour,\n\nMerci de trouver ci-joint le bon de livraison BL-2026-0488 relatif à votre commande du 12 avril.\n\nPour valider la réception, merci d'ouvrir le fichier et de signer électroniquement via l'assistant intégré.\n\nPièce jointe : BL-2026-0488.iso (3,4 Mo)\n\nCordialement,\nService Expéditions\nTramX Logistics"
  },
  "question": "Qu'est-ce qui doit vous alarmer dans cet email ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Le format .iso est totalement inhabituel pour un bon de livraison — ce format est massivement utilisé par les malwares pour contourner les filtres MOTW (Mark of the Web) de Windows", "is_correct": true, "explanation": "Depuis que Microsoft bloque les macros Office par défaut, les attaquants se sont rabattus sur les conteneurs .iso, .img, .vhd, .zip chiffrés qui contournent l'attribut 'fichier téléchargé d'internet'. Un .iso en bon de livraison est à rejeter systématiquement." },
    { "id": "B", "label": "Un bon de livraison légitime arrive habituellement en PDF ou via un portail prestataire, jamais en archive exécutable", "is_correct": true, "explanation": "Exact. Les formats acceptables sont PDF signé, portail web sécurisé, ou EDI. Toute déviation doit être questionnée." },
    { "id": "C", "label": "La mention 'signer électroniquement via l'assistant intégré' suggère qu'un exécutable est embarqué dans le .iso (probablement un .lnk ou .exe déguisé)", "is_correct": true, "explanation": "Schéma classique : le .iso monté comme lecteur contient un fichier .lnk avec une icône PDF. Un double-clic lance une ligne de commande PowerShell qui télécharge le payload (Emotet, IcedID, Qakbot historiquement)." },
    { "id": "D", "label": "Comme l'expéditeur est le domaine habituel tramx-logistics.com, il n'y a aucun risque à ouvrir la pièce jointe", "is_correct": false, "explanation": "Faux. Même depuis un domaine légitime, la pièce jointe peut être malveillante (compte compromis, usurpation SMTP mal filtrée). Le format du fichier prime sur l'expéditeur." }
  ],
  "red_flags": [
    "Pièce jointe en .iso — format anormal pour de la facturation/logistique",
    "Instruction d'ouvrir et exécuter un 'assistant' embarqué",
    "Aucun message précédent annonçant l'envoi du document",
    "Taille 3,4 Mo cohérente avec un payload malware, pas avec un simple PDF",
    "Absence de signature personnalisée (juste 'Service Expéditions')"
  ],
  "savoir_plus": "Règle mail security : bloquer au niveau passerelle (Exchange Online Protection, Proofpoint, Mailinblack) les extensions .iso, .img, .vhd, .vhdx, .lnk, .iso.gz, .ace, .arj, .r00 en pièces jointes entrantes. Côté poste, activer l'AppLocker pour interdire l'exécution depuis %TEMP% et lecteurs montés. Ces deux mesures neutralisent 90 % des campagnes observées depuis 2023."
}
"""
        });

        // C2.2 — Fausse facture Word avec macro (mailbox)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C22Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Macros Office",
            Title        = "Inbox compta : facture, devis, macro",
            Instructions = "Trois emails arrivent en comptabilité avec des documents Office. Cochez uniquement ceux qui représentent un risque réel d'exécution de macro malveillante.",
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
      "from_name": "Comptabilité Fournisseur",
      "from_address": "billing@invoices-delivery-secure.net",
      "subject": "Facture impayée #INV-88421 — document protégé",
      "preview": "Facture en pièce jointe. Ouvrez et activez la modification pour décrypter...",
      "sent_at": "Aujourd'hui 11:45",
      "is_dangerous": true,
      "body": "Bonjour,\n\nVous avez une facture impayée ci-jointe au format Word.\n\nLe document est protégé. À l'ouverture, cliquez sur 'Activer la modification' puis 'Activer le contenu' pour déchiffrer le contenu de la facture.\n\nFacture_INV-88421.docm (312 Ko)\n\nMerci de régler sous 5 jours pour éviter des pénalités.\n\nService recouvrement",
      "red_flags": [
        "Extension .docm = Word avec macros activables (vecteur historique Emotet / Qakbot)",
        "Instruction explicite d'activer le contenu = activation de la macro",
        "Domaine invoices-delivery-secure.net sans lien avec un vrai fournisseur",
        "Prétexte de 'chiffrement' pour forcer l'activation des macros",
        "Aucune référence d'émetteur vérifiable (pas de SIRET, pas de contact)"
      ]
    },
    {
      "id": "mail_2",
      "from_name": "Cabinet Vuillemin",
      "from_address": "contact@cabinet-vuillemin.fr",
      "subject": "Attestation honoraires Q1 — Alverna SAS",
      "preview": "Bonjour, veuillez trouver ci-joint l'attestation d'honoraires pour le Q1 2026...",
      "sent_at": "Hier 14:20",
      "is_dangerous": false,
      "body": "Bonjour Julie,\n\nVeuillez trouver ci-joint l'attestation d'honoraires concernant nos prestations du premier trimestre 2026.\n\nAttestation_Q1_2026.pdf (98 Ko)\n\nN'hésitez pas à me contacter pour toute question.\n\nCordialement,\nCécile Vuillemin\nCabinet Vuillemin — Expertise comptable\n01 44 58 ** **",
      "red_flags": []
    },
    {
      "id": "mail_3",
      "from_name": "Sophie Delattre — TramX",
      "from_address": "s.delattre@tramx-logistics.com",
      "subject": "Devis mai — ajustement tarifs",
      "preview": "Bonjour Julie, cf. devis mai en PJ, on regarde ensemble jeudi...",
      "sent_at": "Aujourd'hui 09:30",
      "is_dangerous": false,
      "body": "Bonjour Julie,\n\nComme discuté hier au téléphone, voici le devis mai avec l'ajustement tarifaire sur les palettes Europe.\n\nDevis_Mai_2026.xlsx (54 Ko)\n\nOn valide jeudi en call ?\n\nBien à vous,\nSophie",
      "red_flags": []
    },
    {
      "id": "mail_4",
      "from_name": "Finance Department",
      "from_address": "finance@alverna-corporate.co",
      "subject": "Updated bank details — internal transfer",
      "preview": "Please review attached form and sign via macro-enabled wizard...",
      "sent_at": "Aujourd'hui 08:05",
      "is_dangerous": true,
      "body": "Hello,\n\nPlease find attached the updated bank details form for internal transfers. Open the document in Word and enable macros to run the signature wizard.\n\nBankDetails_Update.docm (211 Ko)\n\nDeadline: EOD today.\n\nFinance Department",
      "red_flags": [
        "Domaine alverna-corporate.co n'est pas le domaine officiel (alverna.fr)",
        "Email en anglais dans une entreprise francophone = forte anomalie",
        ".docm + instruction d'activer les macros = kill chain Emotet/Qakbot/Ursnif",
        "Urgence fabriquée (EOD today)",
        "Aucune personnalisation (pas de nom de destinataire)"
      ]
    }
  ]
}
"""
        });

        // C2.3 — Slack invitation phishing + Teams fake user (phishing_ai)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C23Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Canaux de collaboration",
            Title        = "Slack & Teams : le phishing change de terrain",
            Instructions = "Un nouveau 'Martin Keller' apparaît dans Teams externe, et une invitation Slack arrive prétendant venir d'un partenaire. Analysez les risques et donnez le protocole à suivre.",
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
    "from_name": "Slack",
    "from_address": "no-reply@slack-invite-secure.com",
    "to": "j.remi@alverna.fr",
    "subject": "Martin Keller vous a invité(e) à rejoindre l'espace 'Alverna-Finance-Urgent' sur Slack",
    "sent_at": "Aujourd'hui 14:12",
    "body": "Bonjour Julie,\n\nMartin Keller (m.keller@alverna.fr) vous a invité(e) à rejoindre un nouvel espace Slack privé :\n\nEspace : Alverna-Finance-Urgent\nMessage de Martin : 'Julie, on bascule ici le dossier prestataire confidentiel. Plus discret que l'email. Rejoins, je te briefe.'\n\n→ Accepter l'invitation : https://slack-invite-secure.com/join/aX82k\n\nEn parallèle, Julie remarque dans Microsoft Teams qu'un nouvel utilisateur externe 'Martin Keller (Guest)' avec une photo de profil identique à celle du vrai CTO vient d'être ajouté à un canal de discussion partagé avec un prestataire."
  },
  "question": "Expliquez les deux vecteurs d'attaque distincts (fausse invitation Slack + utilisateur externe Teams usurpant le CTO), les risques spécifiques à chacun, et le protocole concret que Julie doit appliquer avant toute interaction.",
  "expected_elements": "Vecteur 1 - fausse invitation Slack : domaine non officiel (slack-invite-secure.com au lieu de slack.com), vraies invitations Slack viennent toujours de @slack.com, technique de bascule hors canaux monitorés (compliance, RSSI), objectif = exfiltration + ingénierie sociale dans environnement moins surveillé. Vecteur 2 - Teams guest usurpation : un utilisateur externe peut définir n'importe quel nom d'affichage, seul le tenant/domaine d'origine est fiable, vérifier le tenant externe dans les propriétés du contact Teams, comparer l'UPN réel avec m.keller@alverna.fr. Protocole Julie : ne pas cliquer sur le lien Slack, vérifier auprès de Martin par canal de confiance (téléphone pro, rencontre physique), signaler au RSSI les deux incidents, demander blocage du guest Teams, vérifier logs d'ajout de l'invité externe (qui l'a ajouté ?), rappeler la politique interne d'usage exclusif des canaux officiels pour les sujets sensibles, ne jamais basculer une discussion critique sur un canal non audité.",
  "min_chars": 180,
  "hint": "Un nom d'affichage n'est pas une identité. Sur Teams comme sur Slack, c'est le domaine / tenant qui fait foi, pas l'avatar."
}
"""
        });

        // C2.4 — Canaux officiels : procédure de validation (free_text)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C24Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Procédures internes",
            Title        = "Rédiger la règle d'or de l'entreprise",
            Instructions = "Martin Keller demande à Julie de rédiger, à destination de tous les employés d'Alverna, les règles concrètes de communication sécurisée. Trois situations à couvrir.",
            Difficulty   = 2,
            Points       = 175,
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
      "question": "Rédigez la procédure obligatoire à appliquer chez Alverna SAS avant tout virement supérieur à 5 000 € demandé par email, quel que soit l'expéditeur.",
      "context": "procédure anti-BEC",
      "expected_elements": "Double validation obligatoire (émetteur + N+1 ou DAF). Vérification systématique par canal indépendant (appel sur numéro pré-enregistré, jamais celui fourni dans l'email). Vérifier toute modification d'IBAN par callback prestataire. Délai de 24h de 'cooling-off' sur tout virement exceptionnel. Aucune urgence ne justifie un contournement. Tracabilité : consigner l'échange de validation dans le ticket. Refus explicite si l'expéditeur insiste sur la discrétion.",
      "min_chars": 120,
      "hint": "Double validation, canal indépendant, cooling-off, traçabilité."
    },
    {
      "id": "q2",
      "question": "Expliquez pourquoi il est dangereux de basculer une conversation professionnelle sensible d'Outlook vers un canal personnel (WhatsApp, Signal, SMS) même à la demande du CTO, et ce que le collaborateur doit faire.",
      "context": "bascule hors canal officiel",
      "expected_elements": "Perte de traçabilité / compliance / archivage légal. Non-conformité RGPD si données personnelles échangées. Absence de DLP et de filtrage antimalware. Contournement des politiques de sécurité d'entreprise. Indicateur fréquent de tentative d'ingénierie sociale (attaquant qui veut sortir du radar). Action : refuser poliment, rappeler la politique, proposer un canal officiel équivalent (Teams privé, Slack interne), signaler au RSSI si l'insistance est suspecte.",
      "min_chars": 120,
      "hint": "Compliance, traçabilité, DLP, et surtout : pourquoi un attaquant adore ce scénario."
    },
    {
      "id": "q3",
      "question": "Un prestataire vous envoie un document Word avec macros activées en prétextant un 'formulaire sécurisé'. Décrivez pourquoi c'est à refuser systématiquement et ce qu'il faut répondre au prestataire.",
      "context": "refus des macros Office",
      "expected_elements": "Macros VBA = vecteur d'exécution de code arbitraire (téléchargement malware, persistance). Microsoft bloque les macros par défaut depuis 2022 sur les fichiers provenant d'internet. Un prestataire légitime n'envoie pas de macros — c'est précisément l'anti-pattern de sécurité. Demander le document en PDF signé ou via portail web. Refuser toute instruction d'activer le contenu / désactiver la protection. Signaler au RSSI et possible campagne massive. Si le prestataire insiste : suspicion de compromission de sa propre boîte, appeler pour vérifier.",
      "min_chars": 120,
      "hint": "Macros = exécution de code. Un prestataire légitime en 2026 n'en envoie plus."
    }
  ]
}
"""
        });
    }
}
