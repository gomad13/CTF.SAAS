// Palette dataviz Sentys (charte vert cyber, theme-aware via var()).
// recharts accepte var(--x) en fill/stroke sur les navigateurs modernes.
export const CHART = {
    accent: "var(--accent)",
    accent2: "var(--accent-hover)",
    grid: "var(--border)",
    axis: "var(--text-3)",
    text: "var(--text-2)",
    surface: "var(--surface)",
};
// Séries distinctes (donut/barres multi-séries) : dégradé de verts + neutres.
export const SERIES = ["var(--accent)", "#4ADE80", "#15803D", "var(--text-3)", "#F59E0B", "#3B82F6"];
