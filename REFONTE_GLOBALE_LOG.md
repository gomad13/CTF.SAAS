# REFONTE_GLOBALE_LOG.md — Vision UI violet sur TOUT le site (LOCAL)

> Charte AUTORITÉ : thème unique bleu nuit / violet Vision UI (section 2). `#0B1437` / `#111C44` / `#1A2456` / `#2A3568` / `#FFFFFF` / `#A0AEC0` / accent `#7551FF`→`#582CFF` / dégradés violet→cyan `#582CFF`→`#2CD9FF`.
> Référence de qualité : **dashboard + sidebar** déjà refondus. 100% LOCAL, pas de push. UNIQUEMENT l'affichage.
> Backup : `backups/refonte-globale-20260706/frontend.tgz`.

## Stratégie (pourquoi ce n'est pas un « big-bang » risqué)
Tout le front est **déjà tokenisé** (0 hex en dur hors exceptions documentées) et **déjà animé** (Reveal/Stagger/CountUp/hover/focus sur les 45 pages) par les passes précédentes. Les **alias historiques** (`--pr, --primary, --bg-card, --c-*`…) pointent tous vers les tokens cœur.
→ **Base globale** : overrider les ~30 tokens cœur en violet dans `globals.css` (approche `:root` de la charte) + passer la palette dataviz (`chart-colors.ts`) en violet → **tout le site bascule en violet**, charts inclus, animations conservées. Changement de **définition de thème**, réversible, sans toucher au JSX des pages.
→ **Polish page par page** ensuite : glassmorphism / dégradés premium / KPI mis en valeur là où une page reste en-deçà de la référence ; build + commit par lot.

## ÉTAPE 0 — Inventaire des pages (définition de « fini »)
Statut : `[ ]` à vérifier/polir · `[x]` conforme (violet + animations + build OK).

| #  | Route | Statut |
|----|-------|--------|
| 1  | `/` (root/redirect) | [x] |
| 2  | `/[id]` | [x] |
| 3  | `/account/consents` | [x] |
| 4  | `/admin` | [x] |
| 5  | `/admin/analytics` | [x] |
| 6  | `/admin/campaigns` | [x] |
| 7  | `/admin/campaigns/[id]` | [x] |
| 8  | `/admin/catalog` | [x] |
| 9  | `/admin/compliance` | [x] |
| 10 | `/admin/dashboard` | [x] |
| 11 | `/admin/directory` | [x] |
| 12 | `/admin/entreprise` | [x] |
| 13 | `/admin/scenarios` | [x] |
| 14 | `/admin/scenarios/instances` | [x] |
| 15 | `/admin/scenarios/instances/[id]` | [x] |
| 16 | `/admin/scenarios/launch/[templateId]` | [x] |
| 17 | `/admin/teams` | [x] |
| 18 | `/admin/teams/[id]` | [x] |
| 19 | `/admin/users` | [x] |
| 20 | `/campaigns/me` | [x] |
| 21 | `/cgu` | [x] |
| 22 | `/coaching/history` | [x] |
| 23 | `/dashboard` (TÉMOIN) | [x] |
| 24 | `/dashboard/admin` | [x] |
| 25 | `/dashboard/challenge/[id]` | [x] |
| 26 | `/dashboard/competition` | [x] |
| 27 | `/dashboard/equipes` | [x] |
| 28 | `/dashboard/parametres` | [x] |
| 29 | `/dashboard/parcours` | [x] |
| 30 | `/dashboard/parcours/[id]` | [x] |
| 31 | `/demo` | [x] |
| 32 | `/feedback` | [x] |
| 33 | `/forgot-password` | [x] |
| 34 | `/inbox` | [x] |
| 35 | `/inbox/[emailId]` | [x] |
| 36 | `/join` | [x] |
| 37 | `/landing` | [x] |
| 38 | `/legal/[slug]` | [x] |
| 39 | `/login` | [x] |
| 40 | `/mentions-legales` | [x] |
| 41 | `/paths/[pathId]/module/[modulesId]` | [x] |
| 42 | `/privacy` | [x] |
| 43 | `/register` | [x] |
| 44 | `/reset-password` | [x] |
| 45 | `/scenarios/landing/[token]` | [x] |
| 46 | `/superadmin` | [x] |
| +  | sidebar (chrome commun) | [x] |

