using CTF.Api.Models;

namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Catalogue — Parcours 03 : "Cybermenaces ciblant les établissements médicaux".
/// Niveau avancé, secteur santé. 2 modules, 8 challenges.
/// Scénarios : ransomware SIH, IoT médical, supply chain pharma, cellule de crise, PCA hospitalier.
/// </summary>
internal static class Parcours03_SanteRansomware
{
    private static readonly Guid PathId     = Guid.Parse("c0000003-0000-0000-0000-000000000000");
    private static readonly Guid Module1Id  = Guid.Parse("c0000003-0001-0000-0000-000000000000");
    private static readonly Guid Module2Id  = Guid.Parse("c0000003-0002-0000-0000-000000000000");

    // Module 1 — Attaques ciblées
    private static readonly Guid M1C1Id = Guid.Parse("c0000003-0001-0001-0000-000000000000");
    private static readonly Guid M1C2Id = Guid.Parse("c0000003-0001-0002-0000-000000000000");
    private static readonly Guid M1C3Id = Guid.Parse("c0000003-0001-0003-0000-000000000000");
    private static readonly Guid M1C4Id = Guid.Parse("c0000003-0001-0004-0000-000000000000");

    // Module 2 — Réponse à incident
    private static readonly Guid M2C1Id = Guid.Parse("c0000003-0002-0001-0000-000000000000");
    private static readonly Guid M2C2Id = Guid.Parse("c0000003-0002-0002-0000-000000000000");
    private static readonly Guid M2C3Id = Guid.Parse("c0000003-0002-0003-0000-000000000000");
    private static readonly Guid M2C4Id = Guid.Parse("c0000003-0002-0004-0000-000000000000");

