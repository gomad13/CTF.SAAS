# FIX_PODIUM_LOG.md — Podium ÉQUIPE en vrai podium (réutilise le podium individuel)

> Refaire le classement équipe (liste) en **vrai podium visuel**, identique en DA + animations au podium individuel (référence validée). 100 % LOCAL, pas de push/déploiement.
> Autorité : `Instructions.md` absent → **CLAUDE.md** (charte §2, DRY §1.3).

## 0. Backup
- `backups/podium-equipe-20260715_004307/` : `page.tsx` + `components-competition/`.

## 1. Comment était construit le podium individuel (référence à réutiliser)
Dans `dashboard/competition/page.tsx` :
- `IndividualTop5` : **estrade 3 marches** (grid 3 colonnes, `alignItems:end`) — ordre **2e / 1er / 3e**, le **1er au centre surélevé** (`height` 160 vs 128/108) et **animé EN DERNIER** (`Reveal delay` 0.05 / 0.15 / **0.32**). En dessous, **4e/5e** en lignes via `Stagger`.
- `PodiumSpot` : carte `v-hover` sur `--v-surface-2`, avatar rond (initiales sur `--v-grad`) avec **anneau couleur médaille** (`box-shadow`), **couronne** sur le 1er, **score en `CountUp`**, sous-titre, puis la **marche** (dégradé + bordure couleur médaille + n°).
- `RankRow` : ligne `v-row` (rang + avatar + nom + `CountUp` score).
- Médailles or/argent/bronze = 3 hex constants (exception charte autorisée).

## 2. Factorisation (DRY §1.3) — composant PARTAGÉ
Nouveau `components/competition/PodiumBoard.tsx` — **exactement la structure/classes/animations du podium individuel**, généralisé :
- `PodiumItem = { key, rank, name, score, subtitle?, isCurrent, currentLabel?, avatarBg, avatarContent }` — **seul l'avatar diffère** (initiales+`--v-grad` pour l'individuel ; **pastille couleur d'équipe `t.color` + icône** pour l'équipe).
- `PodiumBoard({ title, items, footer })` : estrade top 3 (2e/1er/3e, **1er en dernier**) + reste en lignes `Stagger` + footer optionnel. `PodiumSpot`/`PodiumRow`/`Avatar` internes.
- `medalColor` + médailles déplacés ici (source unique).

## 3. Application
- `IndividualTop5` → mappe les entrées en `PodiumItem` (avatar = initiales / `--v-grad`, sous-titre = « N challenges ») → `<PodiumBoard title="Podium — top 5" .../>`.
- `TeamRanking` → **remplace la liste** par un mappage en `PodiumItem` (avatar = **pastille `t.color` + icône équipe**, sous-titre = « N membres », `currentLabel="mon équipe"`) → `<PodiumBoard title="Podium des équipes" footer="Score d'équipe = somme…" .../>`. Podium pour le top 3, **reste des équipes listé proprement en dessous**.
- Ancien rendu liste + `PodiumSpot`/`RankRow` locaux **supprimés** (remplacés par le partagé).

## 4. Règles respectées
- **Vraies données** : scoring d'équipe existant **réutilisé** (`/api/competition/leaderboard/teams`, Σ membres) — **rien recalculé**.
- **Pastilles d'équipe conservées** (`t.color`, identité). Médailles or/argent/bronze = exception documentée.
- **État vide** propre (« Aucune équipe classée… »).
- **Animations identiques** : estrade en `Reveal` (1er en dernier), lignes en `Stagger`, `CountUp` sur les scores, hover `v-hover`/`v-row`, focus (tokens). `prefers-reduced-motion` respecté (Reveal/Stagger/CountUp).
- **0 token neutre, 0 vert cyber** ; seuls hex = les 3 médailles (dans `PodiumBoard`, documentées).

## 5. Vérifs / Rapport
- `tsc --noEmit` **OK** · `eslint` (page + PodiumBoard) **0** · `npm run build` **OK** (`/dashboard/competition`).
- Le podium ÉQUIPE est désormais un **vrai podium visuel**, **identique en DA et animations** au podium individuel, via un **composant partagé** (`PodiumBoard`) — DRY. Build OK, 0 couleur en dur (hors médailles). **100 % LOCAL**, commit local. **STOP.**
