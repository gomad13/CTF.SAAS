# Audit dépendances — Cleanup données polluantes (Bêta DSI)

> Conduit le 2026-04-27 avant suppression. Recheck le 2026-04-28 confirme état propre.

## Cibles

| Type | Identifiant | Présence pré-cleanup |
|---|---|---|
| Parcours `E2E Test Path` | titre contenant `E2E` ou `test path` | 1 ligne (CyberMed) |
| Parcours `AuditTestPathEdited` | résiduels d'audits précédents | 2 lignes |
| Équipe `test` | `Teams.Name = 'test'` | 1 équipe (CyberMed) |
| Tenant `AuditTestRenamed` | `Tenants.Name = 'AuditTestRenamed'` | 1 tenant (actif=t) |
| Tenant `lateteatoto` | `Tenants.Name = 'lateteatoto'` | 1 tenant (actif=f) |

## Dépendances vérifiées avant DELETE

### Parcours de test
- Modules associés : 0 ligne
- Challenges associés : 0 ligne
- Assignments associés : 0 ligne
- Submissions / Progresses associés : aucun (filtrés par join sur paths.Id)

→ **Suppression directe possible** (pas de cascade significatif à anticiper).

### Équipe `test`
- Membres rattachés (`Users.TeamId`) : **0 user**

→ Détachement préventif `UPDATE Users SET TeamId=NULL` puis DELETE — safe.

### Tenants `AuditTestRenamed` + `lateteatoto`
- Users rattachés : **0** sur les 2 tenants
- Paths rattachés : 0
- Teams rattachées : 0
- Submissions / Progresses : 0

→ **Cascade DELETE EF Core sur `Tenants` ON DELETE CASCADE** propage proprement aux entités enfantes (vérifié dans `AppDbContext.cs` configurations Fluent API). Pas de risque d'orphelin.

## Migration des comptes légitimes

Si des users légitimes étaient présents sur les tenants à supprimer, le script les déplace vers le tenant Demo (`00000000-0000-0000-0000-000000000000`) **avant** la suppression des tenants. Filtre exclusif sur emails `@test` / `@audit` / `@example` pour ne pas migrer des comptes d'audit purs.

→ Dans le cas pré-cleanup observé : **0 user à migrer**.

## Risques identifiés

| Risque | Sévérité | Atténuation |
|---|---|---|
| Suppression cascade involontaire d'entités liées au tenant Demo | HIGH | Le filtre `WHERE Tenants.Name IN ('AuditTestRenamed','lateteatoto')` est strict ; pas de match sur `'Demo'` |
| Contraintes RLS PostgreSQL bloquant le DELETE | LOW | Les tables PascalCase EF n'ont pas de RLS active (RLS est sur les tables snake_case fantômes, ignorées par l'app) |
| Schéma double (PascalCase EF + snake_case legacy) | MED — dette | Le cleanup ne touche que PascalCase. Les tables snake_case sont inutilisées par le code .NET. À traiter en V1. |

## Décision

Procéder dans une transaction unique (`BEGIN;` … `COMMIT;`). Script idempotent.

Script final : `scripts/cleanup/cleanup-test-data.sql`.