    public static async Task SeedAsync(AppDbContext db, DateTime now)
    {
        await CatalogSeedBase.UpsertPathAsync(db, new LearningPath
        {
            Id               = PathId,
            TenantId         = CatalogSeedBase.CatalogTenantId,
            Type             = "catalog",
            Title            = "Cybermenaces ciblant les établissements médicaux",
            Description      = "Ransomware, IoT médical compromis, attaques supply chain : les menaces avancées qui ciblent hôpitaux et cliniques, et comment les détecter tôt.",
            Level            = "advanced",
            Status           = "published",
            Version          = 1,
            IsCatalog        = true,
            Sector           = "sante",
            EstimatedMinutes = 36,
            Tags             = "sante,ransomware,iot-medical,supply-chain,incident-response,avance",
            CreatedBy        = CatalogSeedBase.CatalogAuthorId,
            CreatedAt        = now,
            PublishedAt      = now
        });

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = Module1Id,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Attaques ciblées",
            SortOrder = 1,
            CreatedAt = now
        });

        await CatalogSeedBase.UpsertModuleAsync(db, new Module
        {
            Id        = Module2Id,
            TenantId  = CatalogSeedBase.CatalogTenantId,
            PathId    = PathId,
            Title     = "Réponse à incident",
            SortOrder = 2,
            CreatedAt = now
        });

        await SeedModule1Async(db, now);
        await SeedModule2Async(db, now);

        await CatalogSeedBase.EnsureDemoAccessAsync(db, PathId, now);
    }

    private static async Task SeedModule1Async(AppDbContext db, DateTime now)
    {
        // ── M1C1 — Infiltration initiale par email piégé vers le SIH (mailbox) ──
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M1C1Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "mailbox",
            Category     = "Analyse Email",
            Title        = "Boîte DSI — détectez la porte d'entrée du ransomware",
            Instructions = "Vous êtes administrateur du Système d'Information Hospitalier (SIH) du Groupe Hospitalier Nord-Est (GHNE). Examinez votre messagerie du matin et cochez uniquement les emails qui représentent un vecteur d'intrusion réel.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "emails": [
    {
      "id": "sih_mail_1",
      "from_name": "NexusMed Support",
      "from_address": "support@nexusmed-update-portal.com",
      "subject": "Mise à jour critique DPI NexusMed 7.4.2 — patch sécurité",
      "preview": "Une vulnérabilité critique a été identifiée sur votre DPI NexusMed...",
      "sent_at": "Aujourd'hui 07h12",
      "is_dangerous": true,
      "body": "Bonjour,\n\nUne vulnérabilité critique (CVE-2026-18472) a été identifiée sur votre Dossier Patient Informatisé NexusMed en version 7.4.2.\n\nMerci de télécharger et d'exécuter le patch de sécurité via le lien ci-dessous avant 18h ce jour :\n\n→ http://nexusmed-update-portal.com/patch/nexusmed_hotfix_742.exe\n\nLa mise à jour doit être lancée en session administrateur du serveur SIH.\n\nCordialement,\nÉquipe Sécurité NexusMed",
      "red_flags": [
        "Domaine expéditeur suspect (nexusmed-update-portal.com) différent du domaine éditeur officiel",
        "Lien direct vers un exécutable .exe — jamais diffusé ainsi par un éditeur sérieux",
        "Urgence artificielle (avant 18h) pour court-circuiter la procédure de mise à jour",
        "Demande d'exécution en session admin du serveur SIH — escalade de privilèges immédiate",
        "Aucune validation par le canal officiel de NexusMed (portail partenaire, support ticket)"
      ]
    },
    {
      "id": "sih_mail_2",
      "from_name": "Mme Dubreuil — DTS",
      "from_address": "c.dubreuil@ghne.fr",
      "subject": "Comité sécurité SI — support de la réunion de lundi",
      "preview": "Bonjour, vous trouverez ci-joint le support de la prochaine réunion...",
      "sent_at": "Hier 17h45",
      "is_dangerous": false,
      "body": "Bonjour,\n\nVous trouverez sur le partage interne \\\\ghne-fs01\\DTS\\Comite le support de la réunion du comité sécurité SI de lundi prochain.\n\nPoint important à l'ordre du jour : revue du plan de reprise après ransomware suite à l'incident du CHU Valmont.\n\nCordialement,\nCharline Dubreuil\nDirectrice Technique Sécurité — GHNE",
      "red_flags": []
    },
    {
      "id": "sih_mail_3",
      "from_name": "Facturation GHNE",
      "from_address": "facturation@ghne.fr",
      "subject": "Planning astreinte DSI semaine 17",
      "preview": "Bonjour, merci de valider votre créneau d'astreinte...",
      "sent_at": "Hier 11h03",
      "is_dangerous": false,
      "body": "Bonjour,\n\nMerci de valider votre créneau d'astreinte DSI pour la semaine 17 dans le portail RH interne avant vendredi midi.\n\nLien portail : https://rh.ghne.fr/astreintes\n\nCordialement,\nService RH — GHNE",
      "red_flags": []
    },
    {
      "id": "sih_mail_4",
      "from_name": "CERT Santé",
      "from_address": "alert@cert-sante-notification.fr",
      "subject": "ALERTE AnthemLock — scan urgent requis sur votre infrastructure",
      "preview": "Le groupe AnthemLock cible actuellement les établissements hospitaliers...",
      "sent_at": "Aujourd'hui 08h02",
      "is_dangerous": true,
      "body": "Madame, Monsieur,\n\nLe CERT Santé émet une alerte urgente : le groupe ransomware AnthemLock cible actuellement les établissements hospitaliers français.\n\nPour évaluer votre exposition, veuillez exécuter notre outil de scan dédié :\n\n→ Téléchargez cert-sante-scan.ps1 : http://cert-sante-notification.fr/tools/scan.ps1\n\nExécutez en PowerShell administrateur et renvoyez le fichier de log généré à alert@cert-sante-notification.fr.\n\nCERT Santé — Équipe d'Alerte",
      "red_flags": [
        "Domaine inhabituel (cert-sante-notification.fr ≠ cert.sante.gouv.fr)",
        "Le CERT Santé ne diffuse jamais de script PowerShell à exécuter directement par email",
        "Script .ps1 à exécuter en admin = backdoor potentielle",
        "Demande d'exfiltration des logs vers un domaine douteux",
        "Exploitation de l'actualité (alerte AnthemLock) pour crédibiliser l'attaque"
      ]
    },
    {
      "id": "sih_mail_5",
      "from_name": "Laboratoire Bio-Valmont",
      "from_address": "contact@biovalmont.fr",
      "subject": "Interconnexion HL7 — test de recette planifié jeudi",
      "preview": "Bonjour, nous vous confirmons le test de recette de notre flux HL7...",
      "sent_at": "Hier 14h30",
      "is_dangerous": false,
      "body": "Bonjour,\n\nNous vous confirmons le test de recette du flux HL7 entre votre SIH et notre LIMS, planifié jeudi 23 à 14h.\n\nLa fenêtre de tir dure environ 45 minutes. Un de nos ingénieurs sera joignable sur le numéro transmis précédemment par ticket sécurisé.\n\nCordialement,\nÉquipe intégration — Bio-Valmont",
      "red_flags": []
    },
    {
      "id": "sih_mail_6",
      "from_name": "Microsoft 365",
      "from_address": "no-reply@m365-admin-protection.com",
      "subject": "Votre compte administrateur SIH sera désactivé dans 3h",
      "preview": "Action requise : votre compte ghne-admin est identifié à risque...",
      "sent_at": "Aujourd'hui 06h48",
      "is_dangerous": true,
      "body": "Alerte sécurité Microsoft 365\n\nVotre compte administrateur 'ghne-admin@ghne.fr' est identifié à risque. Il sera désactivé dans 3 heures si aucune action n'est prise.\n\n→ Réauthentifier ce compte : http://m365-admin-protection.com/verify?account=ghne-admin\n\nÉquipe Sécurité Microsoft 365",
      "red_flags": [
        "Domaine non officiel (m365-admin-protection.com ≠ microsoft.com / login.microsoftonline.com)",
        "Cible explicitement le compte administrateur du SIH = escalade maximale",
        "Urgence extrême (3h) pour provoquer une action sans vérification",
        "Microsoft n'envoie jamais ce type d'alerte depuis un domaine tiers",
        "Le lien vise un portail de capture d'identifiants admin"
      ]
    }
  ]
}
"""
        });

        // ── M1C2 — Compromission IoT : pompe à insuline connectée (multichoice) ──
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M1C2Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "IoT Médical",
            Title        = "Pompe à insuline compromise — service endocrinologie",
            Instructions = "Le service endocrinologie du CHU Valmont signale des anomalies sur une pompe à insuline IoT. Identifiez la nature de l'attaque et les actions à engager.",
            Difficulty   = 4,
            Points       = 225,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "Dr. Hakim Belkacem — Endocrinologie",
    "from_address": "h.belkacem@chu-valmont.fr",
    "to": "dsi-securite@ghne.fr",
    "subject": "URGENT — anomalies pompes à insuline connectées service E3",
    "sent_at": "Aujourd'hui 09h24",
    "body": "Bonjour,\n\nDepuis ce matin, l'infirmière référente du service E3 signale que plusieurs pompes à insuline connectées (modèle ArtaMed IP-2200) affichent un comportement inhabituel :\n\n- bolus non programmés pendant la nuit sur deux patients, sans alerte médecin\n- console de supervision qui se déconnecte toutes les 10 minutes\n- dans les logs réseau de la VLAN 'IoT_Soins', des connexions sortantes récurrentes vers une IP hébergée à l'étranger\n\nNous avons isolé les deux patients concernés, aucun incident clinique grave à ce stade. Pouvez-vous lancer une investigation côté SI ?\n\nCordialement,\nDr. Belkacem"
  },
  "question": "Quelles interprétations et actions sont correctes face à ce signalement ?",
  "choices": [
    { "id": "A", "label": "Isoler immédiatement la VLAN 'IoT_Soins' en coupant ses routes sortantes et entrantes, sauf flux vitaux", "is_correct": true, "explanation": "Confinement réseau prioritaire. Tant que l'on n'a pas identifié la source de compromission, couper les flux externes de la VLAN IoT empêche l'exfiltration et les commandes malveillantes tout en préservant les flux locaux essentiels au monitoring infirmier." },
    { "id": "B", "label": "Ignorer le signalement : les pompes sont certifiées CE dispositif médical, elles ne peuvent pas être piratées", "is_correct": false, "explanation": "Faux. La certification CE dispositif médical couvre la sûreté clinique, pas la cybersécurité. De nombreux modèles IoT médicaux ont été compromis via des firmwares non signés, des comptes admin par défaut ou des canaux Bluetooth non chiffrés." },
    { "id": "C", "label": "Récupérer les logs réseau, les logs console et le firmware des pompes E3 avant tout redémarrage", "is_correct": true, "explanation": "La préservation des preuves est indispensable. Un redémarrage peut effacer la RAM et détruire les indicateurs de compromission. Les logs et le firmware seront nécessaires au CERT Santé et à l'éditeur pour l'analyse forensique." },
    { "id": "D", "label": "Déclarer l'événement indésirable grave (EIG) auprès de l'ARS et signaler au CERT Santé", "is_correct": true, "explanation": "Tout incident cyber affectant un dispositif médical connecté impliqué dans la prise en charge de patients doit être signalé : EIG à l'ARS (article L1413-14 CSP) et incident cyber au CERT Santé. La non-déclaration engage la responsabilité de l'établissement." },
    { "id": "E", "label": "Remettre les pompes en service dès qu'elles fonctionnent à nouveau normalement, sans modification", "is_correct": false, "explanation": "Catastrophique. Sans identification de la faille (firmware, compte par défaut, canal compromis), le même scénario se reproduira. Il faut au minimum : isoler, auditer, mettre à jour le firmware, changer les secrets, puis réintégrer progressivement." }
  ],
  "red_flags": [
    "Bolus non programmés = prise de contrôle à distance potentielle d'un dispositif médical",
    "Connexions sortantes régulières vers une IP étrangère = C2 (command & control) probable",
    "Console de supervision qui se déconnecte = tentative de masquer l'activité",
    "VLAN IoT médicale trop permissive vers Internet — erreur de segmentation classique",
    "Aucun SIEM couvrant les dispositifs médicaux connectés",
    "Risque vital direct si l'attaquant peut modifier la posologie à distance"
  ],
  "savoir_plus": "Les dispositifs médicaux connectés doivent être traités comme des endpoints critiques : inventaire, segmentation dédiée, SIEM, gestion des firmwares et des comptes par défaut. Le NIS2 étend les obligations aux 'entités essentielles' santé."
}
"""
        });

        // ── M1C3 — Supply chain pharma compromise (phishing_ai) ──────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M1C3Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Supply Chain",
            Title        = "Fournisseur pharmaceutique compromis — analyse libre",
            Instructions = "Un email provient apparemment de votre fournisseur pharmaceutique habituel, mais plusieurs éléments intriguent. Expliquez en détail pourquoi il s'agit très probablement d'une attaque supply chain.",
            Difficulty   = 4,
            Points       = 250,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "PharmaDis — Service Commandes",
    "from_address": "commandes@pharmadis-clients.fr",
    "to": "pharmacie@ghne.fr",
    "subject": "Mise à jour urgente procédure de commande — nouveau portail fournisseur",
    "sent_at": "Aujourd'hui 10h47",
    "body": "Chers partenaires,\n\nSuite à un incident technique interne survenu ce week-end, notre portail habituel commandes.pharmadis.fr est temporairement indisponible.\n\nAfin de garantir la continuité de vos approvisionnements (notamment opioïdes et anticoagulants de classe sensible), nous avons mis en place un portail de secours à utiliser jusqu'à rétablissement :\n\n→ https://pharmadis-clients.fr/portal\n\nPour votre première connexion, veuillez renseigner :\n• Votre identifiant PharmaDis habituel\n• Votre mot de passe\n• Le code interne de votre établissement\n• Un numéro de carte bancaire professionnelle pour validation de session\n\nAfin de ne pas perturber vos commandes de la semaine, merci de basculer immédiatement.\n\nCordialement,\nÉquipe Support — PharmaDis"
  },
  "question": "Expliquez pourquoi cet email est très probablement une attaque supply chain pharmaceutique, et décrivez les étapes de vérification et de réponse que vous engageriez en tant que responsable pharmacie d'un établissement hospitalier.",
  "expected_elements": "Domaine différent du domaine officiel connu (pharmadis-clients.fr au lieu de pharmadis.fr / commandes.pharmadis.fr). Demande de mot de passe par email = jamais légitime. Demande d'un numéro de carte bancaire sans rapport avec le processus d'achat hospitalier (passage par marché et bons de commande). Ciblage des opioïdes et anticoagulants = détournement pharmaceutique. Exploitation d'un prétexte d'incident pour forcer un changement de portail. Vérification : appeler le contact commercial habituel sur le numéro connu, consulter le vrai portail, croiser avec les autres établissements. Réponse : ne pas cliquer, signaler au RSSI, au CERT Santé, vérifier qu'aucun collaborateur n'a déjà saisi ses identifiants, reset éventuel, communication interne.",
  "min_chars": 220,
  "hint": "Regardez le domaine, le type d'informations demandées, le type de produits mis en avant, et le canal d'annonce d'un tel changement."
}
"""
        });

        // ── M1C4 — Wiper ciblant la radiologie (ceo_fraud) ────────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M1C4Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module1Id,
            Type         = "interactive",
            ContentType  = "ceo_fraud",
            Category     = "Ingénierie Sociale",
            Title        = "DTS piégée — faux ordre d'urgence sur la radiologie",
            Instructions = "Vous êtes administrateur système. Vous recevez un message interne qui semble émaner de la Directrice Technique Sécurité, Mme Dubreuil, demandant une action critique sur les serveurs de radiologie. Choisissez la ou les bonnes réactions.",
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
    "from_name": "Charline Dubreuil — DTS",
    "from_address": "c.dubreuil@ghne-direction-securite.fr",
    "to": "sysadmin@ghne.fr",
    "subject": "CONFIDENTIEL — lancer immédiatement script d'urgence sur RADIO-SRV",
    "sent_at": "Aujourd'hui 13h02",
    "body": "Bonjour,\n\nJe vous contacte depuis l'extérieur, ma session VPN ne passe pas correctement.\n\nSuite à l'alerte AnthemLock reçue ce matin du CERT Santé, nous devons lancer une contre-mesure immédiate sur les serveurs de radiologie (RADIO-SRV01 et RADIO-SRV02) AVANT la réunion de crise de 14h.\n\nMerci d'exécuter en tant que SYSTEM le script 'ghne_emergency_purge.ps1' que je vous partage ci-dessous, sans concertation avec l'équipe, pour ne pas éveiller l'attention d'un éventuel opérateur interne compromis.\n\nLien : http://ghne-direction-securite.fr/emergency/ghne_emergency_purge.ps1\n\nJe vous fais confiance, c'est confidentiel, n'en parlez à personne avant la réunion.\n\nCharline Dubreuil\nDirectrice Technique Sécurité"
  },
  "choices": [
    { "id": "exec",    "label": "Exécuter immédiatement le script, l'urgence est réelle et la DTS a autorité", "icon": "bank",  "is_correct": false, "explanation": "C'est exactement le piège attendu. Un 'script d'urgence' à passer en SYSTEM sans revue est typique d'un wiper (destruction des données). Aucune procédure d'urgence légitime ne demande une exécution silencieuse et non tracée." },
    { "id": "callback","label": "Rappeler Mme Dubreuil sur son numéro du répertoire interne avant toute action", "icon": "phone", "is_correct": true,  "explanation": "Vérification hors-bande indispensable. Une demande critique et confidentielle hors procédure doit toujours être reconfirmée par un canal indépendant, idéalement via le téléphone interne et non via un numéro donné dans le mail." },
    { "id": "report",  "label": "Alerter immédiatement le RSSI et la cellule d'astreinte sécurité",              "icon": "flag",  "is_correct": true,  "explanation": "L'injonction au secret envers l'équipe interne est un red flag majeur. Informer RSSI et astreinte permet de traiter la demande comme un incident potentiel plutôt qu'une opération légitime." },
    { "id": "ignore",  "label": "Ignorer le mail pour ne pas retarder la réunion de crise",                      "icon": "x",     "is_correct": false, "explanation": "Ignorer seul est insuffisant : le mail est un indicateur d'attaque active. Il doit être remonté pour analyse, préservation (headers, logs) et éventuelle corrélation avec d'autres signaux." }
  ],
  "red_flags": [
    "Domaine expéditeur ressemblant (ghne-direction-securite.fr ≠ ghne.fr)",
    "Demande explicite de confidentialité envers l'équipe = contournement des contrôles internes",
    "Exécution en SYSTEM d'un script téléchargé depuis un domaine externe",
    "Prétexte d'indisponibilité VPN pour justifier un canal inhabituel",
    "Ciblage des serveurs de radiologie, souvent couplés à du stockage d'imagerie non sauvegardé en temps réel",
    "Fenêtre d'urgence ('avant 14h') pour éviter toute revue"
  ]
}
"""
        });
    }

    private static async Task SeedModule2Async(AppDbContext db, DateTime now)
    {
        // ── M2C1 — Cellule de crise ransomware (multichoice) ─────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M2C1Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Gestion de Crise",
            Title        = "Cellule de crise AnthemLock — priorités des 60 premières minutes",
            Instructions = "Le SIH du GHNE est chiffré. Une demande de rançon AnthemLock s'affiche sur tous les postes. Vous êtes membre de la cellule de crise. Choisissez les priorités correctes pour la première heure.",
            Difficulty   = 4,
            Points       = 250,
            SortOrder    = 1,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "Chiffrement massif AnthemLock en cours au GHNE",
    "context": "06h41 — les postes du service urgences, réanimation et pharmacie affichent une note de rançon signée 'AnthemLock' exigeant 1,2 M€ en Bitcoin sous 72h. Le SIH (DPI NexusMed) est inaccessible.\n\nLe PC Sécurité remonte que le chiffrement semble actif sur au moins 40 % des serveurs, dont les serveurs PACS (imagerie) et LIMS (laboratoire). Les sauvegardes journalières sont hébergées sur un NAS interne connecté au même domaine Active Directory.\n\nMme Dubreuil convoque la cellule de crise. Vous devez définir les priorités de la première heure.",
    "icon": "alert"
  },
  "question": "Quelles actions sont à engager en priorité dans les 60 premières minutes ?",
  "choices": [
    { "id": "A", "label": "Isoler immédiatement le réseau : couper les liens WAN, VPN, inter-sites, et déconnecter les NAS de sauvegarde du domaine",                                "is_correct": true,  "explanation": "La priorité absolue est d'empêcher la propagation et la destruction des sauvegardes. Les NAS joints au domaine AD sont régulièrement chiffrés aussi — les déconnecter peut sauver la capacité de restauration." },
    { "id": "B", "label": "Lancer le Plan Blanc cyber et activer les procédures dégradées papier dans les services critiques (urgences, réa, bloc, maternité)",                     "is_correct": true,  "explanation": "Obligatoire. La continuité des soins est une mission de service public. Le Plan Blanc et les procédures papier (prescription, transmissions, identito-vigilance) doivent être activés sans attendre la résolution technique." },
    { "id": "C", "label": "Payer rapidement la rançon de 1,2 M€ pour récupérer l'accès au SIH avant le début des opérations programmées",                                          "is_correct": false, "explanation": "Le paiement est déconseillé par l'ANSSI et le ministère de la Santé : aucune garantie de déchiffrement, financement du crime organisé, récidive très probable, et la clé fournie n'est souvent pas fiable. Le paiement peut en outre être qualifié pénalement dans certains contextes." },
    { "id": "D", "label": "Notifier sans délai le CERT Santé, l'ANSSI, l'ARS, la CNIL (violation de données), et déposer plainte",                                                 "is_correct": true,  "explanation": "Obligations croisées : CERT Santé (incident cyber santé), ANSSI (OIV/OSE), ARS (événement indésirable grave), CNIL (violation de données sous 72h, art. 33 RGPD), plainte (preuve pénale). La non-notification engage la responsabilité." },
    { "id": "E", "label": "Communiquer immédiatement sur les réseaux sociaux et à la presse le montant exact de la rançon et l'identité supposée des attaquants",                 "is_correct": false, "explanation": "Dangereux. La communication publique doit être coordonnée avec la direction, la cellule communication, les autorités et les conseils juridiques. Une fuite désorganisée peut aggraver la situation et compromettre l'enquête." }
  ],
  "red_flags": [
    "Sauvegardes jointes au domaine AD = chiffrement cascade quasi garanti",
    "Chiffrement simultané multi-serveurs = propagation avancée avant détection",
    "Rançon Bitcoin = anonymat de l'attaquant, paiement difficilement traçable",
    "Délai court (72h) = pression sur la gouvernance",
    "Hôpital = risque vital direct, pression médiatique maximale",
    "Exfiltration probable avant chiffrement (double extorsion AnthemLock)"
  ],
  "savoir_plus": "Le ministère de la Santé impose un Plan Blanc cyber à tous les établissements. L'ANSSI publie un guide dédié 'cybersécurité hospitalière'. Les sauvegardes doivent être isolées (offline / immutable / hors domaine)."
}
"""
        });

        // ── M2C2 — Demande de rançon Bitcoin (phishing_ai) ───────────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M2C2Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "phishing_ai",
            Category     = "Négociation & Rançon",
            Title        = "Négociation AnthemLock — que répondre ?",
            Instructions = "La cellule de crise vous soumet le message reçu des attaquants. La direction hésite sur la posture à adopter. Expliquez ce qu'il faut faire et ne pas faire.",
            Difficulty   = 4,
            Points       = 225,
            SortOrder    = 2,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "email": {
    "from_name": "AnthemLock Team",
    "from_address": "contact@anthemlock-negotiate.onion",
    "to": "direction@ghne.fr",
    "subject": "GHNE — negotiation room opened — 72h countdown",
    "sent_at": "Aujourd'hui 09h15",
    "body": "Hello GHNE,\n\nYou have 72 hours to pay 1 200 000 EUR in Bitcoin to wallet bc1q... to receive the decryption key.\n\nWe have also exfiltrated 640 GB of data from your NexusMed DPI, including patient records, staff credentials, financial data and contracts with NexusMed.\n\nIf you refuse or try to involve media / law enforcement, we will :\n- double the amount after 72h,\n- publish the data on our leak site,\n- specifically release oncology and psychiatry records of minors.\n\nYou can chat with us in our negotiation room (link inside readme.txt) — we recommend paying quickly. Other hospitals paid and got their data back.\n\n— AnthemLock"
  },
  "question": "La direction vous demande votre avis en tant que RSSI. Expliquez ce que vous recommandez concrètement : faut-il engager la négociation, payer, ignorer ? Que faire des menaces d'exfiltration ? Quelles instances mobiliser et dans quel ordre ?",
  "expected_elements": "Ne pas payer : aucune garantie, financement du crime, récidive. Ne pas répondre directement aux attaquants depuis des comptes officiels. Impliquer immédiatement un négociateur professionnel spécialisé (via les autorités ou un partenaire cyber) avant tout contact, uniquement pour gagner du temps, pas pour payer. Impliquer : ANSSI, CERT Santé, ARS, ministère de la Santé, police judiciaire (BL2C / C3N), CNIL, conseil juridique, DPO. Préparer une communication aux patients (violation données sensibles, article 34 RGPD). Préparer la défense contre la double extorsion : notification préventive aux patients concernés, cellule d'écoute psy notamment pour oncologie / psychiatrie. Ne jamais céder à la menace de publication de dossiers de mineurs — signaler pénalement l'aggravation (chantage + atteinte mineurs). Restaurer à partir de sauvegardes offline. Maintenir le Plan Blanc.",
  "min_chars": 260,
  "hint": "Pensez : instances à saisir, rôle du négociateur, double extorsion, obligations RGPD, communication patients, restauration technique."
}
"""
        });

        // ── M2C3 — Activation du PCA hospitalier (multichoice) ───────────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M2C3Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "multichoice",
            Category     = "Continuité d'Activité",
            Title        = "PCA hospitalier cyber — décisions critiques heure+2",
            Instructions = "Deux heures après l'activation du Plan Blanc cyber, vous devez arbitrer plusieurs décisions concrètes. Choisissez celles qui respectent le PCA d'un établissement de santé.",
            Difficulty   = 3,
            Points       = 200,
            SortOrder    = 3,
            Status       = "published",
            CreatedBy    = CatalogSeedBase.CatalogAuthorId,
            CreatedAt    = now,
            PublishedAt  = now,
            ContentJson  = """
{
  "scenario": {
    "title": "PCA activé — arbitrages à H+2",
    "context": "Le Plan Blanc cyber est actif depuis 2 heures. Les urgences tournent en mode papier, la maternité fonctionne sur procédures dégradées, le bloc a reporté les interventions programmées et maintient les urgences vitales.\n\nPlusieurs décisions doivent être arbitrées :\n- la pharmacie ne peut plus accéder au logiciel de prescription,\n- la biologie ne peut plus envoyer ses résultats via le SIH,\n- un transfert de patient vers le CHU Valmont est demandé,\n- des journalistes appellent le standard,\n- certains praticiens veulent rebrancher leurs postes personnels 'pour continuer à travailler'.",
    "icon": "alert"
  },
  "question": "Parmi ces décisions, lesquelles sont conformes au PCA d'un établissement de santé ?",
  "choices": [
    { "id": "A", "label": "Autoriser les praticiens à rebrancher leurs ordinateurs personnels non maîtrisés sur le réseau hospitalier pour 'continuer à travailler'",                "is_correct": false, "explanation": "Interdit. En pleine crise, brancher un poste non maîtrisé sur le réseau peut introduire une nouvelle compromission ou interférer avec l'investigation. Le PCA prévoit des postes dédiés 'mode dégradé' pré-configurés." },
    { "id": "B", "label": "Mettre en place pour la pharmacie un circuit papier validé : prescriptions manuscrites, doubles signatures, registre centralisé par service",            "is_correct": true,  "explanation": "Conforme. Le circuit papier d'urgence doit être prévu dans le PCA, avec procédures d'identito-vigilance, doubles signatures pour les médicaments à risque, et reconstitution systématique dans le SIH après rétablissement." },
    { "id": "C", "label": "Coordonner un transfert du patient vers le CHU Valmont par fiche papier sécurisée, avec appel téléphonique de confirmation entre médecins",             "is_correct": true,  "explanation": "Conforme. Les transferts doivent être doublés par un contact oral direct entre médecins pour garantir la continuité des soins malgré la perte d'interopérabilité du SIH. Le CHU doit être informé du contexte cyber." },
    { "id": "D", "label": "Répondre à la presse via la cellule communication, avec un message unique validé par direction + ARS, sans donner de détails techniques ou de rançon", "is_correct": true,  "explanation": "Conforme. Une seule voix, un message coordonné (direction + ARS + cellule cyber), centré sur la continuité des soins et la protection des patients. Ne pas divulguer détails techniques ni montant de la rançon." },
    { "id": "E", "label": "Détruire les journaux d'événements pour éviter qu'ils ne fuitent et donner une image rassurante",                                                        "is_correct": false, "explanation": "Interdit et illégal. Les logs sont des preuves indispensables pour l'enquête, les autorités, le CERT Santé et les assurances. Les détruire constitue une destruction de preuves et aggrave la responsabilité." }
  ],
  "red_flags": [
    "Rebranchement non maîtrisé = réintroduction de la menace",
    "Absence de procédure papier pré-validée = erreurs médicamenteuses",
    "Communication désorganisée = panique publique, perte de confiance",
    "Destruction de logs = obstruction à l'enquête + infraction",
    "Confusion entre rôles médical / technique en cellule de crise",
    "Oubli d'informer l'ARS et les établissements partenaires"
  ],
  "savoir_plus": "Le Plan Blanc cyber est un volet du Plan Blanc hospitalier. Il doit être exercé au moins une fois par an (retour d'expérience consigné), pré-affiché dans chaque service, avec kits papier prêts à l'emploi."
}
"""
        });

        // ── M2C4 — Retour d'expérience et durcissement (free_text) ───────────
        await CatalogSeedBase.UpsertChallengeAsync(db, new Challenge
        {
            Id           = M2C4Id,
            TenantId     = CatalogSeedBase.CatalogTenantId,
            ModuleId     = Module2Id,
            Type         = "interactive",
            ContentType  = "free_text",
            Category     = "REX & Durcissement",
            Title        = "Post-incident — plan de durcissement en 3 axes",
            Instructions = "Trois semaines après la crise, la direction vous demande de proposer un plan de durcissement. Rédigez vos réponses : l'IA évaluera la pertinence et la précision.",
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
      "question": "Quelles mesures techniques prioritaires mettriez-vous en place pour empêcher qu'un nouveau ransomware type AnthemLock puisse à nouveau chiffrer les sauvegardes du GHNE ?",
      "context": "Sauvegardes et résilience",
      "expected_elements": "Stratégie 3-2-1-1-0 : 3 copies, 2 supports différents, 1 hors site, 1 hors ligne (offline / air-gap), 0 erreur au test de restauration. Sauvegardes immutables (object lock). Isolation réseau et compte dédié (ne pas réutiliser les admins du domaine). Tests de restauration trimestriels documentés. Surveillance des volumes (anomalies de chiffrement). Séparation des domaines Active Directory pour les serveurs de sauvegarde.",
      "min_chars": 180,
      "hint": "Pensez règle 3-2-1-1-0, immutabilité, séparation de domaine, tests de restauration."
    },
    {
      "id": "q2",
      "question": "Comment renforceriez-vous la sécurité des dispositifs médicaux connectés (pompes, PACS, appareils d'imagerie, moniteurs) après l'incident sur l'endocrinologie ?",
      "context": "Sécurité IoT médical",
      "expected_elements": "Inventaire exhaustif (CMDB) des dispositifs médicaux connectés avec criticité clinique. Segmentation réseau dédiée (VLAN IoT médical) avec règles pare-feu strictes limitant les flux sortants. Désactivation des comptes par défaut, changement des secrets, mise à jour des firmwares avec politique éditeur. Intégration au SIEM. Contrats de maintenance incluant clauses cybersécurité (SBOM, délais de patch). Exigences cyber dans les marchés d'achat de dispositifs médicaux. Surveillance comportementale (UEBA).",
      "min_chars": 180,
      "hint": "Pensez inventaire, segmentation, SIEM, contrats fournisseurs, marchés publics."
    },
    {
      "id": "q3",
      "question": "Quelle stratégie humaine et organisationnelle mettriez-vous en place pour éviter que la DTS Mme Dubreuil soit usurpée ou que des exécutions silencieuses de scripts redeviennent possibles ?",
      "context": "Facteur humain et gouvernance",
      "expected_elements": "Procédures 'quatre yeux' obligatoires sur toute action critique (double validation). Formation continue et exercices red team / phishing réguliers. Processus formel d'urgence cyber, publié, connu de tous, avec canaux authentifiés (téléphone interne, portail). Interdiction explicite des exécutions en SYSTEM sans ticket, revue et signature du script. Gestion des identités privilégiées (PAM) avec enregistrement de session. Culture du signalement sans blâme (psychological safety). Exercices de cellule de crise au moins 1 fois par an. Politique explicite : aucun dirigeant ne demande jamais une action confidentielle hors procédure.",
      "min_chars": 200,
      "hint": "Pensez double validation, PAM, formation, exercices de crise, culture de signalement."
    }
  ]
}
"""
        });
    }
}
