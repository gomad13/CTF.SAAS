---
name: charte-graphique-sentys
description: À appliquer dès qu'on crée ou modifie de l'UI Sentys (couleurs, boutons, fonds, contraste, tokens). Impose la charte officielle noir/gris/blanc + vert cyber, mode sombre (défaut) ET clair, contraste WCAG AA dans les 2 modes.
---

# Charte graphique Sentys — noir/gris/blanc + vert cyber

Charte **officielle** (remplace l'ancienne teal `#03b5aa`, définitivement abandonnée). Système de **tokens CSS** dans `src/app/globals.css` : mode clair = `:root`, mode sombre = `html.dark` (défaut). Toggle persistant (`useTheme` / `ThemeToggle`, `ctf_theme`). **Contraste WCAG AA garanti dans les 2 modes.**

## Palette — Mode SOMBRE (défaut)
| Rôle | Variable | Hex |
|---|---|---|
| Fond principal | `--bg` | `#0A0A0B` |
| Surface / carte | `--surface` | `#161618` |
| Surface élevée | `--surface-2` | `#1F1F22` |
| Bordures | `--border` | `#242427` |
| Texte principal | `--text` | `#F5F5F7` |
| Texte secondaire | `--text-2` | `#A1A1A6` |
| Texte tertiaire | `--text-3` | `#6B6B70` |
| **Accent (vert cyber)** | `--accent` | `#22C55E` |
| Accent hover | `--accent-hover` | `#16A34A` |

## Palette — Mode CLAIR
| Rôle | Variable | Hex |
|---|---|---|
| Fond principal | `--bg` | `#FFFFFF` |
| Surface / carte | `--surface` | `#F5F5F7` |
| Surface élevée | `--surface-2` | `#EBEBED` |
| Bordures | `--border` | `#E4E4E7` |
| Texte principal | `--text` | `#0A0A0B` |
| Texte secondaire | `--text-2` | `#6B6B70` |
| **Accent (vert cyber)** | `--accent` | `#16A34A` |
| Accent hover | `--accent-hover` | `#15803D` |

États (communs) : succès `#22C55E`, danger `#EF4444`, alerte `#F59E0B`, info `#3B82F6`.

## Tokens Tailwind mappés (à utiliser, jamais de hex en dur)
`bg-canvas`/`bg-background` (=`--bg`) · `bg-surface` · `bg-surface-2` · `border-border` · `text-fg-heading` (=`--text`) · `text-fg-body` (=`--text-2`) · `text-fg-muted` (=`--text-3`) · `bg-primary`/`text-primary` (=`--accent`, vert) · `bg-success`/`bg-danger`/`bg-warning`.
En style inline : `var(--bg)`, `var(--surface)`, `var(--text)`, `var(--text-2)`, `var(--accent)`, `var(--accent-subtle)`, `var(--border)`.

## Règles
- **Aucun hex en dur** dans les composants → sinon l'élément ne suit pas le mode sombre/clair et casse le contraste. Toujours un token.
- Le **vert cyber = identité** : boutons primaires, liens actifs, focus, badges, highlights. Base neutre (noir/gris/blanc) pour tout le reste → sobre, pro.
- **Bouton primaire** : `bg-primary text-white hover:bg-primary-hover` (ou `bg-[var(--accent)]` → `var(--accent-hover)`). Jamais d'alpha sur un fond de bouton.
- **Hover** = teinte plus foncée (`--accent-hover`), jamais `opacity`.
- **Focus visible obligatoire** : `focus-visible:ring-2` couleur accent (déjà en base : `*:focus-visible{outline:2px solid var(--accent)}`).
- **Interdits** : réintroduire le teal `#03b5aa`/`#037971`/`#023436`/`#00bfb3`, le bleu `#3B82F6` (hors état info), le navy `#0F172A`. Ombres lourdes (`shadow-2xl`). Icônes non-Lucide.

## Contraste WCAG AA (les 2 modes)
- Les tokens texte (`--text`/`--text-2`/`--text-3`) sont calibrés pour AA sur `--bg`/`--surface` dans chaque mode.
- Texte sur fond accent : blanc sur `#22C55E`/`#16A34A` OK pour boutons ; pour du petit texte coloré préférer les tokens `-t` (`--success-t`, `--danger-t`, `--pr-t`).

## Exemple
```tsx
<button className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 font-medium text-white transition-colors duration-200 hover:bg-primary-hover">
  Enregistrer
</button>
<div className="rounded-xl border border-border bg-surface p-6 text-fg-heading">…</div>
```
