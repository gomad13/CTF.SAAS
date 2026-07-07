# ANALYTICS_LOG.md — Refonte Analytics admin (onglet ENTREPRISE)

> Objectif : refondre `/admin/analytics` (doublon actuel) en **3 onglets** Entreprise / Groupe / Individuel. **Cette passe = onglet ENTREPRISE uniquement** ; Groupe + Individuel en placeholder « bientôt disponible ».
> Règles : VRAIES données du tenant (TenantId via JWT), jamais de donnée inventée, état vide propre. `[Authorize]` admin serveur, DTO, pas de N+1, agrégation SQL, pas de fallback tenant démo. Charte violet, bloc « points faibles » dominant, AA, zéro vert résiduel. 100% LOCAL, pas de push.
> ⚠️ Le fichier `PROMPT_ANALYTICS_ENTREPRISE.md` est **absent** du repo — je procède depuis la spec inline du message.

## Backup ✅
`backups/analytics-entreprise-20260706/` : `frontend-src.tgz`, `backend.tgz`, `ctf_training_local.sql` (928K, 70 tables, dump superuser postgres = RLS contournée).

---

## Inventaire

### Existant (le « doublon »)
- **Backend** `AnalyticsController` (`Controllers/AnalyticsController.cs`) : `GET /api/analytics/overview?days=30` + `/api/analytics/status`. `[Authorize(admin,SuperAdmin)]`, tenantId via `_tenant.TenantId` (JWT) + `Guid.Empty→Unauthorized`, **gated par ModeToggle (mode Analytics)**. DTO `AnalyticsOverviewDto` = KPIs (actifs 7j/30j, complétions, score moyen) + activité/jour + par parcours + par type. Agrégations `GroupBy` dans `AnalyticsService`.
- **Front** `admin/analytics/page.tsx` (150 l.) : état « Analytics désactivés » si mode off, KPIs, activité (N jours), complétions par parcours, répartition par type. Reveal/Stagger/CountUp, tokens `fg-*`.
- **Doublon** : ces KPIs/activité recoupent l'onglet Statistiques du dashboard admin.

### Ce qui MANQUE pour l'onglet Entreprise cible
1. **Points faibles à renforcer** (thèmes les plus faibles) — **bloc phare, inexistant**.
2. **Risque cyber global + courbe de progression** — inexistant (l'existant a une courbe d'activité, pas de risque).
3. **Engagement** (participation, assiduité, actifs 7j/30j) — actifs 7j/30j existent (réutilisables).
4. **Sélecteur de période + export CSV** (PDF en TODO).

### Données réelles disponibles (BDD locale)
- **Thème d'un challenge** : `Challenge.Category` (string?, nullable). **104/112 renseignés (93%)**, mais **59 valeurs distinctes avec doublons de casse/espaces** (ex. « Ingénierie Sociale » ×10 vs « Ingénierie sociale » ×2) + 8 null.
- **Score par complétion** : `ChallengeCompletion.ScorePercent` (+ `ChallengeId`, `UserId`, `TenantId`, `CompletedAt`). Le thème d'un score = `Category` du `Challenge` lié.
- **Risque** : `RiskScoreHistories` (`UserId, TenantId, Score 0-100, Components(json), ComputedAt`) — 327 lignes → risque tenant = moyenne des `Score` + courbe par période.
- **Engagement** : `Users.LastLoginAt / LastActivityAt / CreatedAt` ; `ChallengeCompletions.CompletedAt`.
- Tenants **locaux** : CyberMed Innovations (14 users), Demo (10). (Prepa Bloc/Poitier = prod, absents en local.)

---

## ⚠️ Notions NON définies en base — à VALIDER avant de coder (aucune invention)

### N1 — Définition du « thème » (bloc Points faibles)
`Challenge.Category` existe mais est **incohérent** (casse/espaces, 59 variantes, 8 null).
**Proposition** : thème = `Category` **normalisée** (trim + regroupement insensible à la casse, libellé canonique = 1re occurrence). Challenges sans catégorie → bucket « Non catégorisé » **exclu du bloc points faibles** (ou affiché à part). 
→ **À valider** : (a) utiliser `Category` comme thème ? (b) normalisation OK ?

### N2 — Métrique de « faiblesse »
Un thème est « faible » selon quoi ?
**Proposition** : par **score moyen** (`AVG(ScorePercent)` des complétions du tenant sur ce thème), **le plus bas = le plus faible**. Avec **seuil de fiabilité** : n'afficher qu'un thème ayant **≥ 3 complétions** (sinon échantillon non significatif → écarté ou marqué « données insuffisantes »). Bloc = **top 5 thèmes les plus faibles**.
→ **À valider** : (a) score moyen comme critère ? (b) seuil ≥ 3 complétions ? (c) top 5 ?

