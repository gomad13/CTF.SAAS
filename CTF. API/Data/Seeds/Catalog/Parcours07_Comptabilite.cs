using CTF.Api.Models;

namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Parcours catalogue 07 — Cybersécurité en comptabilité.
/// Cible les fraudes financières spécifiques aux équipes comptables :
/// faux fournisseur (FOVI), CEO fraud, changement de RIB, fausses factures,
/// arnaque à l'expert-comptable.
/// </summary>
public static class Parcours07_Comptabilite
{
    private static readonly Guid PathId    = Guid.Parse("c0000007-0000-0000-0000-000000000000");
    private static readonly Guid Module1Id = Guid.Parse("c0000007-0001-0000-0000-000000000000");
    private static readonly Guid Module2Id = Guid.Parse("c0000007-0002-0000-0000-000000000000");

    // Module 1 — Fraude au faux fournisseur (FOVI)
    private static readonly Guid C11Id = Guid.Parse("c0000007-0001-0001-0000-000000000000");
    private static readonly Guid C12Id = Guid.Parse("c0000007-0001-0002-0000-000000000000");
    private static readonly Guid C13Id = Guid.Parse("c0000007-0001-0003-0000-000000000000");
    private static readonly Guid C14Id = Guid.Parse("c0000007-0001-0004-0000-000000000000");

    // Module 2 — Fraude interne & arnaque au président
    private static readonly Guid C21Id = Guid.Parse("c0000007-0002-0001-0000-000000000000");
    private static readonly Guid C22Id = Guid.Parse("c0000007-0002-0002-0000-000000000000");
    private static readonly Guid C23Id = Guid.Parse("c0000007-0002-0003-0000-000000000000");
    private static readonly Guid C24Id = Guid.Parse("c0000007-0002-0004-0000-000000000000");

    public static async Task SeedAsync(AppDbContext db, DateTime now)
    {
        await SeedPathAsync(db, now);
        await SeedModulesAsync(db, now);
        await SeedModule1ChallengesAsync(db, now);
        await SeedModule2ChallengesAsync(db, now);
        await CatalogSeedBase.EnsureDemoAccessAsync(db, PathId, now);
    }

    private static async Task SeedPathAsync(AppDbContext db, DateTime now)
    {
        await CatalogSeedBase.UpsertPathAsync(db, new LearningPath
        {
            Id               = PathId,
            TenantId         = CatalogSeedBase.CatalogTenantId,
            Type             = "catalog",
            Title            = "Cybersécurité en comptabilité — Fraudes financières",
            Description      = "Détecter les fraudes ciblant les équipes comptables : faux fournisseur (FOVI), CEO fraud, changement de RIB frauduleux, fausses factures, arnaque à l'expert-comptable.",
            Level            = "intermediate",
            Status           = "published",
            Version          = 1,
            IsCatalog        = true,
            Sector           = "comptabilite",
            EstimatedMinutes = 27,
            Tags             = "comptabilite,fovi,ceo-fraud,fournisseur,rib,facture,intermediaire",
            CreatedBy        = CatalogSeedBase.CatalogAuthorId,
            CreatedAt        = now,
            PublishedAt      = now
        });
    }

