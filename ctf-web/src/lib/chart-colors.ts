// FICHIER DE TOKENS DATAVIZ Sentys (charte noir + vert cyber).
// NB : les `fill`/`stroke` recharts sont des ATTRIBUTS SVG qui ne résolvent pas var(),
// d où des hex ici (ce fichier EST le fichier de tokens graphiques). Les props CSS
// (grid/axis/tooltip) utilisent var() et suivent le thème.
export const CHART = {
    accent: "#7551FF",       // série principale violet (attribut SVG -> hex)
    accent2: "#2CD9FF",      // cyan (dégradé violet→cyan, signature Vision UI)
    grid: "var(--border)",   // props CSS -> var OK
    axis: "var(--text-3)",
    text: "var(--text-2)",
    surface: "var(--surface)",
};
// Séries distinctes (donut/barres) — charte Vision UI : violet / cyan / succès / ambre / accent-2 / info.
export const SERIES = ["#7551FF", "#2CD9FF", "#01B574", "#FFB547", "#582CFF", "#3965FF"];
// Statuts métier (recharts fill = attribut SVG -> hex requis). Charte violet : gris/ambre/succès/danger.
export const STATUS = { grey: "#718096", yellow: "#FFB547", green: "#01B574", red: "#EE5D50" } as const;