### N3 — Seuils de risque (bandes)
`RiskScoreHistories.Score` est 0-100, mais les bandes (faible/moyen/bon/excellent) ne sont **pas en base**.
**Proposition** : reprendre les seuils déjà utilisés côté dashboard : **≥80 Excellent · ≥60 Bon · ≥40 Moyen · <40 À renforcer**.
→ **À valider** : ces seuils conviennent ?

### N4 — Périmètre « risque global du tenant »
**Proposition** : moyenne du **dernier `Score` par utilisateur** du tenant (1 point courant par user) pour le chiffre global ; courbe = moyenne mensuelle des scores sur N mois. 
→ **À valider**.

---

## Plan (après validation des N1–N4)
1. Structure 3 onglets (Entreprise actif / Groupe + Individuel placeholder « bientôt disponible »).
2. Bloc **Points faibles** (endpoint `GET /api/analytics/enterprise/weak-topics` → DTO, agrégation SQL par Category normalisée, top-5, ≥3 complétions ; front bloc phare violet + état vide).
3. Bloc **Risque global + courbe** (endpoint `/enterprise/risk` → score global + points mensuels ; front jauge + aire).
4. Bloc **Engagement** (réutilise/complète actifs 7j/30j + participation + assiduité).
5. **Sélecteur de période + export CSV** (PDF TODO).
Chaque bloc : endpoint → front → états (chargement/données/vide) → build back+front → commit. Pas de big-bang.

## Décisions validées (N1–N4)
- **N1 Thème** = `Challenge.Category` **normalisée** (trim + insensible casse ; libellé canonique = 1re occurrence). Non catégorisés exclus.
- **N2 Faiblesse** = **combiné score × complétion** → `Mastery = AvgScore × CompletionRate / 100` (bas = faible). `CompletionRate = complétions / (users × challenges du thème)`. Seuil de fiabilité **≥ 3 complétions**, **top 5**.
- **N3 Seuils risque** = **80/60/40** (Excellent/Bon/Moyen/À renforcer).
- **N4 Risque global** = moyenne du **dernier score par user** ; **courbe mensuelle** (moyenne du mois, report du dernier connu si mois vide — jamais 0 inventé).

