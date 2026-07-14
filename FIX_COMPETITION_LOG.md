# FIX_COMPETITION_LOG.md — Refonte Compétition (charte violet + RGPD anti-stigmatisation)

> Objectif : compétition motivante SANS stigmatiser les derniers. Top 5 public, « ma position » privée, podium équipe public, classement complet réservé admin. 100 % LOCAL, pas de push/déploiement.
> Autorité : `Instructions.md` absent → **CLAUDE.md** (charte §2, sécu §3/§7, RGPD §4).

## 0. Backup (méthode)
- `backups/competition-20260714_144014/` : `front/` (page + components), `back/` (CompetitionController, CompetitionService, ICompetitionService, CompetitionDtos), `schema.sql`.

## 1. Existant — endpoints (CompetitionController)
`[Route("api/competition")] [Authorize]` (tout membre) :
| Endpoint | Retour | RGPD |
|---|---|---|
| `GET status` | activé ? | ok |
| **`GET scoreboard?page&pageSize`** | `PagedResult<ScoreboardEntryDto>` | ⚠️ **classement nominatif COMPLET paginé exposé aux membres** |
| `GET podium` | `PodiumDto` | à vérifier : top 5 ? |
| **`GET leaderboard/individual`** | alias de scoreboard | ⚠️ **même fuite** |
| `GET leaderboard/teams` | `List<TeamLeaderboardEntryDto>` | ok (collectif) |
| `GET podium/teams` | `TeamPodiumDto` | ok |
| `GET my-rank` | `MyRankDto` (rang du connecté) | ok (privé) |
| `POST duration` | bonus rapidité | ok |

`[Route("api/admin/competition")] [Authorize(Roles="admin,SuperAdmin")]` : `PATCH toggle` uniquement (pas de classement complet admin encore).

### ⚠️ Constat RGPD (gravité ÉLEVÉ)
`GET /api/competition/scoreboard` (+ `leaderboard/individual`) sont **accessibles à tout membre** et renvoient un **classement nominatif complet paginé** → un membre peut lire le rang/score individuel de **tous** les autres (positions 6+). **À corriger** : côté membre, individuel = **top 5 (podium) + ma position privée** uniquement ; le complet passe **admin**.

## 2. Scoring existant (RÉUTILISÉ, non réinventé)
- **Individuel** = `BasePoints (Σ PointsEarned)` + `SpeedBonus (Σ round(points·0.5·clamp((90−durée)/90,0,1)))`. Filtré `TenantId` + `!IsDemo`, users actifs. Rang par `OrderByDescending(Total)` (`CompetitionService.ComputeUserScoresAsync` l.48-64, `GetRankedIndividualAsync` l.66-100).
- **Équipe** = Σ des scores des membres (via `TeamMemberships`, many-to-many ; un membre compte dans chaque équipe). Rang par score (`GetRankedTeamsAsync` l.121-166).
- **my-rank** = rang du connecté uniquement (par `userId` JWT) — privé (`GetMyRankAsync` l.177-198).
- **Team** : `Color` (hex libre), `Icon` (Lucide). Pas d'enum de couleurs.
- **Garde-fou nominatif admin** (référence) : `AdminScenariosController` = `[Authorize(Roles="admin,SuperAdmin")]` au niveau classe → **mirroré** (l'`AdminCompetitionController` l'a déjà).
- **Podium existant = top 3** → étendu à **top 5** (nouveau).

## 3. Plan
1. **Backend** :
   - **Podium individuel top 5** (public) — `podium` limité à 5 nominatifs max.
   - **`my-rank`** (privé) — réutilisé.
   - **Podium/leaderboard équipe** (public) — réutilisé (score équipe existant).
   - **Classement complet nominatif → ADMIN** : déplacer le scoreboard complet paginé sous `api/admin/competition/leaderboard` `[Authorize(admin,SuperAdmin)]` + tenant JWT + DTO ; **retirer/plafonner** le `/scoreboard` membre (top 5 max, ou 403). Réutiliser le même garde-fou que la vue nominative scénarios.
