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
    // Risque ↔ Contre-mesure
    { id: "mdp", kind: "risque-parade", left: "Mot de passe faible", right: "Gestionnaire + 2FA" },
    { id: "phishing-rp", kind: "risque-parade", left: "Email de phishing", right: "Ne pas cliquer, signaler" },
    { id: "wifi", kind: "risque-parade", left: "Wi-Fi public ouvert", right: "Utiliser un VPN" },
    { id: "ransomware", kind: "risque-parade", left: "Ransomware", right: "Sauvegardes hors-ligne" },
    { id: "vol", kind: "risque-parade", left: "Vol d'appareil", right: "Chiffrement du disque" },
    { id: "compte", kind: "risque-parade", left: "Compte compromis", right: "Activer la 2FA" },
    { id: "maj", kind: "risque-parade", left: "Logiciel obsolète", right: "Mises à jour régulières" },
    { id: "usb", kind: "risque-parade", left: "Clé USB inconnue", right: "Ne pas la brancher" },
    { id: "president", kind: "risque-parade", left: "Fraude au virement", right: "Valider par appel interne" },
    { id: "fuite", kind: "risque-parade", left: "Fuite de données", right: "Accès au strict besoin" },
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