## Journal
1. ✅ **Backend** (`Contracts/EnterpriseAnalyticsDtos.cs` + `Controllers/EnterpriseAnalyticsController.cs`, route `api/analytics/enterprise`) — 4 endpoints :
   - `GET weak-topics?top=5` → `EnterpriseWeakTopicsDto` (top thèmes faibles).
   - `GET risk?months=6` → `EnterpriseRiskDto` (score global + bande + courbe).
   - `GET engagement` → `EnterpriseEngagementDto` (actifs 7j/30j, participation, jamais connectés…).
   - `GET export?months=6` → CSV (rapport). PDF = **TODO** (non fait, trop lourd pour cette passe).
   - Conventions : `[Authorize(admin,SuperAdmin)]`, tenant via `_tenant.TenantId` (**JWT**) + `Guid.Empty→Unauthorized`, **gate mode Analytics** (403 sinon), DTO records, `.AsNoTracking()`, filtre `TenantId` / jointure sur challenges du tenant, **pas de fallback tenant démo**, agrégations SQL + regroupement date/normalisation en mémoire (pas de N+1). Logique factorisée en `Compute*Async` (réutilisée par l'export).
2. ✅ **Frontend** (`admin/analytics/page.tsx` refondu) — **3 onglets** Entreprise (actif) / Groupe / Individuel (placeholder « Bientôt disponible »). Wrappé `.vision-dashboard` (violet). Onglet Entreprise, dans l'ordre de priorité :
   - **Bloc phare « Points faibles à renforcer »** : dominant (bordure danger, icône alerte), top 5 thèmes avec barre de maîtrise colorée (rouge<40, ambre<60, violet), score/complétion/nb — lisible en 3 s. État vide propre.
   - **Risque global** (jauge `VisionGauge` + bande) + **courbe** (aire violet→cyan `VisionAreaChart`).
   - **Engagement** (KPI count-up : actifs 7j/30j, participation, jamais connectés).
   - **Sélecteur de période** (3/6/12 mois) + **export CSV** (téléchargement blob).
   - 3 états gérés partout (skeleton / données / vide). Charte violet, tokens, 0 hex.

## Vérifications
- [x] **Build backend** (`dotnet build -c Release/Debug`) → **réussi**. **Build frontend** (`npm run build`) → **✓ Compiled successfully**.
- [x] **Runtime** : backend démarré (health 200) ; les 4 routes `api/analytics/enterprise/*` → **401 sans auth** (existent + protégées `[Authorize]`).
- [x] **Sécurité/RGPD** : tenant du JWT, jamais démo en fallback, DTO, pas de N+1. Charte violet, 0 vert résiduel, AA.
- ⚠️ **Test données réelles** : nécessite une session admin authentifiée (non réalisable dans cette passe headless). Build + schéma + routes vérifiés ; requêtes calquées sur patterns éprouvés. À valider visuellement une fois connecté (tenant local CyberMed Innovations a 14 users + données).

## Notes / à valider ultérieurement
- **Normalisation Category** résout les doublons de casse mais 8 challenges sans catégorie sont exclus du bloc points faibles (attendu). Une vraie table de « thèmes » référentiels serait plus robuste (évolution future).
- **PDF** de l'export = TODO (CSV livré).
- **Doublon** : l'ancien `/api/analytics/overview` + ses sections restent ; recoupement à rationaliser dans une passe ultérieure (Groupe/Individuel).

## RAPPORT FINAL — onglet ENTREPRISE
Onglet Entreprise complet sur **vraies données** (points faibles combinés, risque global + courbe, engagement, période + export CSV), structure **3 onglets** en place (Groupe/Individuel en placeholder), **2 builds OK**, sécurité (JWT/[Authorize]/DTO/pas de N+1/pas de démo) + charte violet + RGPD respectés. 100% LOCAL, pas de push.

---

# PASSE 2 — Onglet GROUPE

## Inventaire
- **Groupe = `Team`** (Teams : Name, Color, Icon, Manager…). Appartenance via **`TeamMemberships`** (Id, TenantId, TeamId, UserId, JoinedAt) — un user peut être dans plusieurs équipes. **`User.TeamId` inutilisé** (vide).
- ⚠️ **Données locales vides** : CyberMed a 3 équipes (finance, ISO-Cyber-Team, Team Cyber) mais **0 membre** (TeamMemberships vide) → construit avec états vides propres, **non validable sur vraies données en local** (prod potentiellement peuplée).

## Décisions validées
- **Structure = les deux** : classement des équipes (bloc phare) **+** drill-down par équipe (blocs points faibles/risque/engagement scopés).
- **Faiblesse d'une équipe = maîtrise combinée** (score × complétion), agrégée sur les membres.

## Journal
1. ✅ **Backend** (mêmes conventions, dans `EnterpriseAnalyticsController` + DTO `GroupRowDto`/`GroupsComparisonDto`) :
   - `GET /api/analytics/groups` → classement des équipes (une passe : memberships + complétions + risque chargés une fois, agrégation par équipe en mémoire ; **pas de N+1**). Métriques/équipe : maîtrise, score moyen, taux complétion, risque moyen (dernier score des membres, seuils 80/60/40), participation, nb membres. Tri par maîtrise croissante (plus faible en 1er).
   - `GET /api/analytics/groups/{teamId}/weak-topics|risk|engagement` → **drill-down** : réutilise les `Compute*Async` refactorés avec un scope `memberIds` (membres de l'équipe). `404` si l'équipe n'appartient pas au tenant.
   - `GetTeamMemberIdsAsync` : membres via `TeamMemberships` filtré `TenantId+TeamId` ; null si équipe hors tenant.
2. ✅ **Frontend** (`admin/analytics/page.tsx`) : onglet **Groupe** = classement cliquable des équipes (rang, barre de maîtrise colorée, membres/participation/risque) + **drill-down** (bouton retour → blocs détaillés de l'équipe via composant `AnalyticsDetail` factorisé, réutilisé aussi par l'onglet Entreprise). États chargement/données/vide partout. Charte violet, 0 hex.
3. Refactor : les 3 blocs Entreprise extraits en `<AnalyticsDetail basePath keyPrefix showExport>` → réutilisés tels quels pour le détail d'une équipe (scope backend via l'URL).

## Vérifications (passe Groupe)
- [x] Build backend (Release/Debug) **réussi** ; build frontend **✓ Compiled successfully** ; 0 hex.
- [x] Runtime : 4 routes `api/analytics/groups*` → **401 sans auth** (existent + protégées).
- [x] Sécurité/RGPD : tenant JWT, scope équipe = membres du tenant uniquement (jointure TeamMemberships filtrée tenant), pas de N+1, pas de démo. Charte violet.
- ⚠️ Test données réelles non faisable en local (équipes sans membres) — à valider une fois connecté sur un tenant avec équipes peuplées.

## STOP — Individuel non traité (à faire dans une passe ultérieure).
