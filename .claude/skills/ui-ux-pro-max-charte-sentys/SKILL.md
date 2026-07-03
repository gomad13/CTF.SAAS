---
name: ui-ux-pro-max-charte-sentys
description: À appliquer chaque fois qu'on utilise le skill ui-ux-pro-max sur le projet Sentys. Le skill guide la STRUCTURE / layout / UX / accessibilité / dataviz, mais la PALETTE DE COULEURS reste TOUJOURS la charte Sentys (teal). Ne jamais adopter les couleurs suggérées par ui-ux-pro-max.
---

# ui-ux-pro-max × charte Sentys (règle d'articulation)

Le skill **`ui-ux-pro-max`** (installé dans `.agents/skills/ui-ux-pro-max`, symlinké dans `.claude/skills/`) est une base de recommandations design (styles, patterns, typographies, UX, accessibilité, charts). On l'utilise, MAIS avec une garde stricte sur les couleurs.

## Règle (non négociable)
> **Utiliser ui-ux-pro-max pour le « comment » : structure, layout, hiérarchie d'information, patterns de composants, interactions, animations, accessibilité, choix de graphiques.**
> **NE PAS adopter la palette de couleurs qu'il propose.** La palette reste la charte Sentys :
> `primary #03b5aa` · `primary-dark #037971` · `background-dark #023436` · `accent #00bfb3`, contraste WCAG AA.

Exemple : pour « healthcare SaaS dashboard », le skill propose souvent un primary cyan (`#0891B2`) / vert. **On ignore ces couleurs** et on mappe sur la charte : `primary → #03b5aa` (`bg-sentys`), `accent → #00bfb3` (`bg-sentys-accent`), fond sombre → `#023436` (`bg-sentys-bg`). Voir [[charte-graphique-sentys]].

## Ce qu'on GARDE du skill
- **Pattern / structure de page** (hero, sections, hiérarchie, placement des CTA).
- **Style & UX** (ex. « Accessible & Ethical » : haut contraste, 16px+, focus states, semantic).
- **Typographies** suggérées (si cohérentes ; sinon garder Inter, cf. charte).
- **Effets & checklist a11y** : focus rings 3-4px, ARIA, skip links, cibles tactiles **44×44px**, `prefers-reduced-motion`, transitions 150-300ms → alignés avec [[responsive-mobile-sentys]] et [[animations-sentys]].
- **Dataviz** : types de graphiques recommandés → mais couleurs de séries = palette teal, cf. [[dataviz-sentys]].

## Ce qu'on IGNORE / REMPLACE
- **Couleurs primaires/secondaires/accent/background/border/ring** proposées → **remplacées par les tokens `sentys*`**.
- Gradients « AI purple/pink », néons → hors charte Sentys (sobre/pro).

## Comment lancer une recherche
```bash
# design system pour un type de page (puis on ne garde PAS ses couleurs)
python .agents/skills/ui-ux-pro-max/scripts/search.py "admin dashboard" --design-system --stack nextjs
# recherche ciblée (composant, style, ux, chart…)
python .agents/skills/ui-ux-pro-max/scripts/search.py "data table" --domain ux --stack shadcn
```
> Sur Windows, Python est à `C:\Users\madoumih\AppData\Local\Programs\Python\Python312\python.exe` (ou `python` après ouverture d'un nouveau terminal).

**Ordre de priorité en cas de conflit :** charte Sentys (couleurs) > recommandations ui-ux-pro-max (structure/UX). Le fichier d'instructions projet (CLAUDE.md, §5.4 charte, §5.3 sécurité) prime toujours.
