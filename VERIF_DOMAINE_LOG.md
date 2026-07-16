# VERIF_DOMAINE_LOG.md — Vérification de domaine par tenant (PASSE 1/3)

> Fondation sécurité : prouver qu'un tenant possède un domaine email via un enregistrement DNS TXT,
> avant tout rattachement automatique (passe 2) ou SSO (passe 3).
> **Travail 100 % LOCAL — pas de push, pas de déploiement.** Autorité : `CLAUDE.md` (sécurité 3/6/7, RGPD 4, qualité 1, charte 5.4).
> Date : 2026-07-16.

---

## 1. Backup

`backups/verif-domaine-20260716-032526/` : `code-all.bundle` (git --all), `db-schema.sql.gz` (72 tables), `HEAD.txt`.
Note : dump **schéma** seul (les lignes sont protégées par RLS `FORCE ROW LEVEL SECURITY` pour `ctf_app_user` ; BDD dev re-seedable via `DbSeeder`). Migration additive → risque faible.

---

## 2. État de l'existant (exploration)

- `Models/TenantEmailDomain.cs` : `Id, TenantId, Domain, IsAutoProvisioningEnabled, CreatedAt, CreatedBy`. **Pas de champ de vérification.**
- `AppDbContext` : `DbSet<TenantEmailDomain>`, index uniques `Domain` (global) et `{TenantId,Domain}`.
- Migrations : `AddTenantEmailDomains` + `MakeDomainGloballyUnique`. Dernière migration = `20260715152004_EnforceSingleActiveSuperAdmin`.
- CRUD **SuperAdmin** existant : `SuperAdminDomainsController` (`api/superadmin/domains`) — inchangé.
- Consommateur métier : `TenantResolutionService` (match domaine→tenant au login SSO, flag `IsAutoProvisioningEnabled`) — **inchangé** (passe 2).
- Front : `ctf-web/src/app/admin/entreprise/page.tsx` (design system **Vision `--v-*`**, composants `VisionForm`, `CopyButton` + clipboard déjà présents).
- Aucune lib DNS ; `System.Net.Dns` ne fait pas TXT.

---

## 3. Modèle de données (ajouts)

Nouvelles colonnes sur `TenantEmailDomain` :

| Colonne | Type | Rôle |
|---|---|---|
| `VerificationToken` | `string?` (64) | nonce CSPRNG unique par (tenant,domaine), publié en DNS. `null` pour lignes historiques déjà vérifiées. |
| `IsVerified` | `bool` (def. false) | preuve DNS obtenue. |
| `VerifiedAt` | `DateTime?` | horodatage de la vérif réussie. |
| `VerifiedBy` | `Guid?` | admin ayant déclenché la vérif. |
| `LastCheckedAt` | `DateTime?` | dernière tentative de vérif (traçabilité / re-vérif). |

