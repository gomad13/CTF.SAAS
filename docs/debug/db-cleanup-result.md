# Résultat cleanup DB — Bêta DSI

> Première exécution : **2026-04-27**. Recheck propre : **2026-04-28**.
> Script appliqué : `scripts/cleanup/cleanup-test-data.sql`.

## Lignes supprimées

| Table | Avant | Après | Diff |
|---|---|---|---|
| `Paths` (titres `%test%` ou `%E2E%`) | 3 | 0 | **-3** (1 `E2E Test Path` + 2 `AuditTestPathEdited`) |
| `Teams.Name = 'test'` | 1 | 0 | **-1** |
| `Tenants.Name IN ('AuditTestRenamed','lateteatoto')` | 2 | 0 | **-2** |
| Users déplacés vers Demo (UPDATE) | 0 | n/a | aucun à migrer |

## État final DB (recheck 2026-04-28)

```
=== Tenants restants ===
 Name                 | IsActive
----------------------+----------
 CyberMed Innovations | t
 Demo                 | t

=== Paths restants avec 'test' ou 'E2E' ===
0 ligne ✅

=== Teams 'test' ===
0 ligne ✅

=== Users orphelins (TenantId sans Tenant correspondant) ===
0 ligne ✅

=== Users par tenant ===
CyberMed Innovations | 11
Demo                 |  5

=== Counts globaux ===
tenants =  2
paths   = 11
teams   =  4
users   = 16
```

## Vérifications passées ✅

- Aucun parcours résiduel `test` / `E2E` / `AuditTest`
- Aucune équipe `test`
- Aucun tenant polluant
- **Aucun user orphelin** (FK `Users.TenantId` → `Tenants.Id` cohérente)
- Tenant Demo et CyberMed Innovations préservés
- Tous les seeds applicatifs (Demo path, parcours médical, parcours sensibilisation, parcours catalogue) intacts

## Conclusion

Cleanup intégral. Le script `cleanup-test-data.sql` peut être ré-exécuté à tout moment ; il est idempotent (les patterns de filtre ne matchent plus rien après le 1er passage).