2. **Frontend** : page compétition refondue (charte violet) — onglets Individuel / Équipe ; podium top 5 + « ma position » privée ; podium équipe ; (côté admin, la vue complète là où c'est cohérent). États vides propres.
3. **Animations** : entrée podium en stagger (1er en dernier), count-up scores, hover, transitions onglets, focus. `prefers-reduced-motion`.

## 4. Garde-fous RGPD (cibles)
- [ ] Aucun endpoint membre n'expose de rang/score nominatif au-delà du top 5.
- [ ] `my-rank` ne renvoie que le rang du **connecté** (JWT), jamais celui d'un autre.
- [ ] Classement complet = `[Authorize] admin` serveur, tenant JWT, DTO, pas de démo.
- [ ] Podium équipe = collectif (non stigmatisant), complet autorisé.

## 5. Corrections / Rapport

### Backend (réutilise le scoring, corrige la fuite RGPD)
- **Fuite fermée** : `GET /api/competition/scoreboard` (+ alias `leaderboard/individual`) ne renvoient plus le classement complet paginé → **top 5 uniquement** (`GetTopIndividualsAsync`, clampé à 5 côté service = garde-fou dur).
- **`GET /api/competition/individual/top5`** (public) : top 5 nominatif.
- **`GET /api/competition/my-rank`** : réutilisé (position privée du connecté).
- **`GET /api/competition/leaderboard/teams`** + `podium/teams` : réutilisés (équipe, collectif).
- **`GET /api/admin/competition/leaderboard?page&pageSize`** (NOUVEAU, `[Authorize(admin,SuperAdmin)]`) : **classement nominatif complet** → `AdminLeaderboardDto { Purpose, Ranking }` (finalité rappelée + **log d'accès RGPD**). Tenant JWT, DTO, `!IsDemo`, pas de démo.

### Frontend (`dashboard/competition/page.tsx`, réécrit — charte `--v-*`)
- Wrap `.vision-dashboard`. **« Ma position » privée** (my-rank) : rang individuel + équipe. **Onglets** Individuel / Équipe / (Admin si rôle habilité, via `/api/auth/me`).
- **Individuel = podium TOP 5** (1er/2e/3e sur marches surélevées + 4e/5e en lignes). **Plus de liste nominative complète côté membre** (`ScoreboardTable` retiré → RGPD).
- **Équipe** = classement collectif complet (podium médailles + lignes).
- **Admin** = table nominative complète + bandeau de finalité (`purpose`).
- **Animations** : entrée `Reveal`, **1er du podium en dernier** (delays 0.05/0.15/0.32), **count-up** scores, hover (`v-hover`/`v-row`), **transition d'onglets** (`AnimatePresence`), focus (tokens). `prefers-reduced-motion` respecté.
- **Médailles or/argent/bronze** = 3 hex constants **documentés** (exception explicitement autorisée par le cahier des charges) ; couleurs d'équipe = `t.color` (vraie donnée). Sinon **0 hex, 0 vert cyber, 0 token neutre**.

### Garde-fous RGPD (validés)
- [x] Aucun endpoint membre n'expose de rang/score nominatif **au-delà du top 5** (scoreboard/individual plafonnés à 5, clamp serveur).
- [x] `my-rank` = uniquement le connecté (JWT).
- [x] Classement complet = `[Authorize(admin,SuperAdmin)]` serveur + tenant JWT + DTO + log d'accès, non exposé aux membres.
- [x] Podium équipe = collectif (non stigmatisant).

### Vérifs
- Build backend **OK** (0/0). Frontend `tsc` **OK**, `eslint` **0**, `npm run build` **OK** (`/dashboard/competition`). Endpoints **401** sans auth. Aucune erreur.
- Anciens composants `Podium/ScoreboardTable/TeamLeaderboard` **superseded** (non importés, non rendus) — laissés en dead code inoffensif.

### Bugs / notes
- (aucun bug fonctionnel repéré)
- Le classement individuel complet côté admin est rendu **dans la page compétition** (onglet gated rôle + endpoint `[Authorize] admin`) — cohérent avec « réservé admin ». Peut être déplacé vers `/admin` si souhaité.

### Rapport final
Podium individuel **top 5** (public) + **« ma position » privée** + **podium/classement équipe** (collectif) + **classement complet nominatif réservé admin** (garde-fou = celui des scénarios). Animations sobres (entrée/stagger 1er-en-dernier/count-up/hover/transition/focus). **Aucune exposition nominative des non-top-5 aux autres membres.** 2 builds OK, sécu+RGPD+charte OK. **100 % LOCAL**, commit local. STOP.
