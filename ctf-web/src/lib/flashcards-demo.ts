// Données DÉMO en dur pour le menu de test isolé /flashcards-test.
// Aucune table BDD, aucun lien avec les parcours réels. Jeu de cartes cyber.

export type Flashcard = {
    id: string;
    category: string; // ex. "Phishing"
    front: string; // recto : question / terme
    back: string; // verso : réponse + définition courte
    choices: string[]; // options QCM (mode Épreuve), dont la bonne réponse
    correctIndex: number; // index de la bonne réponse dans `choices`
};

export const DEMO_FLASHCARDS: Flashcard[] = [
    {
        id: "phishing",
        category: "Ingénierie sociale",
        front: "Qu'est-ce que le phishing (hameçonnage) ?",
        back: "Une tentative d'usurpation (email, SMS, site) visant à vous faire divulguer des informations sensibles ou cliquer sur un lien piégé.",
        choices: [
            "Un email/site frauduleux qui usurpe une identité de confiance pour voler des données",
            "Un logiciel qui accélère la connexion réseau",
            "Une mise à jour automatique du système d'exploitation",
            "Un pare-feu matériel installé par la DSI",
        ],
        correctIndex: 0,
    },
    {
        id: "2fa",
        category: "Authentification",
        front: "À quoi sert l'authentification à deux facteurs (2FA) ?",
        back: "À ajouter une seconde preuve d'identité (code, appli, clé) en plus du mot de passe, pour bloquer un accès même si le mot de passe fuit.",
        choices: [
            "À remplacer définitivement le mot de passe par un visage",
            "À ajouter une 2e preuve d'identité en plus du mot de passe",
            "À chiffrer le disque dur de l'ordinateur",
            "À sauvegarder les fichiers dans le cloud",
        ],
        correctIndex: 1,
    },
    {
        id: "ransomware",
        category: "Malware",
        front: "Qu'est-ce qu'un ransomware (rançongiciel) ?",
        back: "Un logiciel malveillant qui chiffre vos fichiers et exige une rançon pour les déverrouiller. La sauvegarde hors-ligne est la meilleure défense.",
        choices: [
            "Un antivirus gratuit fourni par l'État",
            "Un gestionnaire de mots de passe",
            "Un malware qui chiffre vos fichiers et réclame une rançon",
            "Un protocole de messagerie sécurisée",
        ],
        correctIndex: 2,
    },
    {
        id: "mdp-fort",
        category: "Mots de passe",
        front: "Qu'est-ce qui caractérise un mot de passe robuste ?",
        back: "Long (12+ caractères), unique par service, imprévisible. Une phrase de passe + un gestionnaire de mots de passe est l'idéal.",
        choices: [
            "Le nom de l'entreprise suivi de l'année",
            "Long, unique par service et imprévisible (phrase de passe)",
            "Un mot court mais changé chaque jour",
            "Votre date de naissance en chiffres",
        ],
        correctIndex: 1,
    },
    {
        id: "arnaque-president",
        category: "Fraude",
        front: "Qu'est-ce que l'« arnaque au président » (fraude au virement) ?",
        back: "Un attaquant se fait passer pour un dirigeant et exige un virement urgent et confidentiel. La parade : valider hors canal (appel interne).",
        choices: [
            "Une élection interne du comité de direction",
            "Un virement bancaire automatisé et sécurisé",
            "Une usurpation d'un dirigeant pour obtenir un virement urgent",
            "Une prime versée par la direction en fin d'année",
        ],
        correctIndex: 2,
    },
    {
        id: "wifi-public",
        category: "Réseau",
        front: "Quel est le risque d'un Wi-Fi public ouvert ?",
        back: "Le trafic peut être intercepté et des points d'accès pirates imités. Un VPN et le HTTPS réduisent fortement le risque.",
        choices: [
            "Il recharge automatiquement la batterie",
            "Le trafic peut être intercepté par un tiers sur le même réseau",
            "Il améliore la confidentialité par défaut",
            "Il bloque tous les sites malveillants",
        ],
        correctIndex: 1,
    },
    {
        id: "maj-secu",
        category: "Hygiène",
        front: "Pourquoi appliquer rapidement les mises à jour de sécurité ?",
        back: "Elles corrigent des failles connues activement exploitées. Retarder une mise à jour laisse une porte ouverte aux attaquants.",
        choices: [
            "Pour changer la couleur de l'interface",
            "Pour libérer de l'espace disque",
            "Pour corriger des failles connues avant qu'elles soient exploitées",
            "Uniquement pour obtenir de nouvelles fonctionnalités",
        ],
        correctIndex: 2,
    },
    {
        id: "signalement",
        category: "Réflexe",
        front: "Que faire face à un email suspect au travail ?",
        back: "Ne pas cliquer, ne pas répondre : signaler à l'équipe sécurité / RSSI via le canal prévu. Le signalement protège toute l'organisation.",
        choices: [
            "Le transférer à tous ses collègues pour prévenir",
            "Cliquer sur le lien pour vérifier s'il est réel",
            "Le signaler à l'équipe sécurité sans cliquer ni répondre",
            "Le supprimer discrètement et ne rien dire",
        ],
        correctIndex: 2,
    },
];
