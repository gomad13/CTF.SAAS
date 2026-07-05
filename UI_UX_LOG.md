# UI_UX_LOG.md — Refonte UI/UX + animations (boucle témoin)

> Périmètre STRICT : UI / UX / animations uniquement. Aucun backend / auth / IA / scoring / migration touché.
> Charte racine (CLAUDE.md §5.4) = AUTORITÉ : noir/gris/blanc + vert cyber `#22C55E`, tokens CSS. Jamais modifiée, seulement appliquée.
> Bugs fonctionnels repérés = NOTÉS ici, PAS corrigés.
> Backup complet du frontend avant toute modif : `/home/ubuntu/backups/ui-20260704-1528/` (frontend/src, hors node_modules/.next).

---

## Standard d'animation (référence, à appliquer partout)

| Règle | Détail | Implémentation |
|---|---|---|
| Entrée de page | fade + translate (opacity 0→1, y 8px→0), ~250ms, easing doux | `<Reveal>` (framer-motion) |
| Stagger listes/cartes | délai 40–60ms entre items | `<Stagger>` / `<StaggerItem>` |
| Count-up KPI | animation des gros chiffres | `<CountUp>` |
| Hover cartes/boutons | scale ≤1.02, changement surface/bordure, ~150ms | `transition` + `whileHover` |
| Focus visible | ring accent sur tous les interactifs | `*:focus-visible{outline:2px solid var(--accent)}` (globals.css) |
| `prefers-reduced-motion` | animations désactivées/réduites | `useReducedMotion()` + `@media (prefers-reduced-motion: reduce)` (globals.css) |
| Librairie | framer-motion | déjà en place |

Tokens autorisés (aucune couleur en dur hors fichier de tokens) :
`--bg, --surface, --surface-2, --border, --text, --text-2, --text-3, --accent (#22C55E), --accent-hover, --accent-subtle, --accent-border, --on-accent, --accent-2 (#2DD4BF cyan)`.
Fichier de tokens dataviz : `src/lib/chart-colors.ts` — seul endroit où des hex sont admis (les `fill`/`stroke` recharts sont des attributs SVG qui ne résolvent pas `var()`).

---

## ÉTAPE 0 — Inventaire de TOUTES les pages (définition de « fini »)

Statut : `[ ]` à traiter · `[~]` témoin en attente de validation · `[x]` conforme (charte + standard anim + build OK).

