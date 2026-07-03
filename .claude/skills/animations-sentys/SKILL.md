---
name: animations-sentys
description: À appliquer pour animer l'UI Sentys (framer-motion / tw-animate-css). Transitions sobres et pro, micro-interactions, jamais d'animations excessives. Respecte prefers-reduced-motion.
---

# Animations Sentys (framer-motion)

Librairies : `framer-motion` (installée) pour les animations JS ; `tw-animate-css` pour les utilitaires CSS (fondus/accordéons shadcn). **Sobre et pro** — l'animation sert la lisibilité, pas le spectacle.

## Principes
- **Durées courtes** : 150–250ms (entrées/hover), 300ms max (transitions de page). Jamais > 400ms.
- **Easing** : `ease-out` à l'entrée, `ease-in` à la sortie. Défaut framer : `{ duration: 0.2, ease: "easeOut" }`.
- **Déplacements discrets** : `y: 8→0`, `opacity: 0→1`. Pas de gros translates, bounce, rotation gratuite.
- **Respecter `prefers-reduced-motion`** : désactiver/réduire si l'utilisateur le demande (`useReducedMotion`).
- **Ne pas animer** : listes très longues, chaque ligne de tableau, éléments critiques (formulaires en cours de saisie).
- Hover d'un bouton = **changement de teinte** (`hover:bg-sentys-dark`), pas un scale animé.

## Presets réutilisables
```ts
// src/lib/motion.ts
export const fadeInUp = {
  initial: { opacity: 0, y: 8 },
  animate: { opacity: 1, y: 0 },
  exit:    { opacity: 0, y: 8 },
  transition: { duration: 0.2, ease: "easeOut" },
};
export const stagger = { animate: { transition: { staggerChildren: 0.05 } } };
```

## Exemples
```tsx
import { motion, useReducedMotion } from "framer-motion";
import { fadeInUp } from "@/lib/motion";

// Carte qui apparaît en douceur (respecte reduced-motion)
export function AnimatedCard({ children }: { children: React.ReactNode }) {
  const reduce = useReducedMotion();
  return (
    <motion.div {...(reduce ? {} : fadeInUp)} className="rounded-xl border border-border bg-surface p-6 shadow-sm">
      {children}
    </motion.div>
  );
}
```
```tsx
// Modale : présence animée (entrée/sortie)
import { AnimatePresence, motion } from "framer-motion";
<AnimatePresence>
  {open && (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
      transition={{ duration: 0.15 }} className="fixed inset-0 bg-black/50 backdrop-blur-sm" />
  )}
</AnimatePresence>
```

## À éviter
- Animations en boucle permanentes (sauf spinner de chargement).
- `transition-all` non borné → préférer `transition-colors` / `transition-transform`.
- Parallax, confettis, effets « waouh » : hors charte pro Sentys.
