# DASHBOARD_TEMOIN_LOG.md

Écran témoin : **Dashboard / Accueil user** refait au niveau premium (style SaaS pro, cartes noires, gros KPI animés, graphes recharts dégradé vert→cyan). **Un seul écran modifié** — les autres restent inchangés (on valide celui-ci avant d'étendre). Charte noir + vert cyber `#22C55E`.

Backup : `/home/ubuntu/backups/dashboard_before_temoin_20260703_184701`.

---

## 1. Vraies données utilisées (aucune donnée inventée)

| Élément | Source (endpoint réel) | Champ |
|---|---|---|
| KPI **Cyber Resilience Index** | `GET /api/risk-score/me` (`useRiskScore`) | `score` (0-100, ou « en attente » si `null`) + bande (Excellent/Bon/Moyen/À renforcer) |
| KPI **Parcours en cours** | `GET /api/assignments/mine` | nb d'assignations |
| KPI **Challenges complétés** | idem | nb `status === "completed"` |
| KPI **Progression moyenne** | idem | moyenne des `progressPercent` (%) |
| Graphe **Évolution du CRI** | `GET /api/risk-score/me/history?months=6` (`useRiskScoreHistory`) | `date`→mois, `score`→valeur (courbe) |
| Graphe **Répartition des parcours** | `GET /api/assignments/mine` | comptes par statut (Complété / En cours / Assigné) |
| Section **Mes parcours** | `GET /api/assignments/mine` | titre, niveau, statut, progression, échéance |
| Section **Activité récente** | `GET /api/submissions/recent` | challenge, correct/incorrect, points, date |

Empty states élégants partout où la donnée manque (ex. courbe si < 2 points d'historique, donut si 0 parcours) — **jamais de chiffre inventé**.

## 2. Composants premium créés (réutilisables pour la suite)
- **`KPICard`** — carte `--surface`, grand chiffre **count-up**, label, icône Lucide accent, hint/variation, hover-lift. (`components/premium/KPICard.tsx`)
- **`ChartCard`** — conteneur titre + sous-titre + zone graphe. (`components/premium/ChartCard.tsx`)
- **`AreaChartCard`** — aire **dégradé vert→cyan `#22C55E`→`#2DD4BF`**, grille fine `--border`, tooltip sombre stylisé, points animés. (nouveau)
- **`DonutChart`** — donut charte + libellé central. **`CountUp`**, **`Reveal`**, **`Stagger`/`StaggerItem`** (cascade), `Skeleton*`.
Tous en **tokens** (mode sombre défaut + clair), WCAG AA, responsive.

## 3. Rendu (build OK, live)
- Rangée de **4 KPICards** en haut (grille responsive `grid-cols-1 sm:2 xl:4`), gros chiffres animés count-up, icônes accent, apparition **en cascade** (stagger).
- Rangée **2 graphiques** : aire **Évolution du CRI** (dégradé vert→cyan, 6 mois, col-span 2) + **Répartition des parcours** (donut, col-span 1).
- **Mes parcours** : cartes premium (hover bordure accent, barre de progression dégradé vert→cyan) en cascade.
- **Activité récente** : liste premium (pastille ✔/✖ colorée, points en vert, hover) en cascade.
- Bandeaux Démo/Premiers-pas conservés. Skeletons de chargement.
- **Preuve build** : `✓ Compiled successfully`. Chunk dashboard contient bien `Cyber Resilience Index`, `Évolution du Cyber`, `Répartition des parcours`, `areaStroke/areaFill`, `#22C55E`, `#2DD4BF`. `sentys-frontend` active, `sentys.fr` 200, `/dashboard` 200/307 (redir login normale).

## 4. Périmètre confirmé
**Seul `app/dashboard/page.tsx`** a été réécrit (+ les composants réutilisables `premium/*`, `motion.ts`, `Stagger`). Aucune autre page, aucun endpoint/route/logique backend touché (`sentys-backend` intact). Vérifié : aucun fichier `.tsx` hors dashboard/premium/motion modifié.

## 5. À valider visuellement (AVANT d'étendre)
Ouvrir `https://sentys.fr/dashboard` (compte user) — web + mobile, modes sombre & clair : KPIs animés, courbe CRI dégradé vert→cyan, donut, cartes soignées, hover, cascade. Une fois validé, on étend le même style (KPICard/ChartCard/DataTable/DonutChart/AreaChartCard) aux autres écrans (analytics, classements, admin).

---

# PASSE VIOLET — Dashboard témoin style Vision UI (bleu nuit / violet)

> ⚠️ **Conflit de charte à acter** : ce prompt introduit un thème **bleu nuit / violet (Vision UI)** qui **contredit** la charte actuelle de `CLAUDE.md` (noir/gris/blanc + vert cyber #22C55E). Sur demande explicite de l'utilisateur, ce thème est appliqué **UNIQUEMENT au dashboard témoin, en local, pour validation** avant toute décision de réplication. `CLAUDE.md` et le thème global (vert) ne sont **pas modifiés**. Le violet est **scopé** au seul sous-arbre `.vision-dashboard`.

## Périmètre
- **Un seul écran** refondu : `app/dashboard/page.tsx` (dashboard user). Aucun autre écran, aucun composant partagé vert, aucun backend/auth/IA/scoring touché.
- Backup : `backups/dashboard-temoin-20260705/frontend.tgz`.

## Comment le violet est isolé (zéro hex en dur dans le JSX)
- Tokens Vision UI définis **scopés** dans `globals.css` sous `.vision-dashboard { --v-bg … --v-accent … }` — **seul endroit** où vivent les hex de ce thème (définition de tokens, comme `:root`). Le JSX n'utilise que `var(--v-*)`.
  - `--v-bg:#0B1437` · `--v-surface:#111C44` · `--v-surface-2:#1A2456` · `--v-border:#2A3568` · `--v-text:#FFFFFF` · `--v-text-2:#A0AEC0` · `--v-accent:#7551FF` · `--v-accent-2:#582CFF` · `--v-cyan:#2CD9FF` · `--v-success:#01B574` · `--v-danger:#E53E3E` · `--v-grad` (violet).
- Focus ring violet scopé (`.vision-dashboard *:focus-visible`). Hover doux scopé (`.v-hover`).
- Grep hex sur `page.tsx` + `components/vision/*` = **0**.

## Composants créés (dédiés, non partagés)
`components/vision/` : **VisionCard** (verre dépoli : surface translucide + `backdrop-blur` + bordure + ombre), **VisionKpiCard** (pastille dégradé violet, gros chiffre count-up, variation +/-% réelle), **VisionAreaChart** (recharts, aire dégradé violet→cyan via `stopColor` var, tooltip verre, `isAnimationActive` coupé si reduced-motion), **VisionBarChart** (barres dégradé violet, coins arrondis), **VisionGauge** (jauge SVG circulaire, arc dégradé violet→cyan, count-up central).

## Rendu (avant → après)
- **Avant** : dashboard vert (KPICard/AreaChartCard/DonutChart green tokens).
- **Après** : fond bleu nuit `--v-bg`, cartes verre dépoli, rangée de **4 KPI** count-up (variation réelle sur le CRI = dernier mois − précédent), **aire principale** CRI dégradé violet→cyan (span 2) + **jauge de score** circulaire, **barres** répartition des parcours + carte **activité récente**, section **Mes parcours** (barres de progression dégradé violet, bouton dégradé). Animations : `Reveal` (fade+translate entrée), `Stagger` (cascade cartes), `CountUp` (KPI + jauge), hover doux `.v-hover`, apparition des graphes.

## Données — 100% réelles
`/api/auth/me`, `/api/assignments/mine`, `/api/submissions/recent`, `useRiskScore` (CRI), `useRiskScoreHistory(6)`. Variation CRI calculée depuis l'historique réel. Empty states là où la donnée manque (courbe < 2 points, 0 parcours) — **aucune fausse donnée**. Les KPI sans delta réel (hors CRI) n'affichent **pas** de variation inventée (juste un hint).

## Vérifications
- [x] `npm run build` (local) → **✓ Compiled successfully** ; route `/dashboard` générée ; sert HTTP 307 (redir login normale sans session), aucune erreur runtime au log.
- [x] **Zéro hex en dur** dans le JSX du dashboard (tokens `--v-*` uniquement ; hex seulement dans la définition scopée `globals.css`).
- [x] Contraste AA (blanc/`#A0AEC0` sur `#0B1437`/`#111C44`) ; focus visible (ring violet) ; `prefers-reduced-motion` respecté (Reveal/Stagger/CountUp + `isAnimationActive` charts coupés).
- [x] Aucun autre écran / composant partagé / backend touché.

## Bugs fonctionnels repérés ailleurs
(aucun rencontré durant cette passe)

## DASHBOARD TEMOIN PRET — en attente de validation utilisateur avant replication
> Ne PAS répliquer sur d'autres écrans tant que l'utilisateur n'a pas validé ce dashboard témoin (compte user, `http://localhost:3000/dashboard`). Décision à prendre aussi sur le **conflit de charte** (adopter le violet globalement = mise à jour de `CLAUDE.md`, ou garder le vert et abandonner cette piste).
