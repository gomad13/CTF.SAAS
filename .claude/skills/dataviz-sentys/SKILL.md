---
name: dataviz-sentys
description: À appliquer pour créer des graphiques Sentys (recharts) sur les dashboards Analytics / compétition / progression. Couleurs de la charte teal, lisibilité, responsive.
---

# Dataviz Sentys (recharts)

Librairie : `recharts` (installée). Toujours **responsive** et à la charte teal.

## Palette de séries (ordre recommandé)
```ts
export const CHART_COLORS = {
  primary:  "#03b5aa", // sentys — série principale
  accent:   "#00bfb3", // sentys-accent — 2e série
  dark:     "#037971", // sentys-dark — 3e série / hover
  success:  "#10B981",
  warning:  "#F59E0B",
  danger:   "#EF4444",
  grid:     "#E2E8F0", // bordures/grille
  axis:     "#64748B", // labels d'axe (fg-muted)
};
```
Sur fond sombre (`#023436`), inverser les textes en `#CBD5E1` / `#FFFFFF`.

## Règles
- **Responsive obligatoire** : envelopper dans `<ResponsiveContainer width="100%" height={280}>`. Jamais de largeur fixe en px.
- **Lisibilité** : `CartesianGrid stroke={grid}` discret, axes `tick={{ fill: axis, fontSize: 12 }}`, `Tooltip` avec fond `surface` + bordure `border`.
- **Couleurs charte** : série principale = `#03b5aa`, jamais les couleurs par défaut de recharts.
- **Aires** : dégradé teal léger (`stopColor #03b5aa` de 0.25 → 0.02).
- **Accessibilité** : ne pas coder l'info uniquement par la couleur (légende + labels). Contraste des labels AA.
- **Pas de surcharge** : max ~5 séries ; au-delà, agréger.

## Exemple (aire de progression)
```tsx
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";
import { CHART_COLORS as C } from "@/lib/chart-colors";

export function ProgressArea({ data }: { data: { date: string; pct: number }[] }) {
  return (
    <ResponsiveContainer width="100%" height={280}>
      <AreaChart data={data} margin={{ top: 8, right: 12, left: 0, bottom: 0 }}>
        <defs>
          <linearGradient id="teal" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={C.primary} stopOpacity={0.25} />
            <stop offset="100%" stopColor={C.primary} stopOpacity={0.02} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke={C.grid} vertical={false} />
        <XAxis dataKey="date" tick={{ fill: C.axis, fontSize: 12 }} tickLine={false} axisLine={{ stroke: C.grid }} />
        <YAxis tick={{ fill: C.axis, fontSize: 12 }} tickLine={false} axisLine={false} width={32} />
        <Tooltip contentStyle={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 10, fontSize: 12 }} />
        <Area type="monotone" dataKey="pct" stroke={C.primary} strokeWidth={2} fill="url(#teal)" />
      </AreaChart>
    </ResponsiveContainer>
  );
}
```

Barres : `fill="#03b5aa"`, coins `radius={[6,6,0,0]}`. Camemberts : palette `[primary, accent, dark, success, warning]`, `paddingAngle={2}`.
