# DESIGN_SKILLS_LOG.md

Librairies UI/design + Skills Claude Code design, à la charte Sentys.
Frontend `/home/ubuntu/sentys/frontend/` (Next.js 16.1.6 + React 19.2.3 + **Tailwind v4**), repo `ctf-web/`. Skills dans `.claude/skills/`.

---

## [2026-07-03] — Backups (avant modif)
- Config frontend : `/home/ubuntu/backups/frontend_design_20260703_155027/` (package.json, package-lock.json, tailwind.config.ts, postcss.config.mjs, globals.css, lib/utils.ts).

## Constat initial (important)
- **Déjà installés** : `framer-motion@12.34`, `recharts@3.7`, `clsx@2.1`, `tailwind-merge@3.4`, `lucide-react@0.563` ; `src/lib/utils.ts` a déjà `cn`. **Tailwind v4** (`@import "tailwindcss"` + `@theme`, pas de `@config` → `tailwind.config.ts` est documentaire).
- **Charte réelle** : le design system global est **bleu `#3B82F6`** (globals.css `@theme` + tailwind.config). Le teal `#03b5aa` de la charte n'était utilisé qu'en **ad-hoc** dans quelques pages (scenarios, legal, coaching). L'affirmation du prompt « teal déjà dans tailwind.config » était inexacte.
- **Décision** : exposer la charte teal **additivement** (tokens `sentys*`, variables shadcn teal, skills) comme standard go-forward, **sans repeindre** tout l'app bleu existant (un re-theme global est hors périmètre « librairies + skills » et serait un changement visuel prod non demandé). Documenté dans le skill `charte-graphique-sentys`.

## PARTIE 1 — Librairies (installées + configurées teal)

| Librairie | Version | Rôle | État |
|---|---|---|---|
| framer-motion | ^12.34.0 | animations | déjà présent |
| recharts | ^3.7.0 | dataviz dashboards | déjà présent |
| lucide-react | ^0.563.0 | icônes filaires | déjà présent |
| clsx | ^2.1.1 | classes conditionnelles | déjà présent |
| tailwind-merge | ^3.4.0 | fusion classes | déjà présent |
| **class-variance-authority** | ^0.7.1 | variants de composants (cva) | **ajouté** |
| **tw-animate-css** | ^1.4.0 | animations CSS shadcn (v4) | **ajouté** |
| **@radix-ui/react-slot** | ^1.3.0 | `asChild` des composants shadcn | **ajouté** |

> `tailwindcss-animate` (cité dans le prompt) est la variante **Tailwind v3**. En **v4**, shadcn utilise `tw-animate-css` (importé en CSS) → c'est ce que j'ai installé. `sonner` non installé (le projet a déjà un système de toasts — éviter la redondance).

### shadcn/ui — infrastructure + configuration charte
- `components.json` créé (style `new-york`, `cssVariables: true`, `iconLibrary: lucide`, alias `@/*`) → `npx shadcn@latest add <composant>` est désormais utilisable.
- `src/lib/utils.ts` : `cn` (clsx + tailwind-merge) déjà présent.
- **Composant de démo** `src/components/ui/button.tsx` : pattern shadcn (cva + cn + Radix Slot), **à la charte teal** — variantes `default (bg-sentys)`, `accent`, `outline`, `ghost`, `secondary`, `destructive`, `link` ; tailles avec **cibles tactiles ≥ 44px** ; focus visible teal.
- **Tokens charte teal** ajoutés dans `globals.css` `@theme` → utilitaires générés : `bg-sentys #03b5aa`, `bg-sentys-dark #037971`, `bg-sentys-bg #023436`, `bg-sentys-accent #00bfb3`, `text-sentys`, `border-sentys` (+ variantes `/opacity`). `@import "tw-animate-css"` ajouté.
- `tailwind.config.ts` (documentaire en v4) complété : tokens `sentys*` + `primary/primary-dark/background-dark/accent` = teal.

**Preuve build (utilitaires teal générés)** — extrait du CSS compilé :
```
.bg-sentys{background-color:#03b5aa}
.bg-sentys-dark{background-color:#037971}
.bg-sentys-accent{background-color:#00bfb3}
.text-sentys{color:#03b5aa}  .border-sentys{border-color:#03b5aa}
```

## PARTIE 2 — Skills design (`.claude/skills/`, lus automatiquement par Claude Code)

| Skill | Emplacement | Couvre |
|---|---|---|
| `charte-graphique-sentys` | `.claude/skills/charte-graphique-sentys/SKILL.md` | palette teal officielle, tokens `sentys*`, règles boutons/hover/focus, **contraste WCAG AA** |
| `composants-ui-sentys` | `…/composants-ui-sentys/SKILL.md` | conventions shadcn+cva+cn, patterns Card/Modal/Tabs/Badge/Table/Form, a11y |
| `responsive-mobile-sentys` | `…/responsive-mobile-sentys/SKILL.md` | mobile-first 320px, cibles 44px, grilles, burger, anti-débordement |
| `dataviz-sentys` | `…/dataviz-sentys/SKILL.md` | recharts à la charte, palette séries teal, responsive |
| `animations-sentys` | `…/animations-sentys/SKILL.md` | framer-motion sobre/pro, `prefers-reduced-motion`, presets |

Chaque `SKILL.md` a un frontmatter (`name` + `description` = QUAND l'appliquer), des règles concises et des **exemples de code** conformes à la charte.

## PARTIE 3 — Build + audit
- **`npm run build`** : `✓ Compiled successfully` (avant et après changements — 0 erreur).
- **`npm audit`** : **10 vulnérabilités (0 critique, 4 high, 5 moderate, 1 low)** — **toutes préexistantes** (transitives : `next`, `@babel/core`, `postcss`, `minimatch`, `flatted`, `picomatch`, `js-yaml`, `ajv`, `dompurify`, `brace-expansion`). **Aucune** introduite par cva / tw-animate-css / react-slot. `audit fix --force` **non exécuté** (il force-upgrade Next.js → risque de casse prod). Aucune vulnérabilité **critique** → conforme.
- Service `sentys-frontend` redémarré → `active`, `https://sentys.fr/login` → 200.

## Comment les utiliser ensuite
- **Skills** : Claude Code lit automatiquement `.claude/skills/*/SKILL.md` à chaque session dans ce repo ; le bon skill se déclenche via sa `description` (ex. dès qu'on touche aux couleurs → `charte-graphique-sentys`). On peut aussi l'invoquer explicitement.
- **Composants** : `import { Button } from "@/components/ui/button"` ; nouveaux composants shadcn via `npx shadcn@latest add …` puis remapper `primary`→`sentys` (voir skill composants).
- **Charte** : utiliser `bg-sentys` / `text-sentys` / `bg-sentys-dark` pour le teal go-forward.
- **Déploiement** : changements appliqués sur le serveur (build + restart faits) et synchronisés dans le repo `ctf-web/`.

## Livrables
1. ✅ Librairies installées + configurées teal (cva, tw-animate-css, radix-slot ajoutés ; framer/recharts/lucide/clsx/merge déjà là ; shadcn infra + Button démo).
2. ✅ 5 skills design en `.claude/skills/`.
3. ✅ Build OK, 0 vulnérabilité critique.
4. ✅ Backups + ce log.
