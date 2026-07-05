// FICHIER DE TOKENS DATAVIZ Sentys (charte noir + vert cyber).
// NB : les `fill`/`stroke` recharts sont des ATTRIBUTS SVG qui ne résolvent pas var(),
// d où des hex ici (ce fichier EST le fichier de tokens graphiques). Les props CSS
// (grid/axis/tooltip) utilisent var() et suivent le thème.
export const CHART = {
    accent: "#22C55E",       // série principale (attribut SVG -> hex)
    accent2: "#2DD4BF",      // cyan (dégradé vert→cyan)
    grid: "var(--border)",   // props CSS -> var OK
    axis: "var(--text-3)",
    text: "var(--text-2)",
    surface: "var(--surface)",
};
// Séries distinctes (donut/barres) — mappées sur la charte : vert / ambre / gris / cyan…
export const SERIES = ["#22C55E", "#F59E0B", "#6B6B70", "#2DD4BF", "#4ADE80", "#3B82F6"];
// Statuts métier (recharts fill = attribut SVG -> hex requis). Charte : gris/ambre/vert/rouge.
export const STATUS = { grey: "#6B6B70", yellow: "#F59E0B", green: "#22C55E", red: "#EF4444" } as const;