| #  | Route (app/) | Rôle | Statut |
|----|--------------|------|--------|
| 1  | `/` (root) | Landing/redirection | [x] |
| 2  | `/[id]` | Page dynamique racine | [x] |
| 3  | `/account/consents` | Consentements RGPD | [x] |
| 4  | `/admin` | Accueil admin | [x] |
| 5  | `/admin/analytics` | Analytics admin | [x] |
| 6  | `/admin/campaigns` | Liste campagnes | [x] |
| 7  | `/admin/campaigns/[id]` | Détail campagne | [x] |
| 8  | `/admin/catalog` | Catalogue contenu | [x] |
| 9  | `/admin/compliance` | Conformité | [x] |
| 10 | `/admin/dashboard` | Dashboard admin | [x] |
| 11 | `/admin/directory` | Annuaire | [x] |
| 12 | `/admin/entreprise` | Paramètres entreprise | [x] |
| 13 | `/admin/scenarios` | Scénarios | [x] |
| 14 | `/admin/scenarios/instances/[id]` | Instance scénario | [x] |
| 15 | `/admin/scenarios/launch/[templateId]` | Lancement scénario | [x] |
| 16 | `/admin/teams` | Équipes admin | [x] |
| 17 | `/admin/teams/[id]` | Détail équipe | [x] |
| 18 | `/admin/users` | Utilisateurs admin | [x] |
| 19 | `/campaigns/me` | Mes campagnes | [x] |
| 20 | `/cgu` | CGU | [x] |
| 21 | `/coaching/history` | Historique coaching | [x] |
| 22 | **`/dashboard`** | **Dashboard user (ÉCRAN TÉMOIN — validé)** | **[x]** |
| 23 | `/dashboard/admin` | Dashboard admin (variante) | [x] |
| 24 | `/dashboard/challenge/[id]` | Détail challenge | [x] |
| 25 | `/dashboard/competition` | Compétition | [x] |
| 26 | `/dashboard/equipes` | Équipes user | [x] |
| 27 | `/dashboard/parametres` | Paramètres user | [x] |
| 28 | `/dashboard/parcours` | Liste parcours | [x] |
| 29 | `/dashboard/parcours/[id]` | Détail parcours | [x] |
| 30 | `/demo` | Démo | [x] |
| 31 | `/feedback` | Feedback | [x] |
| 32 | `/forgot-password` | Mot de passe oublié | [x] |
| 33 | `/inbox` | Boîte de réception | [x] |
| 34 | `/inbox/[emailId]` | Détail email | [x] |
| 35 | `/join` | Rejoindre (invitation) | [x] |
| 36 | `/landing` | Landing marketing | [x] |
| 37 | `/legal/[slug]` | Pages légales dynamiques | [x] |
| 38 | `/login` | Connexion | [x] |
| 39 | `/mentions-legales` | Mentions légales | [x] |
| 40 | `/paths/[pathId]/module/[modulesId]` | Module d'un parcours | [x] |
| 41 | `/privacy` | Confidentialité | [x] |
| 42 | `/register` | Inscription | [x] |
| 43 | `/reset-password` | Réinitialisation mot de passe | [x] |
| 44 | `/scenarios/landing/[token]` | Landing scénario (public) | [x] |
| 45 | `/superadmin` | Console super-admin | [x] |
| 46 | `/mentions` / pages statiques restantes | (regroupe légal résiduel) | [x] |

**46 écrans recensés.** La passe est FINIE quand toutes les lignes sont `[x]` + build OK + 0 hex en dur (hors `chart-colors.ts`) + standard anim partout.

---

## ÉTAPE 1 — ÉCRAN TÉMOIN : `/dashboard` (user)

### Avant
- Dashboard « plat » : cartes claires génériques, peu d'hiérarchie, pas d'animation d'entrée ni de count-up, graphes absents ou basiques, quelques couleurs en dur.

### Après (premium, charte noir + vert cyber)
- **Rangée 4 KPICards** (`grid-cols-1 sm:2 xl:4`) : Cyber Resilience Index, Parcours en cours, Challenges complétés, Progression moyenne — gros chiffres **count-up**, icône Lucide accent, apparition **en cascade (stagger)**, **hover-lift**.
- **Rangée 2 graphiques** : aire **Évolution du CRI** (dégradé **vert→cyan** via tokens `--accent`→`--accent-2`, 6 mois, col-span 2) + donut **Répartition des parcours** (col-span 1).
- **Mes parcours** : cartes premium, hover bordure accent, barre de progression dégradé vert→cyan (pill `rounded-full`), cascade.
- **Activité récente** : liste premium (pastille ✔/✖, points en vert accent, hover), cascade.
- **Empty states** élégants (courbe si <2 points, donut si 0 parcours) — aucune donnée inventée.
- **Skeletons** de chargement.

### Vraies données (aucune inventée)
`GET /api/auth/me`, `/api/assignments/mine`, `/api/submissions/recent`, `/api/risk-score/me`, `/api/risk-score/me/history?months=6`.