    private static async Task SeedModulesAsync(AppDbContext db, DateTime now)
    {
        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = Module1Id,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Fraude au faux fournisseur (FOVI)",
            SortOrder = 1,
            CreatedAt = now
        });

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = Module2Id,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Fraude interne & arnaque au président",
            SortOrder = 2,
            CreatedAt = now
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // MODULE 1 — Fraude au faux fournisseur (FOVI)
    // ────────────────────────────────────────────────────────────────────────
    private static async Task SeedModule1ChallengesAsync(AppDbContext db, DateTime now)
    {
        // C1.1 — Mailbox : inbox d'une comptable, repérer les emails frauduleux (FOVI, fausse URSSAF, etc.)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C11Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "La boîte mail de Nathalie — Service comptabilité",
            Instructions = "Vous êtes Nathalie Pons, comptable senior du cabinet Durand & Associés. Voici votre boîte de réception du matin. Cochez uniquement les emails dangereux ou frauduleux.",
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
    { "id": "mail_1", "from_name": "Papeterie Lorraine — Comptabilité", "from_address": "compta@papeterie-lorraine-fr.com", "subject": "Changement de coordonnées bancaires — à prendre en compte dès maintenant", "preview": "Bonjour, suite à un changement de banque nous vous communiquons notre nouveau RIB...", "sent_at": "Aujourd'hui 08:47", "is_dangerous": true, "body": "Bonjour Nathalie,\n\nJe vous informe que nous avons changé de banque cette semaine. Merci de mettre à jour notre RIB dans votre logiciel pour les prochains règlements, y compris la facture n°2026-0418 (4 320 € TTC) qui arrive à échéance vendredi.\n\nNouveau IBAN : LT12 1000 0111 0100 1000 (Paysera LT)\nBénéficiaire : Papeterie Lorraine\n\nMerci de bien vouloir confirmer la prise en compte par retour de mail.\n\nMarc Thivert\nService Comptabilité — Papeterie Lorraine", "red_flags": ["Domaine expéditeur altéré (papeterie-lorraine-fr.com au lieu du domaine habituel)", "Interlocuteur Marc Thivert inconnu du fournisseur historique", "IBAN lituanien (LT) chez Paysera pour une PME française", "Urgence sur une facture existante pour accélérer la fraude", "Pas de confirmation téléphonique proposée", "Demande de réponse uniquement par email"] },
    { "id": "mail_2", "from_name": "URSSAF Île-de-France", "from_address": "recouvrement@urssaf-idf-paiement.fr", "subject": "Mise en demeure — Solde de cotisations dû 2 187,40 €", "preview": "Vos cotisations du 1er trimestre n'ont pas été réglées dans les délais...", "sent_at": "Aujourd'hui 09:12", "is_dangerous": true, "body": "Madame, Monsieur,\n\nNos services constatent un solde impayé de 2 187,40 € au titre des cotisations sociales du 1er trimestre 2026.\n\nFaute de régularisation sous 48h, une majoration de 10 % sera appliquée et un avis à tiers détenteur sera émis.\n\nRégler en ligne : http://urssaf-idf-paiement.fr/reglement/n8742\n\nRéférence dossier : 26-IDF-88742\n\nService Recouvrement — URSSAF Île-de-France", "red_flags": ["Domaine non officiel (le vrai est urssaf.fr)", "Lien direct de paiement dans un email (l'URSSAF n'en envoie jamais)", "Urgence artificielle (48h)", "Menace de majoration pour pousser au paiement immédiat", "Paiement en ligne demandé hors espace professionnel net-entreprises.fr"] },
    { "id": "mail_3", "from_name": "Maxime Chevalier — PDG", "from_address": "m.chevalier@durand-associes.fr", "subject": "Validation note de frais séminaire Deauville", "preview": "Nathalie, pouvez-vous valider la note de frais du séminaire de septembre...", "sent_at": "Hier 17:22", "is_dangerous": false, "body": "Bonjour Nathalie,\n\nPeux-tu valider la note de frais du séminaire d'équipe à Deauville (dossier partagé habituel, 1 840 € TTC) ? Justificatifs déjà transmis par Camille.\n\nMerci,\nMaxime Chevalier\nPDG — Durand & Associés", "red_flags": [] },
    { "id": "mail_4", "from_name": "Ordre des Experts-Comptables", "from_address": "inscription@ordre-experts-comptables-fr.org", "subject": "Renouvellement tableau 2026 — dernier rappel avant radiation", "preview": "Votre inscription au tableau expire dans 48h. Renouvelez maintenant...", "sent_at": "Aujourd'hui 07:33", "is_dangerous": true, "body": "Cher confrère,\n\nNotre base indique que votre inscription au tableau de l'Ordre 2026 n'a pas été renouvelée. Faute de régularisation sous 48h, votre cabinet sera suspendu du tableau et la publication sera effective au Journal Officiel.\n\nRenouveler maintenant (387 €) :\n→ https://ordre-experts-comptables-fr.org/renouveler?ref=DUR2026\n\nCarte bancaire requise lors du renouvellement.\n\nConseil Supérieur de l'Ordre", "red_flags": ["Domaine frauduleux (le vrai est experts-comptables.fr)", "Menace de radiation pour créer la panique", "Paiement par carte bancaire via lien email", "L'Ordre gère le renouvellement via son espace privé, jamais par lien direct", "Pas d'interlocuteur nommé, pas de numéro d'adhérent"] },
    { "id": "mail_5", "from_name": "Camille Bouvier — Administratif", "from_address": "c.bouvier@durand-associes.fr", "subject": "Classement factures avril 2026", "preview": "Bonjour Nathalie, j'ai terminé le classement des factures d'avril...", "sent_at": "Hier 16:10", "is_dangerous": false, "body": "Bonjour Nathalie,\n\nJ'ai terminé le classement des factures fournisseurs d'avril dans le dossier SharePoint habituel (Compta > 2026 > 04). Rien de bloquant, deux factures en attente d'approbation de Maxime.\n\nBonne soirée,\nCamille Bouvier\nAssistante administrative", "red_flags": [] },
    { "id": "mail_6", "from_name": "Service-Factur-X", "from_address": "noreply@factur-x-portail.net", "subject": "Nouvelle facture reçue — 12 450,00 € TTC (à valider)", "preview": "Une facture a été déposée sur votre portail Factur-X...", "sent_at": "Aujourd'hui 10:04", "is_dangerous": true, "body": "Bonjour,\n\nUne nouvelle facture a été déposée sur votre portail Factur-X pour un montant de 12 450,00 € TTC.\n\nTélécharger la facture :\nhttp://factur-x-portail.net/download/f7841.zip\n\nMerci de la valider sous 24h pour éviter toute relance.\n\nService Factur-X", "red_flags": ["Aucun émetteur identifiable (pas de nom de fournisseur)", "Pièce en .zip — vecteur classique de malware / ransomware", "Domaine non officiel (le vrai portail public est Chorus Pro)", "Montant important utilisé pour attirer un clic", "Pas d'objet/numéro de facture détaillé"] }
  ]
}
"""
        });

        // C1.2 — Multichoice : email de changement de RIB fournisseur historique (FOVI classique)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C12Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "FOVI",
            Title        = "Fausse demande de changement de RIB — Papeterie Lorraine",
            Instructions = "Un email vous demande de modifier l'IBAN de votre fournisseur historique. Identifiez tous les signaux prouvant qu'il s'agit d'une fraude FOVI (Faux Ordre de Virement International).",
            Difficulty   = 2,
            Points       = 175,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Marc Thivert — Papeterie Lorraine",
    "from_address": "m.thivert@papeterie-lorraine-fr.com",
    "to": "n.pons@durand-associes.fr",
    "subject": "URGENT — Mise à jour RIB avant règlement facture 2026-0418",
    "sent_at": "Aujourd'hui 08:47",
    "body": "Bonjour Nathalie,\n\nJe me présente : Marc Thivert, je remplace depuis lundi Sébastien à la comptabilité de Papeterie Lorraine (il est en arrêt prolongé).\n\nNotre banque historique ayant été absorbée suite à fusion, nous vous communiquons notre nouvel IBAN pour tous les règlements à venir, y compris la facture n°2026-0418 (4 320 € TTC) qui arrive à échéance vendredi :\n\nBénéficiaire : PAPETERIE LORRAINE\nIBAN : LT12 1000 0111 0100 1000\nBIC : EVIULT2VXXX (Paysera LT)\n\nMerci de mettre à jour votre logiciel avant le virement. Pourriez-vous me confirmer la prise en compte par retour de mail pour sécuriser le règlement ?\n\nBien cordialement,\nMarc Thivert\nService Comptabilité — Papeterie Lorraine"
  },
  "question": "Quels éléments de cet email prouvent qu'il s'agit d'une fraude FOVI ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "Domaine expéditeur légèrement altéré (papeterie-lorraine-fr.com au lieu du domaine officiel habituel)", "is_correct": true, "explanation": "La comparaison fine du domaine est le premier réflexe. Les fraudeurs achètent des domaines typosquattés (ajout d'un '-fr', d'un .net, etc.) visuellement proches du domaine légitime. Toujours vérifier dans les emails d'archive du vrai fournisseur." },
    { "id": "B", "label": "Nouvel interlocuteur inconnu (Marc Thivert) qui remplace soi-disant le contact habituel", "is_correct": true, "explanation": "Le remplacement subit d'un contact habituel, combiné à une demande bancaire immédiate, est un schéma classique de FOVI. L'attaquant se renseigne sur LinkedIn pour crédibiliser l'histoire. À vérifier par téléphone sur le numéro du répertoire, jamais celui de la signature." },
    { "id": "C", "label": "IBAN lituanien (LT) chez Paysera pour un fournisseur français régional", "is_correct": true, "explanation": "Les banques en ligne transfrontalières (Paysera LT, Revolut, Wise) sont massivement utilisées par les fraudeurs : ouverture rapide, blanchiment plus facile. Un fournisseur PME français conserve quasi systématiquement une banque française." },
    { "id": "D", "label": "Urgence sur une facture déjà connue pour accélérer la validation", "is_correct": true, "explanation": "La FOVI s'accroche à une facture réelle, déjà enregistrée, pour crédibiliser la demande. Le changement de RIB doit suivre une procédure : formulaire signé tamponné, vérification contradictoire par téléphone, validation hiérarchique." },
    { "id": "E", "label": "Une confirmation par retour d'email suffit — c'est un second canal, donc fiable", "is_correct": false, "explanation": "Faux. Répondre au même email ou au même expéditeur n'est PAS un second canal : le fraudeur contrôle toute la conversation. Seule une vérification par téléphone (numéro du répertoire, pas celui de l'email) ou en face-à-face constitue un vrai canal indépendant." }
  ],
  "red_flags": [
    "Domaine expéditeur légèrement altéré (typosquatting)",
    "Nouveau contact jamais rencontré + ancien contact 'parti'",
    "IBAN étranger inattendu (LT, Paysera, banque en ligne)",
    "Urgence liée à une facture réelle connue",
    "Demande de confirmation par email uniquement (pas de téléphone)",
    "Absence de formulaire officiel de changement de RIB"
  ],
  "savoir_plus": "La fraude FOVI (Faux Ordre de Virement International) est la première cause de pertes cyber dans les PME françaises (source : Cybermalveillance.gouv.fr). La règle absolue : tout changement d'IBAN doit être contre-vérifié par téléphone sur le numéro du répertoire historique, JAMAIS sur un numéro fourni dans l'email lui-même."
}
"""
        });

        // C1.3 — Phishing AI : fausse facture avec numéro de compte subtilement modifié
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C13Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Fausse facture",
            Title        = "La facture aux deux IBAN — analyse libre",
            Instructions = "Analysez l'email ci-dessous et son pied de facture. Rédigez une analyse précise des éléments suspects (minimum 100 caractères). Un évaluateur IA notera votre raisonnement.",
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
    "from_name": "Comptabilité — Papeterie Lorraine",
    "from_address": "compta@papeterie-lorraine.fr",
    "to": "n.pons@durand-associes.fr",
    "subject": "Re: Facture 2026-0418 — règlement à venir",
    "sent_at": "Aujourd'hui 14:22",
    "body": "Bonjour Nathalie,\n\nSuite à votre échange ce matin avec notre nouveau collègue, je vous confirme la facture 2026-0418 en pièce jointe. Merci de régler selon les nouvelles coordonnées.\n\nPour rappel, le pied de facture mentionne :\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\nPAPETERIE LORRAINE SARL\nSIRET : 418 724 903 00027\nRèglement par virement :\nIBAN : FR76 1027 8050 0000 0218 4290 188\nBIC : CMCIFR2A\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n(Note : l'IBAN imprimé sur la facture PDF jointe est FR76 1027 8050 0000 0218 4290 155 — merci d'utiliser celui ci-dessus dans le corps du mail, c'est le dernier à jour.)\n\nMerci d'avance,\nPapeterie Lorraine"
  },
  "question": "Décrivez précisément les éléments suspects de cet email et expliquez pourquoi cette situation est caractéristique d'une fraude.",
  "expected_elements": "Deux IBAN différents entre le corps de l'email et la pièce jointe PDF (les trois derniers chiffres diffèrent : 188 vs 155). La demande d'utiliser celui du mail plutôt que celui du PDF est une manipulation typique. Référence à un 'nouveau collègue' non identifié. Absence de procédure formelle de changement de RIB. Recommandation : ne pas payer, appeler le fournisseur sur le numéro du répertoire interne pour confirmer l'IBAN officiel, signaler au responsable comptable, conserver l'email comme preuve.",
  "min_chars": 100,
  "hint": "Comparez attentivement les deux IBAN présents dans le message. Que se passe-t-il si vous regardez les derniers chiffres de chacun ?"
}
"""
        });

        // C1.4 — Free text : fausse visite physique d'un "commissaire aux comptes"
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C14Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Ingénierie sociale",
            Title        = "Le faux commissaire aux comptes & la fausse URSSAF",
            Instructions = "Deux situations concrètes à analyser. Rédigez votre réponse, l'IA évaluera votre raisonnement.",
            Difficulty   = 3,
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
      "question": "Un homme se présente à l'accueil du cabinet Durand & Associés avec une carte professionnelle de 'commissaire aux comptes mandaté par la Compagnie Régionale'. Il demande un accès immédiat aux journaux comptables du client X pour un contrôle 'imprévu'. Quels contrôles faites-vous avant de lui donner le moindre accès ?",
      "context": "fraude par usurpation d'auditeur",
      "expected_elements": "Vérifier l'identité au registre public des CAC sur cncc.fr. Appeler la Compagnie Régionale des Commissaires aux Comptes pour confirmation. Exiger une lettre de mission signée par le client concerné. Ne jamais donner accès immédiat aux journaux ou au logiciel comptable sans validation de l'expert-comptable associé et du client. Conserver une copie de la carte professionnelle. Informer le client du passage.",
      "min_chars": 80,
      "hint": "Pensez : vérification de l'identité, canal officiel, et qui doit autoriser l'accès aux données client."
    },
    {
      "id": "q2",
      "question": "Nathalie reçoit un appel téléphonique d'une femme qui se dit 'contrôleuse URSSAF' et annonce un contrôle sur place la semaine prochaine. Elle demande d'envoyer dès maintenant par email les bulletins de paie des 12 derniers mois et l'accès au portail de paie (login + mot de passe) 'pour gagner du temps'. Que faites-vous et quels sont les signaux d'arnaque ?",
      "context": "fraude à l'expert-comptable / faux contrôle URSSAF",
      "expected_elements": "Un contrôle URSSAF est TOUJOURS précédé d'un avis de passage écrit et recommandé (article R243-59 CSS). L'URSSAF ne demande jamais de mot de passe du logiciel de paie par téléphone ni par email. Ne rien envoyer. Demander nom, matricule, service, puis rappeler au standard officiel URSSAF (3957) pour vérifier. Signaler la tentative à l'expert-comptable et en interne. Les bulletins de paie sont consultés sur place lors du contrôle, pas envoyés à l'avance.",
      "min_chars": 80,
      "hint": "Pensez à la procédure officielle de contrôle URSSAF et à ce qu'aucune administration ne demande jamais par téléphone."
    }
  ]
}
"""
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // MODULE 2 — Fraude interne & arnaque au président
    // ────────────────────────────────────────────────────────────────────────
    private static async Task SeedModule2ChallengesAsync(AppDbContext db, DateTime now)
    {
        // C2.1 — CEO Fraud : email du PDG demandant virement confidentiel
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C21Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Arnaque au président",
            Title        = "L'email de Maxime Chevalier — opération confidentielle",
            Instructions = "Vous recevez un email apparemment envoyé par Maxime Chevalier, PDG de Durand & Associés, demandant un virement urgent et confidentiel. Choisissez la ou les bonnes réactions.",
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
    "from_name": "Maxime Chevalier — PDG",
    "from_address": "maxime.chevalier@durand-associes-direction.fr",
    "to": "n.pons@durand-associes.fr",
    "subject": "[CONFIDENTIEL] Opération d'acquisition — virement à exécuter ce jour",
    "sent_at": "Aujourd'hui, 16h43",
    "body": "Nathalie,\n\nJe suis en ce moment en négociation finale d'une opération d'acquisition stratégique. C'est strictement confidentiel : ni le DAF, ni les associés ne sont au courant à ce stade pour éviter toute fuite.\n\nJ'ai besoin que tu exécutes aujourd'hui avant 17h30 un virement de 94 800 € vers le compte séquestre de notre conseil juridique :\n\nBénéficiaire : CABINET VAN DER MEULEN NOTARIAAT\nIBAN : NL91 ABNA 0417 1643 00\nBIC : ABNANL2A\nMotif : Honoraires — dossier A26-VIP\n\nJe te fais confiance pour la discrétion absolue. Je ne peux pas t'appeler, je suis en visio avec les avocats. Tu recevras le justificatif demain matin.\n\nMerci de me confirmer par mail dès que c'est fait.\n\nMaxime Chevalier\nPDG — Durand & Associés"
  },
  "choices": [
    { "id": "pay", "label": "Exécuter le virement, c'est le PDG et c'est urgent", "icon": "bank", "is_correct": false, "explanation": "C'est exactement le piège de l'arnaque au président. Un PDG légitime ne demande JAMAIS un virement en contournant la procédure (DAF, double signature, formulaire de paiement). L'urgence + confidentialité + absence de hiérarchie = fraude dans 99 % des cas." },
    { "id": "callback", "label": "Appeler Maxime Chevalier sur son numéro habituel pour vérifier de vive voix", "icon": "phone", "is_correct": true, "explanation": "Réflexe essentiel. Utiliser le numéro du répertoire professionnel (JAMAIS un numéro fourni dans l'email). Si le PDG est injoignable, la demande ne peut pas être traitée — c'est aussi simple que ça." },
    { "id": "report", "label": "Alerter immédiatement le DAF et le RSSI (ou le responsable IT) avant toute action", "icon": "flag", "is_correct": true, "explanation": "Bonne réaction. Un email qui demande explicitement de court-circuiter le DAF est un signal d'alerte : le DAF doit TOUJOURS être mis dans la boucle. Signaler également au responsable cybersécurité pour bloquer d'éventuels autres emails du même expéditeur." },
    { "id": "reply", "label": "Répondre à l'email pour demander plus de détails avant de traiter", "icon": "x", "is_correct": false, "explanation": "Dangereux. Répondre à l'email engage la conversation avec le fraudeur, qui contrôle toute la boîte. Le second canal de vérification doit être INDÉPENDANT : téléphone (numéro du répertoire), visite en personne, ou messagerie interne (Teams, Slack) certifiée." }
  ],
  "red_flags": [
    "Domaine expéditeur falsifié (durand-associes-direction.fr ≠ durand-associes.fr)",
    "Confidentialité exigée y compris envers le DAF et les associés",
    "Indisponibilité téléphonique invoquée ('je suis en visio')",
    "Urgence artificielle avec deadline à l'heure près (17h30)",
    "IBAN étranger (NL) pour un soi-disant honoraire juridique",
    "Motif vague ('dossier A26-VIP') sans dossier ouvert en interne",
    "Contournement explicite de la procédure de double validation"
  ]
}
"""
        });

        // C2.2 — Multichoice : deepfake audio du PDG au téléphone
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C22Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Deepfake vocal",
            Title        = "Deepfake audio — la voix du PDG au téléphone",
            Instructions = "Nathalie reçoit un appel téléphonique de Maxime Chevalier. La voix ressemble à la sienne mais plusieurs détails étranges l'interpellent. Identifiez les bonnes réactions.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Appel de Maxime Chevalier — 17h18",
    "context": "Nathalie reçoit un appel d'un numéro masqué. La voix de Maxime Chevalier (reconnaissable, intonations familières) lui dit :\n\n« Nathalie, j'ai besoin de toi tout de suite. Je suis coincé chez un notaire, ma messagerie est bloquée. Il me faut un virement de 62 500 € vers l'IBAN que je t'ai envoyé par SMS à l'instant. C'est pour finaliser le rachat dont je t'ai parlé la semaine dernière. Tu peux faire ça en priorité ? Je te remercie. »\n\nLa ligne est légèrement saccadée. Nathalie n'a jamais entendu parler d'un rachat. Le SMS arrive effectivement d'un numéro inconnu avec un IBAN maltais.",
    "icon": "alert"
  },
  "question": "Que doit faire Nathalie face à cette situation de deepfake vocal ?",
  "choices": [
    { "id": "A", "label": "Raccrocher poliment et rappeler immédiatement Maxime sur son numéro professionnel connu", "is_correct": true, "explanation": "Réflexe clé face à tout appel suspect, même avec une voix reconnaissable. Les deepfakes vocaux (voice cloning) sont aujourd'hui accessibles à tous : quelques minutes d'audio extrait de LinkedIn, YouTube ou de conférences suffisent. Le second canal (rappel sur numéro du répertoire) est l'unique parade fiable." },
    { "id": "B", "label": "Exécuter le virement car la voix est indiscutablement celle du PDG", "is_correct": false, "explanation": "Faux. La voix seule n'est plus une preuve d'identité depuis 2023. Les plateformes de voice cloning (ElevenLabs, Respeecher, etc.) permettent de reproduire une voix en moins d'une minute à partir d'un simple extrait vidéo public. Plusieurs fraudes à 7 chiffres ont été documentées en France en 2024." },
    { "id": "C", "label": "Poser une question personnelle et vérifiable (ex: le prénom du chien de Maxime) avant d'agir", "is_correct": true, "explanation": "Technique de 'code mot' / challenge personnel : très efficace face à un deepfake. Un attaquant qui a cloné la voix n'a pas accès aux anecdotes privées. Mieux encore : avoir un mot de passe verbal convenu à l'avance entre direction et comptabilité." },
    { "id": "D", "label": "Signaler l'appel au DAF, au RSSI et déposer plainte si virement tenté", "is_correct": true, "explanation": "Démarche correcte : même sans virement effectué, la tentative doit être documentée (date, numéro, IBAN fourni, SMS reçu). Signalement obligatoire sur cybermalveillance.gouv.fr et à la plateforme THESEE pour les fraudes financières." },
    { "id": "E", "label": "Rappeler le numéro masqué qui vient d'appeler — c'est sûrement le bon", "is_correct": false, "explanation": "Dangereux. Un numéro masqué ne peut pas être rappelé de manière fiable. Si la ligne revient, elle est probablement maîtrisée par le fraudeur. Toujours passer par les coordonnées de référence (annuaire interne, carte de visite d'origine)." }
  ],
  "red_flags": [
    "Numéro masqué ou inconnu pour un appel de la direction",
    "Voix identifiable mais ligne saccadée (artefacts de synthèse)",
    "SMS complémentaire d'un numéro inconnu avec IBAN",
    "Opération 'rachat' jamais évoquée en interne",
    "IBAN étranger (MT, LT, LU, NL) typique du blanchiment",
    "Pression pour exécuter 'tout de suite, en priorité'"
  ],
  "savoir_plus": "Les fraudes par deepfake vocal ont explosé depuis 2023. Cas documentés : l'arnaque de 25 M$ à Arup Hong Kong (février 2024, deepfake vidéo + audio en visio). Parade : un mot de passe verbal convenu à l'avance entre direction et comptabilité — simple, gratuit, extrêmement efficace."
}
"""
        });

        // C2.3 — Phishing AI : faux expert-comptable demandant accès au logiciel de paie
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C23Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Usurpation expert-comptable",
            Title        = "Faux expert-comptable — accès au logiciel de paie",
            Instructions = "Analysez l'email ci-dessous et expliquez pourquoi cette demande est suspecte, même si elle provient d'un 'expert-comptable' (minimum 100 caractères).",
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
    "from_name": "Jean-Philippe Rocher — Cabinet Rocher Expertise",
    "from_address": "jp.rocher@cabinet-rocher-expertise.fr",
    "to": "n.pons@durand-associes.fr",
    "subject": "Mission ponctuelle — audit de paie — accès Silae temporaire",
    "sent_at": "Aujourd'hui 11:04",
    "body": "Bonjour Madame Pons,\n\nJe suis Jean-Philippe Rocher, expert-comptable mandaté par Monsieur Chevalier pour un audit ponctuel de votre paramétrage de paie (contexte : réforme 2026 des cotisations patronales).\n\nPour gagner du temps, pourriez-vous créer dès aujourd'hui un utilisateur temporaire dans votre logiciel Silae avec mes identifiants ?\n\nLogin : jp.rocher_audit\nMot de passe initial : Audit2026!\nDroits : administrateur (nécessaire pour consulter tous les bulletins)\n\nMerci de me transmettre l'URL de connexion par retour de mail. L'intervention ne prendra que 2 à 3 jours. Je vous communiquerai mon rapport directement à Monsieur Chevalier.\n\nN° ordre : 26-75-8472\n\nBien cordialement,\nJean-Philippe Rocher\nExpert-Comptable associé"
  },
  "question": "Expliquez pourquoi cette demande est suspecte et quelles vérifications Nathalie doit effectuer AVANT toute création de compte.",
  "expected_elements": "Aucune lettre de mission n'est jointe ni mentionnée. Demande de droits administrateur (accès à tous les bulletins incluant données salariales sensibles = RGPD). Contournement hiérarchique direct de Nathalie sans passage par le cabinet associé habituel. Vérification à faire : appeler Maxime Chevalier sur son numéro connu pour confirmer la mission ; vérifier l'inscription de Jean-Philippe Rocher au tableau de l'Ordre sur experts-comptables.fr ; exiger la lettre de mission signée ; ne jamais créer un compte administrateur sans validation DAF et RSSI ; préférer un accès en lecture seule et en session accompagnée. Le mot de passe transmis en clair dans l'email est un autre signal d'alerte.",
  "min_chars": 100,
  "hint": "Pensez : qui doit valider une mission externe, quels droits minimaux donner, et que dit le RGPD sur l'accès aux bulletins de paie ?"
}
"""
        });

        // C2.4 — Multichoice : double facturation / fournisseur qui facture deux fois
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C24Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Fraude interne",
            Title        = "La double facturation suspecte",
            Instructions = "Un fournisseur a envoyé deux factures pour la même prestation, à quelques jours d'intervalle et avec des numéros de facture quasi identiques. Identifiez les bonnes actions.",
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
    "title": "Deux factures, un seul bon de commande",
    "context": "Nathalie constate en rapprochant les factures du mois que le fournisseur 'Logistique Moselle' a émis deux factures très similaires :\n\n• Facture n° LM-2026-0873 du 02/04/2026, 7 480 € TTC — réglée le 08/04\n• Facture n° LM-2026-0837 du 09/04/2026, 7 480 € TTC — en attente de règlement\n\nLes deux mentionnent la même prestation (transport avril 2026) et le même bon de commande interne (BC-2026-0112). L'IBAN de la seconde facture est différent (IBAN portugais au lieu de français).\n\nLa seconde facture a été approuvée dans le workflow par un compte 'm.chevalier' mais Maxime était en déplacement ce jour-là.",
    "icon": "alert"
  },
  "question": "Quelles sont les bonnes réactions immédiates ?",
  "choices": [
    { "id": "A", "label": "Suspendre immédiatement le règlement de la seconde facture et la marquer en investigation", "is_correct": true, "explanation": "Action prioritaire. Tout double règlement détecté doit être gelé avant tout autre geste : un fois le virement parti vers un compte étranger, il est quasi impossible de récupérer les fonds." },
    { "id": "B", "label": "Vérifier les logs de connexion et l'historique du compte m.chevalier pour détecter une compromission ou une usurpation interne", "is_correct": true, "explanation": "Point clé. Une approbation 'à distance' par un compte dirigeant alors que celui-ci est en déplacement est un signal majeur : compte volé (phishing), session restée ouverte, ou complicité interne. Le RSSI doit récupérer les logs horodatés immédiatement." },
    { "id": "C", "label": "Régler la seconde facture car le workflow l'a approuvée", "is_correct": false, "explanation": "Dangereux. Le workflow ne remplace JAMAIS la vérification humaine en cas d'anomalie détectée : même IBAN différent du fournisseur historique, même numéro de commande — tous les signaux sont au rouge." },
    { "id": "D", "label": "Contacter le fournisseur Logistique Moselle sur leur numéro habituel pour confirmer les deux factures", "is_correct": true, "explanation": "Indispensable. Soit c'est une erreur de facturation du fournisseur, soit la deuxième facture est frauduleuse (scénario classique : fraude interne ou email compromis côté fournisseur). La vérification téléphonique sur le numéro du répertoire lève l'ambiguïté." },
    { "id": "E", "label": "Supprimer la seconde facture du logiciel pour simplifier le dossier", "is_correct": false, "explanation": "Destruction de preuve. En cas de fraude, la seconde facture et son historique d'approbation sont des preuves essentielles pour le dépôt de plainte et la déclaration à l'assureur. Rien ne doit être supprimé : marquer en 'suspendue' et archiver." }
  ],
  "red_flags": [
    "Deux factures quasi identiques à quelques jours d'écart",
    "Numéros de facture dans le désordre (0873 puis 0837)",
    "IBAN différent entre les deux factures du même fournisseur",
    "Approbation par un dirigeant en déplacement ce jour-là",
    "Même bon de commande pour deux factures",
    "Montant important dupliqué (risque de pertes élevées)"
  ],
  "savoir_plus": "La double facturation est l'un des scénarios les plus récurrents de fraude interne ou d'attaque sur la supply chain financière. Parades : rapprochement systématique facture / bon de commande / bon de réception, contrôle des IBAN par rapport à la fiche fournisseur signée, et séparation des rôles (la personne qui saisit la facture n'est pas celle qui approuve)."
}
"""
        });
    }
}
