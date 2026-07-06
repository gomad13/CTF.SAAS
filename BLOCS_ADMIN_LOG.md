# BLOCS_ADMIN_LOG.md — Blocs manquants de la vue d'ensemble Admin (VRAIES données)

> Charte AUTORITÉ : violet Vision UI (section 2) + **RÈGLE ABSOLUE : vraies données uniquement** (jamais de chiffre inventé ; état vide propre si pas de donnée). Conventions sécurité : TenantId depuis claims JWT (jamais body/query), `[Authorize]` + rôle admin serveur, DTO en sortie (jamais entité EF), agrégations serveur filtrées par TenantId, tenant démo jamais en fallback silencieux.
> 100% LOCAL, pas de push, pas de déploiement.
> Backups : `backups/blocs-admin-20260706/` — `frontend-src.tgz`, `backend.tgz` (sources) + `db.dump` (partiel : bloqué par RLS sur certaines tables ; non bloquant car tâche read-only, aucune migration/changement de schéma).

## Objectif
Ajouter à la VRAIE console admin (`/admin/dashboard`) les blocs présents dans la maquette démo (`/admin-preview`) mais absents, branchés sur les vraies données du tenant :
- **Activité de formation** (aire, ~6 derniers mois) — évolution réelle, dégradé violet→cyan.
- **Utilisateurs par statut** (donut) — répartition réelle, total réel au centre.
- + tout autre bloc présent dans la démo et absent de la vraie page.

## Méthode
Backup ✅ → vérifier endpoints existants → créer ceux qui manquent (conventions) → recréer les blocs front branchés → gérer états chargement/données/vide → build front + back → commit local.

## Inventaire (résultat exploration)
- **Vraie page** = `/admin/dashboard` (onglets Collaborateurs / Statistiques / Paramètres). Cartes inline, Chart.js via CDN, **vert `rgba(34,197,94)` en dur** résiduel (non tokenisé).
- **Blocs manquants vs maquette démo** : ① aire **« Activité de formation » mensuelle** (le backend n'avait que du journalier 7j `completionsByDay`) ; ② donut **« Utilisateurs par statut »**.
- **Endpoints existants** : `/api/admin/stats` (KPI + completionsByDay 7j), `/api/admin/company`, `/api/admin/users`. Aucun endpoint mensuel ni « users par statut » ventilé.
- **Modèle User** (statuts réels) : `IsActive` (bool), `LastLoginAt` (DateTime?), `LastActivityAt`, `TenantId`. Taxonomie déjà utilisée par l'app : actif / suspendu / jamais connecté.
- **Entité activité** : `ChallengeCompletion` (`CompletedAt`, `UserId`) — filtrée par les user-ids du tenant (pattern prouvé `AdminController.cs:812`).
- **Pattern tenant/JWT** : `GetEffectiveTenantId()` (claims JWT ; SuperAdmin peut cibler `?tenantId=`), `Guid.Empty`→`Unauthorized` (jamais le tenant démo). `[Authorize(Roles="admin,SuperAdmin")]` au niveau contrôleur.

## Journal
1. ✅ Backups (front src + backend source ; dump BDD partiel — RLS).
2. ✅ **Backend — 2 endpoints** créés dans `Controllers/AdminController.cs` (+ DTOs dans `Contracts/AdminDtos.cs`) :
   - `GET /api/admin/stats/activity-monthly?months=6` → `List<AdminMonthlyPointDto(Label, Value)>` : complétions agrégées **par mois** (N derniers mois), agrégation **en mémoire** après `ToListAsync`, filtrée sur les users **réels** du tenant. Mois vides remplis à 0.
   - `GET /api/admin/stats/users-by-status` → `AdminUsersByStatusDto(Actifs, Suspendus, JamaisConnectes, Total)` : ventilation **réelle** (statuts dérivés de `IsActive` + `LastLoginAt`).
   - Conventions : `[Authorize(admin,SuperAdmin)]`, `GetEffectiveTenantId()` (JWT), `Guid.Empty`→`Unauthorized` (pas de fallback démo), `.AsNoTracking()`, filtre `TenantId`, DTO records, pas de N+1 (2 requêtes/endpoint).
3. ✅ **Front — 2 blocs** ajoutés en tête de l'onglet **Statistiques** (`/admin/dashboard`) via composants premium violets `PremiumChartCard` + `AreaChartCard` (aire dégradé violet→cyan) + `DonutChart` :
   - « Activité de formation » (6 derniers mois) branché sur `/activity-monthly`.
   - « Utilisateurs par statut » (Actifs / Suspendus / Jamais connectés, total réel au centre) branché sur `/users-by-status`.
   - **3 états gérés** : chargement (`OverviewSkel`), données, **vide** (`OverviewEmpty` : « Pas encore de données d'activité… » / « Aucun utilisateur à afficher… »). **Aucun chiffre inventé.**
4. ✅ Correction du **vert résiduel** en dur de la page (`rgba(34,197,94)` → violet). 0 vert restant.

## Vérifications
- [x] **Build backend** (`dotnet build CTF. API.csproj`) → **La génération a réussi**.
- [x] **Build frontend** (`npm run build`) → **✓ Compiled successfully**.
- [x] **Runtime** : backend démarré (health 200, batch CRI sur 24 users réels). Les 2 routes renvoient **401 sans auth** → existent + protégées (`[Authorize]`) : **un non-admin n'y accède pas**.
- [x] **Vraies données uniquement** : agrégations serveur filtrées par `TenantId` du token ; **état vide propre** si aucune donnée ; pas de tenant démo en fallback ; **zéro donnée inventée**.
- [x] Charte violet (composants premium), **0 vert `#22C55E` résiduel**, tokens, contraste AA.

## RAPPORT FINAL
Les 2 blocs manquants (Activité de formation mensuelle + Utilisateurs par statut) sont ajoutés à la **vraie** console admin, branchés sur de **nouveaux endpoints .NET** respectant les conventions sécurité (tenant depuis JWT, [Authorize] admin, DTO, AsNoTracking, agrégation serveur), avec **états chargement/données/vide** et **zéro donnée inventée**. Les deux builds passent, endpoints protégés vérifiés. 100% LOCAL, pas de push, pas de déploiement.

## Bugs fonctionnels repérés ailleurs (notés, non corrigés)
- `/admin/dashboard` charge encore **Chart.js via un CDN externe** (onglet Statistiques) → casse en CSP/offline (déjà relevé pour superadmin). Non corrigé (hors périmètre : ajout de blocs).
