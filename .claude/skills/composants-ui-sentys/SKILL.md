---
name: composants-ui-sentys
description: À appliquer pour créer/styliser des composants UI Sentys (Button, Card, Modal, Tabs, Badge, Table, Form). Conventions shadcn/ui + cva + cn, à la charte teal, accessibles (aria, focus, contraste).
---

# Composants UI Sentys (shadcn/ui + cva + cn)

Stack installée : `shadcn/ui` (infra via `components.json`), `class-variance-authority` (cva), `clsx` + `tailwind-merge` (`cn`), `@radix-ui/react-slot`, `lucide-react`, `tw-animate-css`. Tailwind **v4**.

## Utilitaire `cn` (déjà présent)
```ts
// src/lib/utils.ts
import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";
export function cn(...inputs: ClassValue[]) { return twMerge(clsx(inputs)); }
```

## Pattern de composant (cva + cn + Slot)
Référence : `src/components/ui/button.tsx` (déjà créé, à la charte teal). Variants : `default` (bg-sentys), `accent`, `outline`, `ghost`, `secondary`, `destructive`, `link` ; sizes avec **cibles tactiles ≥ 44px**.
```tsx
const badgeVariants = cva("inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium", {
  variants: { variant: {
    active:   "bg-success/10 text-success",
    inactive: "bg-danger/10 text-danger",
    pending:  "bg-warning/10 text-warning",
    brand:    "bg-sentys/10 text-sentys-dark",   // teal lisible (AA)
  }}, defaultVariants: { variant: "brand" },
});
```

## Ajouter un composant shadcn
```bash
cd frontend && npx shadcn@latest add dialog   # dropdown-menu, tabs, tooltip, table...
```
Après ajout : **remapper la couleur d'action sur la charte** — remplacer `bg-primary`/`text-primary` par `bg-sentys`/`text-sentys` (le token `primary` hérité est bleu). Voir [[charte-graphique-sentys]].

## Règles par composant
- **Card** : `rounded-xl border border-border bg-surface p-6 shadow-sm`. Padding interne **≥ 24px** (`p-6`). Barre de progression : `h-1.5 rounded-full bg-[#E2E8F0]` remplie `bg-sentys`.
- **Modal / Dialog** : Radix Dialog, overlay `bg-black/50 backdrop-blur-sm`, panneau `rounded-xl bg-surface p-6`, focus trap natif Radix, `Esc` pour fermer. Jamais `window.alert/confirm`.
- **Tabs** : onglet actif `bg-sentys text-white` (teinte pleine, pas d'alpha), inactif `text-fg-muted hover:text-fg-body`.
- **Table admin** : header `bg-table-head uppercase text-xs tracking-wider text-fg-muted`, lignes `divide-y divide-border` (bordures **horizontales seules**), `hover:bg-canvas`. Envelopper dans `overflow-x-auto` (responsive).
- **Form** : `<label>` lié (`htmlFor`), input `min-h-[44px] rounded-lg border border-border focus-visible:ring-2 focus-visible:ring-sentys/50`, message d'erreur `text-danger text-xs` + `aria-invalid`.

## Accessibilité (obligatoire)
- Tout élément interactif : `focus-visible:ring-2 focus-visible:ring-sentys/50 focus-visible:ring-offset-2`.
- Icône seule cliquable : `aria-label`. Icône décorative : `aria-hidden`.
- Contraste AA (cf. [[charte-graphique-sentys]]) : teal petit texte → `text-sentys-dark`.
- `transition-colors duration-200` sur tout hover.
- Zéro `any` TS : props typées, `VariantProps<typeof xVariants>`.
