using CTF.Api.Models;

namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Parcours catalogue 08 — Sécurité bancaire & financière (niveau avancé).
/// Cible les professionnels du secteur bancaire : phishing SEPA/SWIFT,
/// wire fraud, blanchiment, KYC/AML, fraudes interbanques, attaques DAB.
/// </summary>
public static class Parcours08_Finance
{
    private static readonly Guid PathId    = Guid.Parse("c0000008-0000-0000-0000-000000000000");
    private static readonly Guid Module1Id = Guid.Parse("c0000008-0001-0000-0000-000000000000");
    private static readonly Guid Module2Id = Guid.Parse("c0000008-0002-0000-0000-000000000000");

    // Module 1 — Fraudes de paiement & virement
    private static readonly Guid C11Id = Guid.Parse("c0000008-0001-0001-0000-000000000000");
    private static readonly Guid C12Id = Guid.Parse("c0000008-0001-0002-0000-000000000000");
    private static readonly Guid C13Id = Guid.Parse("c0000008-0001-0003-0000-000000000000");
    private static readonly Guid C14Id = Guid.Parse("c0000008-0001-0004-0000-000000000000");

    // Module 2 — Conformité & cybersécurité bancaire
    private static readonly Guid C21Id = Guid.Parse("c0000008-0002-0001-0000-000000000000");
    private static readonly Guid C22Id = Guid.Parse("c0000008-0002-0002-0000-000000000000");
    private static readonly Guid C23Id = Guid.Parse("c0000008-0002-0003-0000-000000000000");
    private static readonly Guid C24Id = Guid.Parse("c0000008-0002-0004-0000-000000000000");

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
            Title            = "Sécurité bancaire & financière — Fraudes avancées",
            Description      = "Parcours avancé pour professionnels du secteur bancaire : phishing SEPA/SWIFT, wire fraud, détection de blanchiment, KYC/AML, fraudes interbanques, attaques sur les DABs.",
            Level            = "advanced",
            Status           = "published",
            Version          = 1,
            IsCatalog        = true,
            Sector           = "finance",
            EstimatedMinutes = 30,
            Tags             = "finance,banque,sepa,swift,wire-fraud,aml,kyc,blanchiment,avance",
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
            Title     = "Fraudes de paiement & virement",
            SortOrder = 1,
            CreatedAt = now
        });

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = Module2Id,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Conformité & cybersécurité bancaire",
            SortOrder = 2,
            CreatedAt = now
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // MODULE 1 — Fraudes de paiement & virement
    // ────────────────────────────────────────────────────────────────────────
    private static async Task SeedModule1ChallengesAsync(AppDbContext db, DateTime now)
    {
        // C1.1 — Mailbox : boîte de Pierre Sorel (directeur clientèle corporate)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C11Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "Inbox de Pierre Sorel — Directeur clientèle corporate CMA",
            Instructions = "Vous êtes Pierre Sorel, directeur de clientèle corporate au Crédit Maritime Adriatique (CMA), agence Lyon-Bellecour. Analysez votre boîte et cochez uniquement les emails réellement dangereux.",
            Difficulty   = 3,
            Points       = 175,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "emails": [
    { "id": "mail_1", "from_name": "SWIFT Alliance Ops", "from_address": "noreply@swift-alliance-security.co", "subject": "[URGENT] Connexion anormale sur votre terminal Alliance Access — authentification requise", "preview": "Notre système a détecté une tentative de connexion depuis Minsk...", "sent_at": "Aujourd'hui 08:12", "is_dangerous": true, "body": "Bonjour,\n\nNotre monitoring a détecté une tentative de connexion anormale sur votre terminal SWIFT Alliance Access (BIC : CMMAFRLY). Pour éviter la suspension de votre habilitation MT/MX, authentifiez-vous immédiatement :\n\n→ https://swift-alliance-security.co/revalidate?op=CMA2026\n\nIdentifiants Alliance + OTP attendus. Sans action sous 6h, votre poste sera déconnecté du réseau SWIFT.\n\nSWIFT Security Operations", "red_flags": ["Domaine swift-alliance-security.co — SWIFT ne communique jamais via cette extension", "SWIFT ne sollicite jamais d'authentification par lien email", "Urgence artificielle (6h) typique du phishing", "Demande d'OTP par formulaire web (interdit chez SWIFT)", "Menace de déconnexion pour forcer l'action"] },
    { "id": "mail_2", "from_name": "NovaCargo SA — Direction Financière", "from_address": "cfo@novacargo-group.net", "subject": "Confirmation IBAN pour virement SEPA B2B dossier NC-FR-887", "preview": "Bonjour Pierre, merci de bien vouloir confirmer l'exécution du virement...", "sent_at": "Aujourd'hui 10:47", "is_dangerous": true, "body": "Bonjour Pierre,\n\nSuite à notre échange téléphonique d'hier, merci de confirmer l'exécution du virement SEPA B2B de 428 000 € vers notre partenaire logistique (dossier NC-FR-887).\n\nBénéficiaire : MARTEC LOGISTIK GMBH\nIBAN : DE42 5001 0517 9876 5432 10\nBIC : INGDDEFFXXX\n\nJe vous confirme que nous avons retiré le mandat de blocage B2B précédent pour cette opération. Merci de me faire savoir dès validation.\n\nJean-Baptiste Leclerc\nCFO — NovaCargo SA", "red_flags": ["Domaine novacargo-group.net au lieu du domaine habituel du client", "Prétendu échange téléphonique jamais eu lieu (à vérifier dans l'agenda)", "Mention d'un 'retrait de mandat B2B' — suspect : le SEPA SDD B2B exige un mandat signé, pas un simple email", "Interlocuteur (CFO 'Jean-Baptiste Leclerc') inconnu chez NovaCargo", "IBAN allemand chez ING-DiBa vers un 'partenaire logistique' non référencé", "Montant élevé combiné à une demande de validation rapide"] },
    { "id": "mail_3", "from_name": "ACPR — Autorité de Contrôle Prudentiel", "from_address": "reporting@acpr-banque-france.fr", "subject": "Rappel : remise COREP C03.00 — échéance vendredi", "preview": "Bonjour, pour rappel votre remise C03.00 sur fonds propres doit être transmise...", "sent_at": "Hier 17:30", "is_dangerous": false, "body": "Bonjour,\n\nPour rappel, la remise COREP C03.00 (fonds propres consolidés — périmètre Tier 1) est à transmettre via le portail ONEGATE de la Banque de France au plus tard vendredi 18h.\n\nLien portail habituel : https://onegate.banque-france.fr\n\nCordialement,\nCellule Reporting — ACPR", "red_flags": [] },
    { "id": "mail_4", "from_name": "Support IT — CMA", "from_address": "it-support@cma-bank.fr", "subject": "Maintenance planifiée agence Lyon-Bellecour — samedi 02:00-04:00", "preview": "Bonjour, une maintenance des postes de l'agence aura lieu samedi...", "sent_at": "Hier 15:18", "is_dangerous": false, "body": "Bonjour,\n\nUne fenêtre de maintenance des postes de l'agence Lyon-Bellecour est planifiée samedi matin de 02:00 à 04:00 (redémarrage + patchs Windows).\n\nMerci de fermer vos sessions vendredi avant 20h et de ne pas laisser d'applications critiques ouvertes.\n\nSupport IT — CMA", "red_flags": [] },
    { "id": "mail_5", "from_name": "Banque Correspondante — KYC", "from_address": "kyc@afriline-bankgroup.com", "subject": "Demande d'ouverture de relation bancaire correspondante — dossier KYC", "preview": "Nous sollicitons l'établissement d'une relation de correspondant bancaire...", "sent_at": "Aujourd'hui 11:22", "is_dangerous": true, "body": "Dear Mr Sorel,\n\nAFRILINE BANK GROUP (Malabo, Guinée Équatoriale) sollicite l'établissement d'une relation de banque correspondante avec CMA pour faciliter les flux USD/EUR de notre clientèle corporate.\n\nNous joignons :\n- Licence bancaire (scan PDF)\n- Pièce d'identité du bénéficiaire effectif\n- Extrait du registre\n\nPourriez-vous nous confirmer l'ouverture sous 5 jours ouvrés ? Nos correspondants à Chypre et aux Seychelles ont besoin de finaliser des opérations importantes.\n\nRegards,\nMr Okoro — Compliance Officer\nAFRILINE BANK GROUP", "red_flags": ["Banque basée dans une juridiction à haut risque AML (Guinée Équatoriale)", "Mention d'activités via Chypre / Seychelles — pavillons de complaisance classiques pour le blanchiment", "Demande d'ouverture 'en 5 jours' — procédure KYC correspondent banking = plusieurs semaines minimum", "Domaine .com non corporate pour une banque réglementée", "Documents fournis uniquement en PDF scan (facilement falsifiables)", "Pression sur urgence d'opérations à venir"] },
    { "id": "mail_6", "from_name": "BCE / SEPA Alert", "from_address": "alerts@sepa-clearing-update.eu", "subject": "Mise à jour obligatoire du module CSM — échéance 30/04", "preview": "Tous les établissements doivent installer la mise à jour 2026.02 du module Clearing & Settlement...", "sent_at": "Aujourd'hui 09:40", "is_dangerous": true, "body": "Chers partenaires,\n\nLa BCE impose à tous les établissements SEPA l'installation de la mise à jour 2026.02 du module Clearing & Settlement Mechanism avant le 30/04/2026.\n\nTéléchargement (exécutable requis sur chaque poste relié au CSM) :\nhttps://sepa-clearing-update.eu/installer/CSM-2026.02.exe\n\nAction manuelle requise — pas de déploiement centralisé prévu.\n\nBCE — Payment Systems Division", "red_flags": ["Domaine sepa-clearing-update.eu — la BCE communique via ecb.europa.eu uniquement", "Exécutable .exe distribué par email — jamais en environnement bancaire régulé", "Pas de signature numérique ni de hash de vérification", "Urgence artificielle (30/04)", "Demande de contournement du déploiement centralisé (alerte majeure côté RSSI)"] }
  ]
}
"""
        });

        // C1.2 — Multichoice : fausse alerte SWIFT avec IBAN mule
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C12Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "SWIFT / Wire fraud",
            Title        = "Fausse alerte SWIFT — IBAN mule",
            Instructions = "Un email prétend provenir du support SWIFT Alliance et demande de valider une nouvelle relation bénéficiaire. Identifiez tous les signaux de fraude.",
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
    "from_name": "SWIFT Support — Beneficiary Validation",
    "from_address": "validation@swift-beneficiary-check.com",
    "to": "p.sorel@cma-bank.fr",
    "subject": "[ACTION REQUIRED] MT103 outbound — beneficiary validation pending",
    "sent_at": "Aujourd'hui 14:08",
    "body": "Dear Mr Sorel,\n\nYour bank's outbound MT103 (Ref: FT26CMA18472) of 1,245,000 EUR to beneficiary NOVACARGO SA is currently held by our sanctions screening tool for routine validation.\n\nTo release the payment, please confirm the new beneficiary account:\n\nBeneficiary : NOVACARGO SA\nIBAN : MT84 VALL 2201 3000 0000 0000 1234 567\nBIC : VALLMTMT (Valletta Bank — Malta)\nHolder address : 45 St Julian's Street, Sliema\n\nValidate here: https://swift-beneficiary-check.com/release/FT26CMA18472\n\nPayment will expire in 3 hours if not released.\n\nSWIFT Customer Security Programme — Support Team"
  },
  "question": "Quels éléments prouvent qu'il s'agit d'une fraude ? (plusieurs réponses possibles)",
  "choices": [
    { "id": "A", "label": "SWIFT n'intervient JAMAIS dans la validation bénéficiaire d'un MT103 — c'est la banque émettrice qui applique le screening sanctions", "is_correct": true, "explanation": "Correct. SWIFT est un réseau de messagerie (MT/MX) et n'est pas partie prenante au contrôle de conformité des paiements. Le screening sanctions (OFAC, UE, ONU, Gel des avoirs français) est fait en interne par la banque émettrice via ses outils (FircoSoft, Accuity, etc.). Aucun 'release link' n'existe côté SWIFT." },
    { "id": "B", "label": "Domaine swift-beneficiary-check.com n'appartient pas à SWIFT — le domaine officiel est swift.com", "is_correct": true, "explanation": "Exact. Tout domaine qui n'est pas swift.com (ou une signature X.509 vérifiable via SWIFTNet PKI) est un faux. SWIFT publie régulièrement ses IOCs dans le Customer Security Programme (CSP)." },
    { "id": "C", "label": "IBAN maltais (MT) chez une banque de Valette pour un destinataire supposément 'NovaCargo SA' client français habituel", "is_correct": true, "explanation": "Point clé. Un IBAN bénéficiaire qui change pour une juridiction à risque (Malte, Chypre, certaines îles anglo-normandes) doit déclencher un callback contradictoire avec le client sur numéro du répertoire (procédure 'dual authentication' imposée par la DSP2 pour les virements non-récurrents élevés)." },
    { "id": "D", "label": "Lien de 'release' cliquable — SWIFT Alliance ne propose pas d'interface web pour libérer un MT103", "is_correct": true, "explanation": "Correct. Les opérations Alliance Access/Lite2 se font exclusivement depuis les terminaux authentifiés en interne (HSM + token), jamais via un lien email. Tout lien externe qui prétend manipuler un MT103 est une escroquerie." },
    { "id": "E", "label": "Le montant (1 245 000 EUR) est élevé, donc la demande est plausible — les gros clients justifient des procédures exceptionnelles", "is_correct": false, "explanation": "FAUX et dangereux. Un montant élevé doit AU CONTRAIRE déclencher tous les contrôles standards (callback client, dual approbation, vérification IBAN auprès du chargé de compte). Aucune procédure ne s'assouplit à cause du montant — elle se renforce." }
  ],
  "red_flags": [
    "Usurpation du nom 'SWIFT' hors du domaine swift.com",
    "Demande de confirmation via lien web externe",
    "IBAN bénéficiaire changé vers juridiction à risque (Malte)",
    "Urgence artificielle (expiration en 3h)",
    "Référence de transaction incohérente avec vos systèmes internes",
    "Absence de signature PKI SWIFTNet sur le message"
  ],
  "savoir_plus": "Le SWIFT Customer Security Programme (CSP) impose depuis 2017 des contrôles de 'beneficiary verification' AVANT émission d'un MT103, côté banque donneuse d'ordre. SWIFT ne contacte JAMAIS les opérateurs pour 'libérer' une transaction. Tout email prétendant le contraire relève du phishing sophistiqué (wire fraud)."
}
"""
        });

        // C1.3 — CEO fraud : faux CFO de NovaCargo (client corporate)
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C13Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Wire fraud",
            Title        = "Le faux CFO de NovaCargo — virement M&A",
            Instructions = "Pierre Sorel reçoit un email urgent apparemment du CFO de son client corporate NovaCargo, demandant l'exécution d'un virement M&A confidentiel. Choisissez la ou les bonnes réactions.",
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
    "from_name": "Élise Mercadier — CFO NovaCargo SA",
    "from_address": "e.mercadier@novacargo-direction.com",
    "to": "p.sorel@cma-bank.fr",
    "subject": "CONFIDENTIEL — Closing opération Falcon aujourd'hui 18h",
    "sent_at": "Aujourd'hui 15:47",
    "body": "Pierre,\n\nJe t'écris en urgence : le closing de notre opération d'acquisition Falcon (acquéreur étranger) tombe aujourd'hui 18h. L'opération est confidentielle, seuls notre CEO, moi et le board restreint sommes au courant.\n\nIl faut impérativement exécuter ce soir un virement SWIFT MT103 de 2 800 000 EUR vers le compte séquestre de nos conseils :\n\nBénéficiaire : LOOMIS ADVISORY LIMITED\nIBAN : CY17 0020 0128 0000 0012 0052 7600\nBIC : BCYPCY2NXXX (Bank of Cyprus)\nMotif : Closing Falcon - escrow fees\n\nNotre mandataire habituel (Maître Vidal) n'est pas joignable aujourd'hui pour valider. Peux-tu prendre cette décision en direct avec moi ? Je suis en visio aux US, peu joignable, mais je te réponds par mail en moins de 5 min.\n\nJe compte sur ta discrétion — toute fuite ferait échouer l'opération.\n\nÉlise Mercadier\nCFO — NovaCargo SA"
  },
  "choices": [
    { "id": "execute", "label": "Exécuter le virement : c'est un client de longue date, on lui fait confiance", "icon": "bank", "is_correct": false, "explanation": "Erreur majeure. La relation de confiance avec le client n'autorise JAMAIS à contourner les procédures : un virement de cette envergure, vers une juridiction à risque (Chypre), hors des interlocuteurs habituels (Maître Vidal absent), sans double signature, relève du scénario-type de wire fraud. Perte moyenne 2,4 M€ par cas (FBI IC3 Report 2024)." },
    { "id": "callback", "label": "Appeler Élise Mercadier sur son numéro professionnel connu au répertoire client — pas celui de l'email", "icon": "phone", "is_correct": true, "explanation": "Réflexe clé. Le callback contradictoire sur le numéro de référence du dossier client (pas de l'email) est le contrôle n°1 exigé par la DSP2 pour les 'payments to new beneficiaries' au-dessus des seuils de risque. Si elle ne répond pas : la transaction attend, point." },
    { "id": "dual", "label": "Exiger la dual approbation interne (compliance + risk) et l'émission via Alliance Access avec double signature", "icon": "flag", "is_correct": true, "explanation": "Procédure standard obligatoire pour tout MT103 supérieur au seuil d'agence. Aucune dérogation ne peut être accordée sur simple email client : le principe 'quatre yeux' (four-eyes principle) est un contrôle interne non négociable des établissements réglementés." },
    { "id": "escalate", "label": "Signaler immédiatement au RSSI, à la cellule anti-fraude et au responsable conformité avant tout acte", "icon": "flag", "is_correct": true, "explanation": "Obligatoire. Tout soupçon de tentative de wire fraud doit être signalé à la cellule LCB-FT (Lutte Contre le Blanchiment et le Financement du Terrorisme) et au RSSI. Le signalement est tracé ; en cas de fraude avérée, la banque doit faire une DS (Déclaration de Soupçon) à TRACFIN sous 24h." }
  ],
  "red_flags": [
    "Domaine novacargo-direction.com au lieu du domaine NovaCargo habituel",
    "Confidentialité exigée, contournement du mandataire de référence",
    "IBAN chypriote vers société offshore 'Loomis Advisory'",
    "Urgence artificielle liée à un closing M&A (scénario typique)",
    "Interlocutrice injoignable sauf par email",
    "Demande de décision unilatérale contre procédure dual approbation",
    "Motif 'escrow fees' non documenté par mandat signé du client"
  ]
}
"""
        });

        // C1.4 — Free text : smishing SMS de validation opération + faux trader
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C14Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "Ingénierie sociale",
            Title        = "Smishing bancaire & ordre d'un 'trader' en urgence",
            Instructions = "Deux situations avancées à analyser. Rédigez votre réponse, l'IA évaluera la rigueur de votre raisonnement.",
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
      "question": "Un client corporate de NovaCargo SA appelle la banque en panique : il a reçu un SMS apparemment de CMA avec un lien 'Sécuriser l'opération' et un code à 6 chiffres. Il a cliqué, saisi ses identifiants et validé le code OTP. Immédiatement après, un virement de 185 000 € a quitté son compte vers l'étranger. Que doit faire Pierre Sorel en urgence pour tenter un rappel de fonds et respecter les obligations DSP2 ?",
      "context": "smishing et droit au rappel DSP2",
      "expected_elements": "Déclencher immédiatement un SWIFT MT192 (cancel) ou, si trop tard, un MT199/MT299 recall vers la banque réceptrice. Contacter la cellule anti-fraude interbancaire (procédure Wire Recall). Déposer plainte pour le client et orienter vers THESEE (plateforme du Ministère de l'Intérieur) et Perceval (signalement fraude CB/virement). Bloquer immédiatement les accès en ligne compromis et régénérer les identifiants. Sous DSP2, obligation de rembourser sans délai le paiement non autorisé (art. L133-18 CMF) sauf preuve de négligence grave du client. Documenter la chronologie complète. Prévenir le Data Protection Officer si données personnelles compromises.",
      "min_chars": 100,
      "hint": "Pensez : mécanismes de recall SWIFT, plateformes de signalement, et obligations de remboursement sous la DSP2."
    },
    {
      "id": "q2",
      "question": "Un individu se présentant comme 'Alexandre Noury, trader senior salle de marché Paris CMA' appelle Pierre en fin de journée depuis un numéro inconnu et demande d'exécuter immédiatement une opération de couverture de change EUR/USD de 6 M€ pour un client VIP, en dehors des canaux normaux. Il prétend que son terminal Bloomberg est en panne et qu'il faut 'saisir à la main côté agence'. Décrivez les contrôles à appliquer et les signaux de fraude.",
      "context": "usurpation trader et contournement du STP",
      "expected_elements": "Aucune opération de marché ne se saisit 'à la main' côté agence : le STP (Straight Through Processing) front-office → middle-office → back-office est obligatoire et tracé. Vérifier l'identité du trader via l'annuaire interne certifié, rappeler sur le numéro fixe de la salle de marché. Un trader n'appelle JAMAIS un chargé de clientèle agence pour saisir un deal. Signaler à la compliance, au RSSI et à la direction. Les ordres de change > seuils (Volcker / MiFID II best execution) suivent un workflow strict avec approbation risk. Consigner l'appel, numéro, heure, prétexte. Possible deepfake vocal en fin de journée pour exploiter la fatigue.",
      "min_chars": 100,
      "hint": "Quel est le parcours obligatoire d'un ordre de marché ? Qui peut saisir, depuis où, et avec quels contrôles ?"
    }
  ]
}
"""
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // MODULE 2 — Conformité & cybersécurité bancaire
    // ────────────────────────────────────────────────────────────────────────
    private static async Task SeedModule2ChallengesAsync(AppDbContext db, DateTime now)
    {
        // C2.1 — Multichoice : détection de structuring / blanchiment
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C21Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "AML / LCB-FT",
            Title        = "Structuring — détection d'un schéma de blanchiment",
            Instructions = "Un compte client présente un comportement anormal. Identifiez la qualification AML correcte et les actions obligatoires.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Compte entreprise KYL-Consulting — opérations inhabituelles",
    "context": "L'outil de monitoring transactionnel CMA alerte sur le compte 'KYL-Consulting SARL' (client classé risque modéré au KYC initial, CA déclaré 420 k€) :\n\n• 14 dépôts en espèces entre 9 400 et 9 800 € au cours des 9 derniers jours, dans 6 agences CMA différentes en Rhône-Alpes\n• Suivi, dans les 48h de chaque dépôt, d'un virement SEPA sortant du même montant environ, réparti entre 5 comptes bénéficiaires domiciliés à Chypre, Malte et Dubaï\n• Les justificatifs économiques fournis par le client (factures) présentent des incohérences : clients finaux inconnus des bases sectorielles, adresses identiques pour plusieurs clients distincts\n\nLe gérant, M. Karim Lesnard, est joignable et coopératif, propose des justifications verbales mais ne fournit pas les contrats.",
    "icon": "alert"
  },
  "question": "Quelle est la qualification correcte et quelles actions s'imposent ?",
  "choices": [
    { "id": "A", "label": "C'est un cas typique de 'structuring' (fractionnement) : dépôts juste en-dessous du seuil psychologique de 10 000 € pour éviter les contrôles renforcés", "is_correct": true, "explanation": "Correct. Le structuring (ou smurfing) consiste à fractionner les dépôts juste sous un seuil déclencheur pour éviter le contrôle LCB-FT renforcé. Combiné à la dispersion géographique (6 agences) et au 'layering' ultérieur vers juridictions offshore, c'est un schéma de blanchiment manuel. Le seuil français de déclaration automatique n'est pas 10 000 € mais la vigilance renforcée s'applique selon l'approche par les risques (art. L561-10 CMF)." },
    { "id": "B", "label": "Une déclaration de soupçon (DS) doit être envoyée à TRACFIN sans délai", "is_correct": true, "explanation": "Obligation légale (art. L561-15 CMF). Toute suspicion de blanchiment ou de financement du terrorisme déclenche une DS à TRACFIN via ERMES. Le seuil de transmission est le soupçon, PAS la certitude. Non-déclaration = sanction pénale (5 ans / 375 000 €) + sanctions ACPR (jusqu'à 100 M€)." },
    { "id": "C", "label": "Informer le client qu'une DS est en cours pour être transparent", "is_correct": false, "explanation": "INTERDIT. L'art. L561-18 CMF impose une interdiction absolue de 'tipping off' : aucune information ne peut être divulguée au client sur la DS, sous peine de 5 ans de prison et 375 000 €. Le devoir de confidentialité envers TRACFIN prévaut sur toute autre relation contractuelle." },
    { "id": "D", "label": "Ré-évaluer le client en risque élevé, demander KYC renforcé (Enhanced Due Diligence) incluant bénéficiaires effectifs, origine des fonds documentée et contrats commerciaux", "is_correct": true, "explanation": "Obligation de vigilance renforcée (EDD) conformément à la directive AML 5 (transposée en France en 2020). Exiger les contrats signés, la preuve de paiement des clients finaux, l'identification complète des bénéficiaires effectifs (UBO > 25 %). En cas de refus du client : rupture de relation et gel des opérations." },
    { "id": "E", "label": "Geler les opérations en attendant la réponse de TRACFIN (7 jours ouvrés)", "is_correct": true, "explanation": "Option prévue par l'art. L561-16 CMF : le professionnel peut (et parfois doit) s'abstenir d'exécuter l'opération le temps que TRACFIN réponde (2 jours ouvrés par défaut, prolongeable). Cela évite la complicité objective de blanchiment. Le client n'est pas informé de la raison du blocage — motif neutre communiqué uniquement." }
  ],
  "red_flags": [
    "Fractionnement systématique sous seuil (structuring)",
    "Dispersion géographique des dépôts (plusieurs agences)",
    "Layering immédiat vers juridictions à risque (Chypre, Malte, Dubaï)",
    "Justificatifs commerciaux incohérents",
    "Adresses partagées par clients supposément distincts",
    "Refus ou délai dans la production des contrats originaux"
  ],
  "savoir_plus": "Le dispositif français LCB-FT (Lutte Contre le Blanchiment et le Financement du Terrorisme) repose sur les articles L561-1 et suivants du Code Monétaire et Financier, et les directives européennes AMLD 4 (2015), AMLD 5 (2018) et AMLD 6 (2018 — dimension pénale). Le règlement AMLR (2024) et la création de l'AMLA (Authority for Anti-Money Laundering) à Francfort renforcent le dispositif au niveau UE à partir de 2026."
}
"""
        });

        // C2.2 — Multichoice : KYC correspondent banking / banque correspondante suspecte
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C22Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "KYC bancaire",
            Title        = "KYC correspondant bancaire — banque à haut risque",
            Instructions = "Une banque correspondante étrangère sollicite une ouverture de relation. Choisissez les contrôles et réactions correctes.",
            Difficulty   = 3,
            Points       = 175,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Demande d'ouverture de relation de correspondent banking",
    "context": "Pierre reçoit (dossier transmis ce matin par la conformité) une demande formelle d'ouverture de relation de correspondent banking entre CMA et 'Adria Private Banking', établissement basé à Podgorica (Monténégro).\n\nDonnées du dossier :\n• Licence bancaire : délivrée en 2023 par la Banque Centrale du Monténégro\n• Actionnariat ultime : structure opaque passant par une fiducie aux BVI\n• Volumes attendus : 80 M€/an en USD clearing, principalement pour clientèle 'high-net-worth' russe et chinoise\n• Pays : Monténégro — non UE, inscrit sur la liste GAFI des juridictions à surveillance renforcée (grey list)\n• Références : aucun correspondent banking européen actif ; partenaire actuel unique à Chypre (Nicosia Commercial Bank)",
    "icon": "alert"
  },
  "question": "Quelles sont les réactions conformes pour le Responsable Conformité / RSSI / directeur clientèle corporate ?",
  "choices": [
    { "id": "A", "label": "Appliquer une procédure KYC renforcée (Enhanced Due Diligence) spécifique correspondent banking : EDD art. 19 AMLD 5 + Wolfsberg CB Questionnaire", "is_correct": true, "explanation": "Obligation directive. Tout correspondent banking avec un établissement non-UE exige EDD avec approbation de la direction générale (pas uniquement conformité). Le questionnaire Wolfsberg Correspondent Banking Due Diligence (CBDDQ) est le standard international." },
    { "id": "B", "label": "Refuser net : la banque est en juridiction à haut risque GAFI + structure actionnariale opaque + volumes russes/chinois", "is_correct": true, "explanation": "Réaction prudente et conforme. L'addition de plusieurs red flags AML (grey list, UBO opaque, géographies sanctionnées/à risque, absence d'historique correspondent banking) justifie pleinement un refus. La politique 'risk appetite' de la banque peut explicitement exclure ces relations." },
    { "id": "C", "label": "Vérifier l'identité des bénéficiaires effectifs (UBO > 25 %) avant toute suite, même si cela exige de traverser une fiducie BVI", "is_correct": true, "explanation": "Sans identification exhaustive des UBO (Ultimate Beneficial Owners), aucune relation ne peut être ouverte (art. L561-2-2 CMF et 4e/5e directives AML). Une structure BVI (British Virgin Islands) est un signal AML en soi — la juridiction pratique l'opacité. Si l'identification exhaustive est impossible : refus obligatoire." },
    { "id": "D", "label": "Accepter la relation pour gagner des parts de marché : la licence est délivrée par une banque centrale, c'est donc régulé", "is_correct": false, "explanation": "Dangereux. Une licence émise par une banque centrale nationale n'absout ni des risques sanctions, ni des risques AML, ni du devoir de vigilance renforcée. Plusieurs banques européennes ont été sanctionnées à des centaines de millions d'euros (ING 775M€ 2018, Danske Bank 2M$ 2022, BNPP 9 Md$ en 2014) pour avoir précisément accepté ce raisonnement." },
    { "id": "E", "label": "Vérifier l'absence d'activité shell bank et documenter par écrit l'approbation de la direction générale avant toute suite", "is_correct": true, "explanation": "La directive AML interdit strictement les relations avec des 'shell banks' (banques fantômes sans présence physique ni supervision). L'approbation doit être écrite, motivée, et signée par la direction générale (principe de décision au niveau approprié à la gravité du risque)." }
  ],
  "red_flags": [
    "Juridiction Monténégro — grey list GAFI",
    "Actionnariat ultime via fiducie BVI — UBO opaque",
    "Clientèle 'HNW russe / chinoise' — sanctions secondaires possibles",
    "Aucun correspondent banking européen établi",
    "Référence unique dans juridiction elle-même à risque (Chypre)",
    "Volumes USD — exposition au régime de sanctions OFAC"
  ],
  "savoir_plus": "Le correspondent banking est l'un des principaux vecteurs historiques de blanchiment international (scandales Danske Bank, Raiffeisen, ABN AMRO). La directive AML 5 et les standards Wolfsberg imposent : (1) identification exhaustive des UBO, (2) EDD renforcée, (3) approbation direction générale, (4) interdiction shell banks, (5) revue annuelle. Côté USD clearing, toute erreur expose à des sanctions OFAC extra-territoriales chiffrées en milliards."
}
"""
        });

        // C2.3 — Phishing AI : faux email BCE / supervision bancaire
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C23Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Cybersécurité bancaire",
            Title        = "Faux email BCE — prétendue inspection conjointe",
            Instructions = "Analysez cet email prétendument issu de la BCE et rédigez les éléments qui prouvent qu'il s'agit d'un phishing ciblant les établissements bancaires (minimum 100 caractères).",
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
    "from_name": "ECB Joint Supervisory Team — CMA Group",
    "from_address": "jst-cma@ecb-supervision-services.eu",
    "to": "p.sorel@cma-bank.fr",
    "subject": "[SSM] Pre-inspection questionnaire — ICT risk review Q2 2026 — response required 48h",
    "sent_at": "Aujourd'hui 09:22",
    "body": "Dear Mr Sorel,\n\nAs part of the 2026 SREP cycle, the Joint Supervisory Team in charge of Crédit Maritime Adriatique has scheduled an on-site ICT and cyber-resilience inspection (DORA compliance readiness assessment).\n\nIn preparation, please complete and return the pre-inspection questionnaire within 48 hours:\n\n→ Secure portal: https://ecb-supervision-services.eu/jst-portal/CMA2026\n\nYou will need:\n- Network diagrams of core banking systems\n- List of outsourced ICT services (per DORA Art.28)\n- BCP/DRP documentation including RTO/RPO per process\n- Credentials of ICT Risk Manager for portal access\n\nThe JST will use your responses as an input for the 2026 SREP decision. Late response will be noted in the Supervisory Examination Programme.\n\nKind regards,\nJoint Supervisory Team — CMA Group\nEuropean Central Bank — SSM"
  },
  "question": "Décrivez précisément pourquoi cet email ne peut pas provenir de la BCE et quels risques il présente pour la banque.",
  "expected_elements": "La BCE communique avec les banques significatives uniquement via la plateforme officielle IMAS (Information Management System) du Single Supervisory Mechanism, jamais par email public. Le domaine légitime de la BCE est ecb.europa.eu (pas ecb-supervision-services.eu). Aucune supervision BCE ne demande les credentials d'un ICT Risk Manager — demande immédiate rejetée, signal d'attaque ciblant potentiellement le cœur du SI bancaire. Les documents sollicités (schémas réseau, BCP/DRP, liste des fournisseurs ICT DORA) permettent à un attaquant de cartographier l'infrastructure pour une attaque ciblée (ransomware, APT). Actions : ne pas cliquer, ne rien transmettre, signaler au RSSI et à la cellule CERT interne, alerter l'ACPR / BCE via le canal officiel pour confirmer que la demande n'existe pas, scanner l'URL dans un bac à sable, préserver les en-têtes pour analyse forensique.",
  "min_chars": 100,
  "hint": "Comment la BCE communique-t-elle officiellement avec les banques supervisées ? Que demande cet email qu'un régulateur ne demanderait JAMAIS par email ?"
}
"""
        });

        // C2.4 — Multichoice : attaque sur DAB (ATM) + jackpotting
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = C24Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Sécurité physique / ATM",
            Title        = "Attaque sur DAB — jackpotting & skimming",
            Instructions = "Un DAB de l'agence CMA Lyon-Bellecour présente des comportements anormaux. Identifiez les scénarios d'attaque probables et les réactions correctes.",
            Difficulty   = 3,
            Points       = 175,
            SortOrder    = 4,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "DAB Lyon-Bellecour — comportements anormaux",
    "context": "Ce matin à 07h40, plusieurs alertes simultanées sur le DAB extérieur de l'agence Lyon-Bellecour (référence ATM CMA-LYB-03) :\n\n• Compteur cassettes : vidage anormal de la cassette 20€ entre 04:02 et 04:07 (7 500 €) sans corresponder aux opérations client enregistrées\n• Journal système : ouverture physique de la trappe supérieure détectée à 03:58, suivie d'un redémarrage du logiciel de distribution avec session administrateur inconnue\n• Panneau façade : technicien vu par caméra en tenue 'maintenance Diebold Nixdorf' à 03:54 — aucune intervention planifiée\n• Côté client visible à l'ouverture : un petit boîtier plastique noir ajouté au-dessus du clavier, une fine plaque métallique collée sur la fente carte",
    "icon": "alert"
  },
  "question": "Quels scénarios d'attaque sont présents ici et quelles actions s'imposent ?",
  "choices": [
    { "id": "A", "label": "Jackpotting : attaque directe sur le logiciel ATM via ouverture physique + connexion à un outil malveillant (type Tyupkin, Cutlet Maker) pour forcer la distribution", "is_correct": true, "explanation": "Très probable. Le jackpotting (ou 'ATM cashout') combine une intrusion physique (trappe supérieure donnant accès au PC interne) et un malware injecté via USB pour vider les cassettes. Séquence typique : technicien faux-flag + ouverture à 04h + vidage simultané ciblé cassette 20€. Documenté dans l'Europol iOCTA depuis 2014." },
    { "id": "B", "label": "Skimming : le boîtier ajouté et la plaque sur la fente carte sont typiques du skimming (capture piste magnétique + clavier via caméra miniature)", "is_correct": true, "explanation": "Cohérent. Le skimming capture les données de la piste magnétique via surmoulage de lecteur + code PIN via mini-caméra ou faux clavier. Même sur cartes EMV à puce, la piste magnétique reste exploitable pour des paiements dans des pays non-EMV." },
    { "id": "C", "label": "Isoler immédiatement le DAB (mise hors service logique via supervision centrale), préserver les preuves, ne rien toucher physiquement avant forensique", "is_correct": true, "explanation": "Procédure standard. Le DAB est une scène d'infraction ; toute manipulation (retrait du boîtier suspect, nettoyage) détruit des preuves pénales. Isolation logique immédiate pour stopper les débits, appel police + cellule CERT, préservation des caméras, journaux système et cassettes en l'état." },
    { "id": "D", "label": "Remettre le DAB en service rapidement pour ne pas pénaliser la clientèle du matin", "is_correct": false, "explanation": "Dangereux. Remettre en service un DAB potentiellement compromis expose : (1) les clients dont les cartes et PIN sont volés par le skimmer, (2) la banque à une poursuite de la fraude jackpot, (3) la preuve judiciaire à une destruction. Aucune remise en service avant audit complet et remplacement des composants suspects." },
    { "id": "E", "label": "Signaler à la police, à la Banque de France et à l'ACPR comme incident opérationnel significatif au titre de DORA / Art.19", "is_correct": true, "explanation": "Obligation réglementaire. DORA (Digital Operational Resilience Act, applicable depuis janvier 2025) impose la notification des incidents ICT majeurs à l'autorité compétente (ACPR) dans des délais stricts. Un jackpotting combiné à skimming est un 'major ICT-related incident' au sens de l'art. 18 DORA + art. 19 pour la notification. Dépôt de plainte parallèle auprès de la police judiciaire." }
  ],
  "red_flags": [
    "Technicien 'maintenance' hors plage planifiée",
    "Ouverture de la trappe supérieure (accès PC interne du DAB)",
    "Session administrateur inconnue + redémarrage logiciel",
    "Vidage ciblé d'une cassette en quelques minutes",
    "Boîtier et plaque ajoutés côté client visible",
    "Absence totale d'intervention autorisée correspondante"
  ],
  "savoir_plus": "Les attaques contre les DAB restent un risque majeur pour les banques de détail : jackpotting (attaque logicielle), skimming (capture de cartes), shimming (capture puce EMV), black box attack (connexion directe au distributeur), explosive attack (gaz / plastic). La norme PCI DSS 4.0 et DORA (règlement UE 2022/2554 entré en application en janvier 2025) encadrent désormais les obligations de résilience opérationnelle ICT des établissements financiers."
}
"""
        });
    }
}
