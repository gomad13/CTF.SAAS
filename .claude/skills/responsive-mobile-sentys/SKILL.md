---
name: responsive-mobile-sentys
description: À appliquer pour que tout composant/écran Sentys soit responsive mobile-first (cible 320px, zones tactiles 44px, pas de débordement horizontal, grilles adaptatives, menu burger).
---

# Responsive mobile-first Sentys

Tout nouvel écran doit être **utilisable dès 320px de large**, sans scroll horizontal.

## Breakpoints (Tailwind v4, définis dans globals.css `@theme`)
`sm:480px` · `md:768px` · `lg:1024px` · `xl:1440px` · `2xl:1600px` (min-width).
Écrire mobile d'abord, puis élargir : `class="p-4 sm:p-6 lg:p-8"`.

## Règles non négociables
1. **Cibles tactiles ≥ 44px** : boutons/liens/inputs `min-h-[44px]` (token `--tap-target-min`). Les tailles du `Button` shadcn respectent déjà 44px.
2. **Pas de débordement horizontal** : jamais de largeur fixe > écran. Tableaux et blocs larges → `overflow-x-auto`. Conteneur racine sans `w-[...px]` rigide.
3. **Grilles adaptatives** : `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4`. Cartes KPI : `grid-cols-1 sm:grid-cols-2 xl:grid-cols-4`.
4. **Flex qui wrap** : barres d'actions `flex flex-wrap items-center gap-3` (jamais figées sur une ligne).
5. **Padding de page** via token responsive : `px-[var(--page-x)]` (16px mobile, élargi en tablette/desktop).
6. **Navigation** : menu **burger** sous `md` (sidebar cachée `hidden md:flex`, drawer mobile). Zone de tap du burger ≥ 44px.
7. **Texte** : `text-sm` de base, `sm:text-base` ; titres `text-xl sm:text-2xl`. `leading-relaxed`.
8. **Images/QR/média** : `max-w-full h-auto`.
9. **Modales** : `w-full max-w-md` + `p-4` marge écran ; hauteur `max-h-[90svh] overflow-y-auto` (utiliser `svh`, pas `vh`).

## Exemples
```tsx
// Grille de cartes responsive
<div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">{cards}</div>

// Tableau qui ne déborde jamais
<div className="overflow-x-auto">
  <table className="w-full min-w-[640px] text-sm">…</table>
</div>

// Barre d'actions mobile-friendly
<div className="flex flex-wrap items-center gap-3">
  <Button size="default">Créer</Button>
  <Button variant="outline">Filtrer</Button>
</div>
```

## Vérification avant de clore
- Rendu à **320px** : pas de scroll horizontal, rien de coupé.
- Tous les boutons/inputs tapables au doigt (≥ 44px).
- Grilles passent bien en 1 colonne sur mobile.