- Le token n'est **pas** un secret (il est destiné à être publié en DNS) → stocké en clair, mais **non devinable** (CSPRNG 32 octets base64url) et **invalidé au retrait** (ligne supprimée = token mort).
- **Unicité** : on **conserve** l'index unique global sur `Domain` (déjà présent). Un domaine appartient donc à **un seul tenant**, vérifié ou non → garantit trivialement l'exigence « domaine vérifié = un seul tenant ».
- **Migration** : additive (5 colonnes) après `20260715152004`. **Lignes existantes grandfather** `IsVerified = true`, `VerifiedAt = CreatedAt` (domaines déclarés par un SuperAdmin = déjà de confiance, pour ne pas casser d'éventuelles hypothèses de la passe 2). Aucune modif d'index, aucune ambiguïté de résolution.

---

## 4. Choix de sécurité

1. **Blacklist domaines publics** (refus de déclaration) — liste en dur par défaut + extensible via `appsettings.json` → `DomainVerification:PublicDomainBlacklist`. Défaut : gmail/googlemail, outlook/hotmail/live/msn, yahoo/ymail, orange/wanadoo, free, sfr/neuf/bbox/numericable, laposte, gmx, protonmail/proton/pm.me, icloud/me/mac, aol, zoho, yandex, mail.com, aliceadsl, tiscali, club-internet, etc.
2. **Unicité du domaine** : index unique global (existant) → déclaration refusée `409` si le domaine appartient déjà à un autre tenant. Conflit géré proprement (message explicite, pas de fuite d'info sur l'autre tenant). *Déviation assumée vs spec (« refus si vérifié ailleurs ») : on refuse dès la déclaration — plus strict, cohérent avec le schéma existant, et sans toucher au SSO. Le modèle multi-déclaration (plusieurs pending, 1 seul vérifié) est renvoyé en passe 2 où `TenantResolutionService` sera retravaillé.*
3. **Token** : CSPRNG, unique par (tenant,domaine), invalidé au retrait.
4. **Rate limiting** de la vérification : `IMemoryCache`, clé `domverify_{tenantId}_{domainId}`, N tentatives / fenêtre → `429`. Évite le spam DNS / brute force.
5. **Autorisation serveur** : `[Authorize(Roles="admin,SuperAdmin")]` + `tenantId` **depuis les claims JWT** (`User.GetTenantId()`), jamais du body/query. Toute requête DB filtre `.Where(d => d.TenantId == tenantId)`. Un membre → `403`.
6. **Pas de fallback tenant demo** : rejet si tenant absent.
7. **Journalisation** (`AuditService.LogAsync`) : `domain.declare`, `domain.verify.success`, `domain.verify.failed`, `domain.remove`.
8. **RGPD (4)** : un domaine email n'est pas une donnée personnelle nominative ; on journalise l'admin acteur + horodatage (traçabilité 4.2), pas de donnée sensible. DTO en sortie, aucune entité EF exposée.

---

## 5. Résolution DNS

- Package **`DnsClient`** (NuGet, standard .NET).
- `IDomainVerificationService.VerifyAsync(domain, expectedToken, ct)` : requête **TXT** sur `_sentys-verification.<domain>`, cherche un enregistrement `sentys-verify=<token>`.
- **Timeout court** (5 s) + retries limités. **DNS indisponible / SERVFAIL / timeout ≠ domaine invalide** → résultat distinct `DnsUnavailable` (message « réessayez plus tard »), pas un échec de propriété.
- Résultats : `Verified` | `RecordNotFound` | `TokenMismatch` | `DnsUnavailable`.

---

## 6. Endpoints (`api/tenant/domains`, admin du tenant)

| Verbe | Route | Action |
|---|---|---|
| GET | `/api/tenant/domains` | liste des domaines du tenant (DTO, statut, TXT à poser) |
| POST | `/api/tenant/domains` | déclarer (blacklist, unicité, génère token) → 201 |
| POST | `/api/tenant/domains/{id}/verify` | vérifier (DNS, rate-limit) → statut |
| DELETE | `/api/tenant/domains/{id}` | retirer (token invalidé) |

DTO : `Contracts/DomainDtos.cs` (`TenantDomainDto`, `DeclareDomainRequest`, `VerifyDomainResultDto`). Pas de N+1.

---

## 7. Frontend

Section « Domaines de l'entreprise » dans `admin/entreprise/page.tsx` (design **Vision `--v-*`**) : liste domaine + pill statut (En attente / Vérifié), bloc TXT à poser + bouton Copier (pattern `CopyButton` existant), boutons Vérifier / Retirer, formulaire d'ajout, instructions pédagogiques DNS. 3 états (chargement/données/vide), framer-motion sobre + `prefers-reduced-motion`. Hook React Query `useTenantDomains`. Zéro hex en dur, contraste AA.

---

## 8. Ordre d'exécution

modèle → migration (local) → DTOs → service DNS → controller + garde-fous → enregistrement Program.cs → build back → UI + hook → build front → tests documentés → rapport §9. **STOP** (pas de passe 2/3, pas de push/déploiement).

---

## 9. Tests — RÉSULTATS (11 tests d'intégration HTTP, `CTF.Api.Tests/TenantDomainsEndpointTests.cs`)

Résolution DNS remplacée par un **fake pilotable** (pas de requête réseau) ; blacklist/format/token **réels**.

| Test | Attendu | Résultat |
|---|---|---|
| `Declare_PublicDomain_Is_Refused` (gmail.com) | 400 | ✅ |
| `Declare_InvalidFormat_Is_Refused` | 400 | ✅ |
| `Declare_Then_List_Shows_Pending_With_Txt_Record` | 201 + TXT `_sentys-verification.<d>` / `sentys-verify=…` | ✅ |
| `Declare_Domain_Taken_By_Another_Tenant_Is_Refused` | 409 | ✅ |
| `Verify_Without_Txt_Fails_Cleanly` | 200 `record_not_found`, non vérifié | ✅ |
| `Verify_Wrong_Token_Fails` | 200 `token_mismatch` | ✅ |
| `Verify_Dns_Unavailable_Is_Not_Treated_As_Invalid` | 200 `dns_unavailable` (reste en attente) | ✅ |
| `Verify_Success_Marks_Domain_Verified` | 200 `verified`, statut = verified, TXT masqué | ✅ |
| `Member_NonAdmin_Is_Forbidden_On_All_Operations` (rôle user) | 403 sur GET/POST/verify/DELETE | ✅ |
| `Verify_Is_Rate_Limited_After_Five_Attempts` | 6ᵉ appel → 429 | ✅ |
| `Remove_Deletes_Domain` | 204 + disparu de la liste | ✅ |

**Suite complète : 138/138 ✅** (stable sur 3 runs). Correctif d'isolation apporté (voir §10).

---

## 10. Rapport final (section 5)

### Approche
Fonctionnalité **additive** sur l'entité `TenantEmailDomain` existante (5 colonnes de vérification) + nouveau `TenantDomainsController` (`api/tenant/domains`, admin-de-tenant), service `DomainVerificationService` (DNS TXT via `DnsClient`), section UI « Domaines de l'entreprise » (design Vision). Le CRUD SuperAdmin et le SSO (`TenantResolutionService`) sont **inchangés** — aucune passe 2/3.

### Sécurité (3/6/7)
- **Preuve de possession DNS** : token CSPRNG 32 o (base64url), publié en `_sentys-verification.<domaine>` TXT = `sentys-verify=<token>`. Seul le détenteur du DNS peut vérifier → empêche l'appropriation du domaine d'un hôpital.
- **Blacklist domaines publics** (in-code par défaut + extensible `DomainVerification:PublicDomainBlacklist`) : déclaration refusée pour gmail/outlook/orange/free/etc. et leurs sous-domaines.
- **Unicité** : index unique global sur `Domain` (existant) → un domaine = un seul tenant. *Déviation assumée vs spec (refus dès la déclaration, pas seulement « si vérifié ailleurs ») : plus strict, cohérent avec le schéma existant, sans toucher au SSO. Modèle multi-déclaration → passe 2.*
- **Autorisation serveur** : `[Authorize(Roles="admin,SuperAdmin")]` + `tenantId` depuis les claims JWT (`User.GetTenantId()`), filtrage `.Where(d => d.TenantId == tenantId)` sur chaque requête. Membre non-admin → 403 (testé). Pas de fallback tenant demo.
- **Rate limiting** : `IMemoryCache`, 5 vérifs / 15 min par `(tenant, domaine)` → 429 (testé).
- **Token invalidé au retrait** (ligne supprimée). Token non secret (publié) mais non devinable.
- **Robustesse DNS** : timeout 5 s, `DnsUnavailable` distinct de `RecordNotFound`/`TokenMismatch` (une panne DNS ≠ échec de propriété, réessayable).
- **Journalisation** (`AuditService`) : `domain.declare`, `domain.verify.success`, `domain.verify.failed`, `domain.remove` (traçabilité 4.2).

### RGPD (4)
Un domaine email n'est pas une donnée personnelle nominative. On journalise l'admin acteur + horodatage (audit 4.2). DTO en sortie (`TenantDomainDto`) — aucune entité EF exposée. Aucun champ sensible retourné ; le TXT n'est plus renvoyé une fois le domaine vérifié.

### Charte (Vision / paramètres entreprise)
Section en design system **Vision `--v-*`** (cohérent avec la page existante) : `VisionSection`, pill de statut (Vérifié / En attente), bloc TXT + `CopyChip`, boutons Vérifier/Retirer, formulaire d'ajout, instructions DNS pédagogiques. **3 états** (chargement / vide / données). **Zéro couleur en dur** (tokens + `color-mix`), contraste AA. Animations sobres (framer-motion) + `useReducedMotion` (respect `prefers-reduced-motion`).

### Builds & migration
- Backend `dotnet build` : **0 erreur / 0 warning**. Frontend `npm run build` : **Compiled successfully** (`/admin/entreprise`). Lint des fichiers touchés : clean.
- Migration `20260716014326_AddDomainVerification` **appliquée en LOCAL uniquement** (5 colonnes + grandfather des lignes existantes en `verified`). **Aucun push, aucun déploiement.**

### Notes de mise en œuvre
1. **DTO record + validation** : `[property: Required]` sur un paramètre de record positionnel est ignoré par ASP.NET (exception). Attribut posé directement sur le paramètre.
2. **Isolation des tests** : 4 classes partageaient une base InMemory au nom fixe via `InteractiveTestApiFactory` → contamination croisée sous parallélisme xUnit (flakiness pré-existante). Corrigé : nom de base **unique par instance** de factory → suite verte et stable.

### Prochaines passes (hors périmètre, non implémentées)
- **Passe 2** : rattachement automatique par domaine — brancher `IsVerified` dans `TenantResolutionService` (conditionner l'auto-provisioning à un domaine vérifié) + éventuel modèle multi-déclaration.
- **Passe 3** : SSO Azure AD.
- Éventuelle **re-vérification périodique** (job Hangfire) : un domaine peut être perdu/transféré ; `LastCheckedAt` est déjà en place. Aujourd'hui, re-vérification **manuelle** via le bouton Vérifier.

**STOP** — périmètre PASSE 1 terminé. Pas de passe 2/3, pas de push, pas de déploiement.