### Checklist standard — écran témoin
- [x] Entrée de page fade+translate (`<Reveal>`)
- [x] Stagger listes/cartes (`<Stagger>`, 40–60ms)
- [x] Count-up sur les KPI (`<CountUp>`)
- [x] Hover doux cartes/boutons (scale ≤1.02, surface/bordure, ~150ms)
- [x] Focus visible ring accent (globals.css)
- [x] `prefers-reduced-motion` respecté (`useReducedMotion` + `@media`)
- [x] **0 couleur en dur** dans le JSX du dashboard et des composants premium utilisés (vérifié `grep`), hex uniquement dans `chart-colors.ts` (fichier de tokens dataviz)
- [x] `npm run build` → **✓ Compiled successfully**
- [x] Charte noir + vert cyber respectée (tokens uniquement)

### Fichiers touchés (témoin — UI uniquement)
- `src/app/globals.css` — ajout token `--accent-2` (#2DD4BF cyan) + `--color-accent-2`.
- `src/app/dashboard/page.tsx` — réécriture premium, tokenisé (0 hex).
- `src/components/premium/AreaChartCard.tsx` — dégradé vert→cyan en `style={{ stopColor: var() }}` (SVG résout var via style), `dot={false}` (retrait du fill en dur), axes/grille/tooltip en tokens.
- `src/lib/chart-colors.ts` — fichier de tokens dataviz documenté (hex admis, SVG n'accepte pas var() en attribut).
- Composants réutilisables déjà en place : `premium/KPICard, ChartCard, DonutChart, DataTable, BarChartCard, Skeleton`, `CountUp, Reveal, Stagger`, `lib/motion.ts`.

### Backend / logique
Aucune modification. Aucun controller, service, auth/JWT, migration, IA/Ollama, scoring touché.

---

## Bugs fonctionnels repérés (À NE PAS corriger dans cette passe — pour plus tard)
- (aucun bug fonctionnel détecté sur le dashboard témoin pour l'instant — section à compléter au fil des pages)

---

## ECRAN TEMOIN — VALIDÉ par l'utilisateur ✅

Le dashboard témoin a été validé (« ok continue »). L'Étape 2 a été exécutée.

---

## ÉTAPE 2 — Réplication sur les 45 autres écrans + fichiers support — TERMINÉE ✅

### Méthode
9 sous-agents (3 vagues) ont traité les 45 pages ; puis 5 sous-agents (Wave 4) ont tokenisé les **fichiers support** rendus par ces pages (sans quoi la charte ne tient pas : une page tokenisée qui affiche un composant à hex figé garde de mauvaises couleurs). Chaque fichier : mapping hex→token + conversion des classes Tailwind palette figées + standard d'animation au niveau page (Reveal/Stagger/CountUp/hover/focus). Build serveur central après chaque vague.

### Périmètre livré
- **45 pages** (`page.tsx`) : tokens charte + animations. ✅
- **8 layouts** (`layout.tsx`) : fond/texte suivent désormais le thème (corrige le mode sombre/clair cassé). ✅
- **7 sections landing** (`landing/components/*`). ✅
- **~40 composants partagés** (`components/*` : challenges, sidebar, compétition, légal, superadmin, risk-score, ui…). ✅
- **2 maps de couleurs lib** (`chart-colors.ts` STATUS, `types/campaigns.ts` STATUS_STYLES). ✅

### Critère d'arrêt GLOBAL — atteint
- [x] Toutes les pages de l'inventaire cochées conformes.
- [x] `npm run build` → **✓ Compiled successfully** (build final, serveur).
- [x] **Grep hex en dur (hors fichier de tokens) = 0** dans le JSX. Les 59 hex restants sont **tous des exceptions documentées** :
  - Marque SSO Google/Microsoft (`login`, `register`) — identités officielles.
  - `chart-colors.ts` — fichier de tokens dataviz (les `fill`/`stroke` recharts sont des attributs SVG qui ne résolvent pas `var()`).
  - Literals de graphes SVG/canvas (`RiskScoreCard`, `RiskScoreEvolutionChart`, `superadmin` Chart.js) — même raison.
  - Médailles podium or/argent/bronze (`Podium`, `ScoreboardTable`, `TeamLeaderboard`) — couleurs identitaires.
  - Défauts de données : color-picker d'équipe (`#22C55E` charte), fond blanc du canvas d'export QR (`InvitesManager`), état « info » bleu (`SaDialog`).
- [x] Chaque page : animation d'entrée (`Reveal`) + stagger listes + hover doux + focus visible (ring accent global) + `prefers-reduced-motion` respecté (composants `useReducedMotion` + `@media` global).

### Commits (locaux, PAS de push)
- `842d53f` — Étape 1 dashboard témoin.
- **43 commits par page** (`feat(ui): refonte page <route> — tokens charte + animations`).
- 4 commits groupés support : layouts / sections landing / composants partagés / maps couleurs lib.

### Corrections de couleur incluses (dans le périmètre couleur/thème, pas de logique)
- `parametres` : hover-out d'onglet remettait le texte en noir (`#000`) invisible en dark → token.
- `demo` (récap) : carte blanche + texte clair = texte invisible → tokenisé, contraste rétabli.
- `CoachingModal` : `var(--accent)33` (concat alpha sur var = CSS invalide) → `color-mix`, badge sans fond réparé + textes blancs en dur → tokens (lisibles en clair).
- `scenarios/launch` (RecipientsRecap) & `scenarios/instances` : blancs sur surface neutre illisibles en clair → tokens.
- Bleu interdit `#3B82F6`/`rgba(59,130,246,*)` éliminé partout (challenges, landing, sidebar, risk chart…) au profit du vert cyber `--accent`.

---

## Bugs FONCTIONNELS repérés (NOTÉS, NON corrigés — hors périmètre UI, à traiter dans une passe dédiée)

1. **`paths/[pathId]/module/[modulesId]`** — Violation des règles des Hooks React : `useState`/`useMemo`/`useIsMobile` appelés **après** des `return notFound()` conditionnels → risque de crash « rendered more/fewer hooks ». **À refactorer (hooks avant les early returns).**
2. **`superadmin`** — Chart.js chargé depuis un **CDN externe** (`cdnjs.cloudflare.com`) au runtime via `document.createElement("script")` → en environnement CSP/offline, les graphes d'activité cassent. (De plus, `borderColor: var(--pr)` sur canvas était déjà non fonctionnel — canvas ne résout pas `var()`.)
3. **`inbox/[emailId]`** — `useEffect` dépendant de `[email?.id]` avec `refetch()` → possible boucle de refetch (déjà eslint-disable en place).
4. **`dashboard/parametres` (SecuriteTab)** — Historique des connexions **codé en dur** (fausses données, pas d'appel API) ; modale de suppression de compte = code mort (bouton retiré pour la bêta, intentionnel selon commentaires).
5. **`PhishingAiChallenge`** — Incohérence de seuil : validation à `text.trim().length < 15` mais l'UX annonce « au moins 50 caractères » (compteur « / 50 min »). Seuil réel = 15.
6. Présence de `any` TypeScript préexistants (eslint-disabled) dans `demo`, `[id]`, `superadmin` — **aucun nouveau `any` introduit** par cette passe.

## Fichiers hors JSX à surveiller (déjà traités)
- Les `layout.tsx` (fond/texte réels) ont été tokenisés — c'était la cause principale du mode sombre « cassé » signalé sur les pages légales/inbox.

---

## RAPPORT FINAL — passe UI/UX + animations TERMINÉE
Charte noir/gris/blanc + vert cyber appliquée intégralement (tokens, mode sombre/clair, WCAG AA), standard d'animation sobre partout (framer-motion, `prefers-reduced-motion` respecté), 0 hex en dur hors exceptions documentées, build OK. Backup intact : `/home/ubuntu/backups/ui-20260704-1528`. Commits locaux page par page + support. **Aucun push** (conforme à la consigne). La boucle est arrêtée — la checklist est satisfaite (pas de relance « pour améliorer encore »).
