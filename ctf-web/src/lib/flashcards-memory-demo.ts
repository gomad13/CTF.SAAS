// Données DÉMO pour le mode Memory (association par paires) du menu isolé /flashcards-test.
// Thématique cyber : chaque paire associe un RISQUE à sa CONTRE-MESURE. Textes courts (grille).
// Aucune BDD, aucun lien avec les parcours réels.

export type MemoryPair = {
    id: string;
    risk: string; // le risque
    counter: string; // la contre-mesure associée
};

export const MEMORY_PAIRS: MemoryPair[] = [
    { id: "mdp", risk: "Mot de passe faible", counter: "Phrase de passe + gestionnaire" },
    { id: "phishing", risk: "Email de phishing", counter: "Vérifier l'expéditeur, ne pas cliquer" },
    { id: "wifi", risk: "Wi-Fi public ouvert", counter: "Utiliser un VPN" },
    { id: "ransomware", risk: "Ransomware", counter: "Sauvegardes hors-ligne" },
    { id: "vol", risk: "Perte / vol d'appareil", counter: "Chiffrement du disque" },
    { id: "compte", risk: "Compte compromis", counter: "Activer la 2FA" },
    { id: "maj", risk: "Logiciel obsolète", counter: "Mises à jour régulières" },
    { id: "usb", risk: "Clé USB inconnue", counter: "Ne pas la brancher" },
];

// Nombre de paires jouées (≤ MEMORY_PAIRS.length). 8 → 16 tuiles, grille 4×4.
export const MEMORY_PAIR_COUNT = 8;

export type MemoryTile = {
    uid: string; // identifiant unique de la tuile
    pairId: string; // deux tuiles de même pairId forment une paire
    label: string; // texte affiché
    role: "risk" | "counter";
};

/** Construit et mélange le deck (Fisher-Yates). Appelé à chaque nouvelle partie. */
export function buildShuffledDeck(pairCount: number = MEMORY_PAIR_COUNT): MemoryTile[] {
    const pairs = MEMORY_PAIRS.slice(0, Math.min(pairCount, MEMORY_PAIRS.length));
    const tiles: MemoryTile[] = pairs.flatMap((p) => [
        { uid: `${p.id}-risk`, pairId: p.id, label: p.risk, role: "risk" as const },
        { uid: `${p.id}-counter`, pairId: p.id, label: p.counter, role: "counter" as const },
    ]);
    for (let i = tiles.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [tiles[i], tiles[j]] = [tiles[j], tiles[i]];
    }
    return tiles;
}
