-- ============================================================
-- Cleanup données polluantes — Bêta DSI
-- Appliqué le 2026-04-27 (cf. docs/debug/db-cleanup-result.md)
-- Idempotent : peut être ré-exécuté sans effet si déjà nettoyé.
-- ============================================================
-- Convention de noms EF Core : tables PascalCase + quotes.
-- Schéma legacy snake_case présent en parallèle (ignoré ici, dette technique).

BEGIN;

-- 1. Détacher les éventuels users rattachés à l'équipe "test"
UPDATE "Users"
SET    "TeamId" = NULL
WHERE  "TeamId" IN (SELECT te."Id" FROM "Teams" te WHERE te."Name" = 'test');

-- 2. Supprimer parcours de test (ON DELETE CASCADE sur Modules/Challenges/Submissions/Progresses
--    via les contraintes EF Core — confirmé en migration AppDbContext).
DELETE FROM "Paths"
WHERE  "Title" ILIKE '%E2E%'
   OR  "Title" ILIKE '%test path%'
   OR  "Title" ILIKE '%audittest%';

-- 3. Supprimer équipe(s) "test"
DELETE FROM "Teams" WHERE "Name" = 'test';

-- 4. Déplacer d'éventuels users légitimes des tenants à supprimer vers le tenant Demo
--    (filtre sur emails non-test pour éviter de déplacer des comptes d'audit purs)
UPDATE "Users"
SET    "TenantId" = '00000000-0000-0000-0000-000000000000'  -- Tenant Demo (GUID fixe seed)
WHERE  "TenantId" IN (
           SELECT t."Id" FROM "Tenants" t
           WHERE  t."Name" IN ('AuditTestRenamed', 'lateteatoto')
       )
  AND  "Email" NOT LIKE '%@test%'
  AND  "Email" NOT LIKE '%@audit%'
  AND  "Email" NOT LIKE '%@example%';

-- 5. Supprimer les tenants polluants (CASCADE supprimera ce qui reste lié)
DELETE FROM "Tenants" WHERE "Name" IN ('AuditTestRenamed', 'lateteatoto');

COMMIT;

-- ============================================================
-- Vérification post-cleanup
-- ============================================================
-- Aucun orphelin :
--   SELECT u."Email" FROM "Users" u
--   LEFT JOIN "Tenants" t ON u."TenantId" = t."Id"
--   WHERE t."Id" IS NULL;
-- → doit retourner 0 ligne.
