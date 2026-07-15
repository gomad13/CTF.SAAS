-- =============================================================================
-- FALLBACK : appliquer UNIQUEMENT la migration de la feature "mot de passe oublié"
-- si `dotnet ef database update` refuse (drift de migrations en prod).
-- Idempotent (IF NOT EXISTS / ON CONFLICT). N'ajoute QUE ce dont la feature a besoin :
--   - Users.SecurityStamp  (révocation immédiate des JWT)
--   - table PasswordResetTokens (hash SHA-256, usage unique, expiration)
-- Ne dépend d'aucune autre migration en attente.
--
-- Usage (sur le serveur) :
--   DBP=$(grep 'Mot de passe BDD' /home/ubuntu/sentys-credentials.txt | cut -d':' -f2 | xargs)
--   PGPASSWORD="$DBP" psql -h localhost -U sentys_app -d ctf_training -v ON_ERROR_STOP=1 \
--       -f scripts/prod/pwreset_migration_only.sql
-- =============================================================================
BEGIN;

ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SecurityStamp" text;

CREATE TABLE IF NOT EXISTS "PasswordResetTokens" (
    "Id"        uuid NOT NULL,
    "UserId"    uuid NOT NULL,
    "TenantId"  uuid NOT NULL,
    "TokenHash" character varying(64) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "UsedAt"    timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "RequestIp" text,
    CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_PasswordResetTokens_TokenHash" ON "PasswordResetTokens" ("TokenHash");
CREATE INDEX IF NOT EXISTS "IX_PasswordResetTokens_UserId"    ON "PasswordResetTokens" ("UserId");

-- Enregistre la migration comme appliquée pour qu'EF ne la rejoue pas.
INSERT INTO "__EFMigrationsHistory" ("MigrationId","ProductVersion")
VALUES ('20260715014205_AddPasswordResetAndSecurityStamp','8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
