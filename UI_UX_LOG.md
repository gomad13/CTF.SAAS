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
| 1  | `/` (root) | Landing/redirection | [ ] |
| 2  | `/[id]` | Page dynamique racine | [ ] |
| 3  | `/account/consents` | Consentements RGPD | [ ] |
| 4  | `/admin` | Accueil admin | [ ] |
| 5  | `/admin/analytics` | Analytics admin | [ ] |
| 6  | `/admin/campaigns` | Liste campagnes | [ ] |
| 7  | `/admin/campaigns/[id]` | Détail campagne | [ ] |
| 8  | `/admin/catalog` | Catalogue contenu | [ ] |
| 9  | `/admin/compliance` | Conformité | [ ] |
| 10 | `/admin/dashboard` | Dashboard admin | [ ] |
| 11 | `/admin/directory` | Annuaire | [ ] |
| 12 | `/admin/entreprise` | Paramètres entreprise | [ ] |
| 13 | `/admin/scenarios` | Scénarios | [ ] |
| 14 | `/admin/scenarios/instances/[id]` | Instance scénario | [ ] |
| 15 | `/admin/scenarios/launch/[templateId]` | Lancement scénario | [ ] |
| 16 | `/admin/teams` | Équipes admin | [ ] |
| 17 | `/admin/teams/[id]` | Détail équipe | [ ] |
| 18 | `/admin/users` | Utilisateurs admin | [ ] |
| 19 | `/campaigns/me` | Mes campagnes | [ ] |
| 20 | `/cgu` | CGU | [ ] |
| 21 | `/coaching/history` | Historique coaching | [ ] |
| 22 | **`/dashboard`** | **Dashboard user (ÉCRAN TÉMOIN)** | **[~]** |
| 23 | `/dashboard/admin` | Dashboard admin (variante) | [ ] |
| 24 | `/dashboard/challenge/[id]` | Détail challenge | [ ] |
| 25 | `/dashboard/competition` | Compétition | [ ] |
| 26 | `/dashboard/equipes` | Équipes user | [ ] |
| 27 | `/dashboard/parametres` | Paramètres user | [ ] |
| 28 | `/dashboard/parcours` | Liste parcours | [ ] |
| 29 | `/dashboard/parcours/[id]` | Détail parcours | [ ] |
| 30 | `/demo` | Démo | [ ] |
| 31 | `/feedback` | Feedback | [ ] |
| 32 | `/forgot-password` | Mot de passe oublié | [ ] |
| 33 | `/inbox` | Boîte de réception | [ ] |
| 34 | `/inbox/[emailId]` | Détail email | [ ] |
| 35 | `/join` | Rejoindre (invitation) | [ ] |
| 36 | `/landing` | Landing marketing | [ ] |
| 37 | `/legal/[slug]` | Pages légales dynamiques | [ ] |
| 38 | `/login` | Connexion | [ ] |
| 39 | `/mentions-legales` | Mentions légales | [ ] |
| 40 | `/paths/[pathId]/module/[modulesId]` | Module d'un parcours | [ ] |
| 41 | `/privacy` | Confidentialité | [ ] |
| 42 | `/register` | Inscription | [ ] |
| 43 | `/reset-password` | Réinitialisation mot de passe | [ ] |
| 44 | `/scenarios/landing/[token]` | Landing scénario (public) | [ ] |
| 45 | `/superadmin` | Console super-admin | [ ] |
| 46 | `/mentions` / pages statiques restantes | (regroupe légal résiduel) | [ ] |

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

## ECRAN TEMOIN PRET — en attente de validation utilisateur

> **PORTE DE VALIDATION DURE.** L'Étape 2 (réplication page par page sur les 45 autres écrans) NE démarre PAS tant que l'utilisateur n'a pas validé le dashboard témoin (`https://sentys.fr/dashboard`, comptes user, web + mobile, modes sombre & clair).
