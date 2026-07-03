---
name: charte-graphique-sentys
description: À appliquer dès qu'on crée ou modifie de l'UI Sentys (couleurs, boutons, fonds, contraste, tokens). Impose la charte teal officielle (#03b5aa / #037971 / #023436 / #00bfb3) et le contraste WCAG AA.
---

# Charte graphique Sentys (teal)

Palette **officielle go-forward** à utiliser pour tout nouveau composant/écran.

| Rôle | Hex | Token Tailwind | Variable CSS |
|---|---|---|---|
| Primary (action) | `#03b5aa` | `bg-sentys` / `text-sentys` / `border-sentys` | `--color-sentys` |
| Primary hover / foncé | `#037971` | `bg-sentys-dark` | `--color-sentys-dark` |
| Fond sombre (canvas dark) | `#023436` | `bg-sentys-bg` | `--color-sentys-bg` |
| Accent | `#00bfb3` | `bg-sentys-accent` | `--color-sentys-accent` |
| Succès | `#10B981` | `text-success` / `bg-success` | `--ok` |
| Danger | `#EF4444` | `text-danger` / `bg-danger` | `--er` |
| Alerte | `#F59E0B` | `text-warning` | `--wa` |
| Bordure | `#E2E8F0` | `border-border` | `--border` |

> Les tokens `sentys*` sont définis dans `src/app/globals.css` (`@theme`). Tailwind est en **v4** : la source de vérité des couleurs est `@theme`, pas `tailwind.config.ts`.
> ⚠️ Le token `primary`/`--color-primary` hérité vaut encore `#3B82F6` (bleu) sur l'app existante. Pour le **go-forward, utiliser `bg-sentys`** (teal), pas `bg-primary`.

## Règles d'usage
- **Bouton primaire** : `bg-sentys text-white hover:bg-sentys-dark transition-colors duration-200 rounded-lg px-4 py-2 font-medium`. Jamais d'`alpha` sur le fond d'un bouton (`bg-sentys/10 text-white` = illisible).
- **Hover** = teinte plus foncée (`bg-sentys-dark`), jamais `opacity`.
- **Lien / ghost** : `text-sentys hover:text-sentys-dark` (ou `hover:bg-sentys/10`).
- **Focus visible obligatoire** : `focus-visible:ring-2 focus-visible:ring-sentys/50 focus-visible:ring-offset-2`.
- **Sur fond sombre** (`bg-sentys-bg` #023436) : texte `text-white` / `text-on-dark-muted` (#CBD5E1).

## Contraste WCAG AA (4.5:1)
- Texte **blanc sur `#03b5aa`** ≈ 3.0:1 → OK seulement pour **gros texte/boutons** (≥ 18px bold). Pour du **petit texte**, utiliser `bg-sentys-dark` (#037971, ≈ 4.6:1 sur blanc… l'inverse : blanc sur #037971 ≈ 4.6:1) ✅.
- **Teal comme texte sur fond blanc** : préférer `#037971` (sentys-dark) pour AA ; `#03b5aa` sur blanc ≈ 2.9:1 → réservé aux gros titres/décoratif.
- Ne jamais mettre `text-sentys` (petit) sur `bg-sentys-bg` (contraste insuffisant).

## Interdits
- Pas de hex en dur pour le teal dans les composants : utiliser `bg-sentys*` / `var(--color-sentys*)`.
- Pas d'ombre lourde (`shadow-2xl`) : rester `shadow-sm` ou l'ombre carte custom.
- Icônes : **Lucide React filaires uniquement**.

## Exemple
```tsx
<button className="inline-flex items-center gap-2 rounded-lg bg-sentys px-4 py-2 font-medium text-white transition-colors duration-200 hover:bg-sentys-dark focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sentys/50 focus-visible:ring-offset-2">
  Enregistrer
</button>
```
