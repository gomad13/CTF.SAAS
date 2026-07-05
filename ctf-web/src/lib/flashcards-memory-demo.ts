// Données DÉMO pour le mode Memory du menu isolé /flashcards-test.
// Pool complet de paires cyber : RISQUE ↔ CONTRE-MESURE et TERME ↔ DÉFINITION.
// Chaque session tire aléatoirement MEMORY_PAIR_COUNT paires du pool → parties différentes.
// Aucune BDD, aucun lien avec les parcours réels.

export type PairKind = "risque-parade" | "terme-def";

export type MemoryPair = {
    id: string;
    kind: PairKind;
    left: string; // risque OU terme
    right: string; // contre-mesure OU définition
};

// Libellés volontairement courts (lisibilité en grille 6×2).
export const MEMORY_POOL: MemoryPair[] = [
    // Risque ↔ Contre-mesure — formulé pour un public NON technique (soignants, cadres, admin) :
    // risque du quotidien ↔ bon réflexe simple, aucun jargon, association UNIVOQUE (une parade = un seul risque).
    { id: "phishing", kind: "risque-parade", left: "Email frauduleux (faux expéditeur)", right: "Ne pas cliquer, le signaler" },
    { id: "usb", kind: "risque-parade", left: "Clé USB trouvée par terre", right: "Ne pas la brancher" },
    { id: "virement", kind: "risque-parade", left: "Virement urgent demandé par mail", right: "Confirmer de vive voix" },
    { id: "mdp-simple", kind: "risque-parade", left: "Mot de passe trop simple", right: "En choisir un plus long" },
    { id: "ecran", kind: "risque-parade", left: "Ordinateur laissé déverrouillé", right: "Verrouiller l'écran en partant" },
    { id: "appel-mdp", kind: "risque-parade", left: "Appel réclamant votre mot de passe", right: "Ne jamais le communiquer" },
    { id: "mdp-reuse", kind: "risque-parade", left: "Même mot de passe partout", right: "Un mot de passe par service" },
    { id: "postit", kind: "risque-parade", left: "Données patients sur un post-it", right: "Ranger les infos sensibles" },
    { id: "tel-perdu", kind: "risque-parade", left: "Téléphone pro perdu", right: "Prévenir le support tout de suite" },
    { id: "sms", kind: "risque-parade", left: "Lien reçu par SMS inattendu", right: "Supprimer sans cliquer" },
    { id: "porte", kind: "risque-parade", left: "Porte de bureau laissée ouverte", right: "Fermer à clé en partant" },
    { id: "doc-jete", kind: "risque-parade", left: "Document confidentiel jeté entier", right: "Le déchirer avant de jeter" },
    { id: "badge", kind: "risque-parade", left: "Badge d'accès prêté", right: "Ne pas prêter son badge" },
    // Terme ↔ Définition
    { id: "phishing-td", kind: "terme-def", left: "Phishing", right: "Usurpation par email piégé" },
    { id: "2fa", kind: "terme-def", left: "2FA", right: "Double preuve d'identité" },
    { id: "vpn", kind: "terme-def", left: "VPN", right: "Tunnel réseau chiffré" },
    { id: "malware", kind: "terme-def", left: "Malware", right: "Logiciel malveillant" },
    { id: "firewall", kind: "terme-def", left: "Pare-feu", right: "Filtre le trafic réseau" },
    { id: "chiffrement", kind: "terme-def", left: "Chiffrement", right: "Illisible sans la clé" },
    { id: "https", kind: "terme-def", left: "HTTPS", right: "Connexion web chiffrée" },
    { id: "antivirus", kind: "terme-def", left: "Antivirus", right: "Détecte et bloque les malwares" },
    { id: "social", kind: "terme-def", left: "Ingénierie sociale", right: "Manipuler pour obtenir un accès" },
    { id: "backup", kind: "terme-def", left: "Sauvegarde", right: "Copie pour restaurer les données" },
    { id: "trojan", kind: "terme-def", left: "Cheval de Troie", right: "Malware déguisé en logiciel sain" },
];

// Nombre de PAIRES par session → 6 paires = 12 tuiles, grille 6×2.
export const MEMORY_PAIR_COUNT = 6;

export type TileTone = "risk" | "counter" | "term" | "def";

export type MemoryTile = {
    uid: string; // identifiant unique de tuile
    pairId: string; // deux tuiles de même pairId forment une paire
    label: string; // texte affiché
    badge: string; // "Risque" | "Parade" | "Terme" | "Définition"
    tone: TileTone; // couleur du badge (token)
};

function sidesOf(pair: MemoryPair): [MemoryTile, MemoryTile] {
    const leftTone: TileTone = pair.kind === "risque-parade" ? "risk" : "term";
    const rightTone: TileTone = pair.kind === "risque-parade" ? "counter" : "def";
    const leftBadge = pair.kind === "risque-parade" ? "Risque" : "Terme";
    const rightBadge = pair.kind === "risque-parade" ? "Parade" : "Définition";
    return [
        { uid: `${pair.id}-l`, pairId: pair.id, label: pair.left, badge: leftBadge, tone: leftTone },
        { uid: `${pair.id}-r`, pairId: pair.id, label: pair.right, badge: rightBadge, tone: rightTone },
    ];
}

function shuffle<T>(arr: T[]): T[] {
    const a = [...arr];
    for (let i = a.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [a[i], a[j]] = [a[j], a[i]];
    }
    return a;
}

/**
 * Construit un deck pour UN SEUL type d'association (`kind`) : filtre le pool sur ce type,
 * tire `pairCount` paires AU HASARD, en fait des tuiles, puis mélange.
 * Une partie = un seul type (jamais de mélange) → jouable. Deux niveaux d'aléatoire → parties différentes.
 */
export function buildShuffledDeck(kind: PairKind, pairCount: number = MEMORY_PAIR_COUNT): MemoryTile[] {
    const pool = MEMORY_POOL.filter((p) => p.kind === kind);
    const n = Math.min(pairCount, pool.length);
    const pickedPairs = shuffle(pool).slice(0, n);
    const tiles = pickedPairs.flatMap((p) => sidesOf(p));
    return shuffle(tiles);
}