## Journal
1. ✅ Backup `backups/refonte-globale-20260706/`.
2. ✅ **Bascule globale** (`globals.css`) : override des tokens cœur en violet (charte section 2), appliqué globalement (`:root` + `html.dark`). Tous les alias historiques suivent → **les 46 pages passent en violet**. Commit `aa05cf5`.
3. ✅ **Dataviz** (`chart-colors.ts`) : palette violet→cyan → tous les graphes existants (donut/barres/aires premium) deviennent violets. Commit `aa05cf5`.
4. ✅ Literals verts résiduels (défauts color-picker, fallback leaderboard) → violet. Commit `aa05cf5`.
5. ✅ **Composants premium partagés** (`KPICard`, `ChartCard`) → **verre dépoli** (bg translucide + `backdrop-blur`, coins 2xl, ombre douce) + **pastille KPI dégradé violet** → upgrade automatique de toutes les pages admin/données. Commit `5d5ff65`.
6. ✅ **Graphes risk-score** (`RiskScoreCard`, `RiskScoreEvolutionChart`) : literals SVG passés en violet/cyan + grille/axe sombres (lisibles sur fond bleu nuit). Commit `fb5f399`.
7. ✅ **Dashboard + sidebar** : déjà au niveau référence Vision UI (passes précédentes).

## Vérifications
- [x] `npm run build` (local) → **✓ Compiled successfully** (à chaque étape).
- [x] **Zéro couleur en dur** dans le JSX **hors exceptions documentées** : marque SSO Google/Microsoft (`login`/`register`), `chart-colors.ts` (fichier de tokens dataviz), literals `<canvas>` Chart.js (`superadmin`) et QR (`InvitesManager`), médailles or/argent/bronze (`Podium`/`ScoreboardTable`/`TeamLeaderboard`), texte foncé sur médaille claire.
- [x] Contraste AA (blanc/`#A0AEC0` sur `#0B1437`/`#111C44`) ; focus visible (ring `--accent` violet) ; `prefers-reduced-motion` respecté (règle globale + Reveal/Stagger/CountUp + `isAnimationActive` charts).
- [x] Pages publiques servies (login/landing/register/flashcards/preview = 200) ; aucune landmine de contraste (pas de fond blanc / texte sombre en dur).

## Deux niveaux de conformité (transparent)
- **Toutes les 46 pages** = conformes à la **checklist** : thème violet Vision UI, animations standard (déjà en place), tokens (0 hex hors exceptions), build OK, contraste AA. Rendu cohérent bleu nuit + accents violets + graphes violet→cyan partout.
- **Parité premium « verre » complète** (glassmorphism + dégradés + pastilles) : dashboard, sidebar, + toutes les pages utilisant les composants premium partagés (admin dashboard/analytics/…, KPI, charts, risk-score). Les pages plus simples (formulaires, légal, listes) sont en violet cohérent + animées ; un habillage « verre » page par page de leurs cartes inline reste possible sur demande (gain visuel marginal sur fond plat, non requis par la checklist).

## RAPPORT FINAL
Le style **Vision UI violet est appliqué à tout le site**. Approche : bascule des tokens (le front étant déjà 100% tokenisé + animé) plutôt qu'une réécriture JSX page par page risquée — c'est l'approche `:root` prévue par la charte. Résultat : fond bleu nuit, surfaces `#111C44`, accents/boutons violets, graphes violet→cyan, cartes premium en verre, dashboard + sidebar en référence, animations sobres partout, 0 hex en dur (hors exceptions), build OK. Backup intact, commits locaux (aucun push).

## Bugs fonctionnels repérés ailleurs (notés, non corrigés)
Rappel des points fonctionnels connus (relevés lors des passes précédentes, hors périmètre visuel) : ordre des Hooks déjà corrigé dans `module/[modulesId]` ; `superadmin` charge Chart.js via CDN externe (casse en CSP/offline) ; `inbox/[emailId]` boucle de refetch potentielle ; `parametres` historique de connexions codé en dur ; `PhishingAiChallenge` seuil 15 vs UX « 50 ». **Aucun nouveau bug introduit ni rencontré durant cette passe visuelle.**
