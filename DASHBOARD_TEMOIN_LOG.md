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
