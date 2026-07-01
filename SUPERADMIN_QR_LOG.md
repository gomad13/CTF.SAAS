# SUPERADMIN_QR_LOG.md

Journal d'exécution — 2 features :
- **M1** : onglet SuperAdmin pour affecter un compte à plusieurs sociétés (réutilise `UserTenant`).
- **M2** : cloisonnement des QR codes / invitations par société (un QR de A ne fait rejoindre que A).

Serveur : `ubuntu@5.196.64.101` — backend `/home/ubuntu/sentys/backend/` (service `sentys-backend`), frontend `/home/ubuntu/sentys/frontend/` (service `sentys-frontend`), BDD `ctf_training`.

---

## [2026-07-01] — Reconnaissance

**État constaté (le multi-sociétés existe déjà sur le serveur, pas dans le repo local) :**
- Entité `Models/UserTenant.cs` : `(UserId, TenantId, Role, IsDefault, JoinedAt)`, index unique `(UserId, TenantId)`. Migration `20260630222115_AddUserTenantMultiSociete` déjà appliquée.
- `AuthController` : société active résolue via `ResolveActiveAsync` (claim `tenant_id`), endpoints `GET /api/me/tenants` et `POST /api/me/active-tenant` (switcher). Un non-SuperAdmin ne peut activer qu'une société dont il est membre.
- `SuperAdminController` (`[Authorize(Roles="SuperAdmin")]`, route `api/superadmin`) : contient déjà tenants/users/licences… mais **aucun** endpoint de gestion des appartenances multi-sociétés d'un user → **c'est ce qu'il faut ajouter (M1)**.
- `InvitesController` : **M2 déjà cloisonné** — `Create` pose `invite.TenantId = User.GetTenantId()` (société active du créateur) ; `Redeem` rattache l'user **uniquement** à `invite.TenantId` (jamais du body/query) via un `UserTenant`. Reste à **prouver** ce cloisonnement par un test réel.

**Décision** : travail effectué directement sur le code serveur (le repo local est en retard : pas de `UserTenant`).

## [2026-07-01] — Backups (avant toute modif)

- DB : `/home/ubuntu/backups/ctf_training_before_superadmin_qr_20260701_065111.dump` (pg_dump -Fc)
- Code : `/home/ubuntu/backups/sentys_code_before_superadmin_qr_20260701_065111.tgz`
- Publish (avant redéploiement) : `/home/ubuntu/backups/publish_bak_before_superadmin_qr_20260701.tgz`

---

## [2026-07-01] — M1 : onglet SuperAdmin « Multi-sociétés » (affectation multi-sociétés)

### Backend (`SuperAdminController.cs`, `[Authorize(Roles="SuperAdmin")]`, réutilise `UserTenant`)
4 endpoints cross-tenant, réservés SuperAdmin :
- `GET  /api/superadmin/users/{userId}/tenants` — liste des sociétés du user (nom, rôle par société, défaut).
- `POST /api/superadmin/users/{userId}/tenants` `{tenantId, role, makeDefault?}` — ajoute une appartenance. Rôle **whitelisté** (`user|admin|owner`). 1re société ⇒ `IsDefault=true`. `409` si déjà membre.
- `PATCH /api/superadmin/users/{userId}/tenants/{tenantId}` `{role}` — change le rôle dans une société.
- `DELETE /api/superadmin/users/{userId}/tenants/{tenantId}` — retire une société ; **refuse de retirer la dernière** ; réassigne le défaut si on retire la société par défaut.
DTOs `AddUserTenantDto` / `UpdateUserTenantRoleDto` (regex de whitelist) dans `DTOs/SuperAdminDtos.cs`. Chaque mutation ⇒ `SuperAdminAuditLog`.

### Frontend (`superadmin/page.tsx`)
Nouvel onglet **« ▸ Multi-sociétés »** : recherche d'un user → affichage de ses sociétés (rôle + badge « défaut ») → ajout d'une société (select société + select rôle) / changement de rôle / retrait. Modals & toasts cohérents avec le thème SuperAdmin.

### Preuve d'affectation multi-sociétés (API réelle, user jetable `testqr@sentys-test.local`)
```
1) GET initial          -> tenants: []                         (aucune société)
2) POST +CyberMed(user)  -> isDefault:true                     (1re société = défaut)
3) POST +Poitier(admin)  -> isDefault:false
4) GET                   -> 2 sociétés : CyberMed(user,défaut) + Poitier(admin)
5) POST +Poitier (dup)   -> HTTP 409                            (index unique respecté)
6) PATCH Poitier->owner  -> role:"owner"
7) POST role="superhacker" -> HTTP 400                          (whitelist rôle)
8) DELETE CyberMed(défaut) -> success ; défaut réassigné à Poitier
9) GET                   -> Poitier(owner, isDefault:true)      (réassignation OK)
10) DELETE Poitier (dernière) -> HTTP 400 "Impossible de retirer la dernière société"
11) GET sans cookie      -> HTTP 401
    GET avec cookie admin (boujut, non-SuperAdmin) -> HTTP 404  (endpoints bien réservés SuperAdmin)
```
➡️ **Un compte peut être affecté à plusieurs sociétés avec un rôle par société**, via l'onglet dédié, réservé SuperAdmin.

---

## [2026-07-01] — M2 : cloisonnement des QR / invitations par société

### État du code (déjà cloisonné — vérifié et prouvé)
- `CreateInviteRequest(ExpiresInHours, MaxUses)` : **aucun `tenantId`**. À la création, `invite.TenantId = User.GetTenantId()` = société active du créateur. Un QR **porte** donc le TenantId de la société où il est généré.
- `RedeemInviteRequest(Token)` : **un seul champ, le token**. Le client **ne peut pas** indiquer quelle société rejoindre. Le redeem rattache l'user **uniquement** à `invite.TenantId` (lu en base), jamais du body/query. Incrément atomique anti-TOCTOU de `UsedCount`. Rattachement = **ajout** d'un `UserTenant` rôle `user`, non-défaut (ne détourne pas la société active, ne donne jamais admin).

### Preuve « le QR de A ne fait rejoindre que A, pas B » (test réel)
- Société **A = Prepa Bloc** (`a84c5c16…`), société **B = CyberMed** (`a0000000…`).
- `boujut@3il.fr` (admin, société active = **A**) crée le QR → en base `TenantInvites.TenantId = a84c5c16…` (**A**), `UsedCount=0`.
- `testqr` (membre de **Poitier** uniquement) appelle le redeem en **injectant `tenantId=CyberMed (B)`** dans le body **ET** dans la query :
  `POST /api/invites/redeem?tenantId=a0000000…  body={"token":"…","tenantId":"a0000000…","TenantId":"a0000000…"}`
- **Réponse** : `{"tenantId":"a84c5c16…","tenantName":"Prepa Bloc"}` → rejoint **A**, l'injection de B est ignorée.
- **Vérif base** après redeem :
```
testqr memberships : Poitier(owner,défaut) + Prepa Bloc(user)   -> A ajouté
member of CyberMed (B) ? : 0                                    -> B JAMAIS rejoint
invite UsedCount : 1/5                                          -> consommé
```
➡️ **Cloisonnement prouvé** : un QR de A ne fait rejoindre que A. La société est dérivée exclusivement de l'invite stockée ; aucune valeur client (body/query) ne peut la changer.

---

## [2026-07-01] — Déploiement & non-régression

- Build backend : `dotnet publish "CTF. API.csproj" -c Release` (⚠️ la **solution** `.sln` référence un projet de tests absent → publier le **.csproj**, pas le .sln). Restart `sentys-backend` → `active`, `/api/health` → **200**.
- Build frontend : `npm run build` OK, label « Multi-sociétés » présent dans le bundle. Restart `sentys-frontend` → `active`. `/superadmin` → 307 (redirection login attendue si non authentifié).
- Non-régression : logins SuperAdmin / admin (boujut) / user (testqr) OK ; sélecteur de sociétés (`/api/me/tenants`) OK ; middleware CGU/consentement OK. Aucune erreur dans les logs.
- **Nettoyage** : user jetable `testqr@sentys-test.local`, ses `UserTenants`/`RefreshTokens`/`UserConsents` et l'invite de test **supprimés** (vérifié : 0 lignes restantes). Aucune donnée réelle modifiée.

### Livrables
1. ✅ Onglet SuperAdmin « Multi-sociétés » (affectation d'un compte à plusieurs sociétés, rôle par société).
2. ✅ Preuve d'affectation multi-sociétés (séquence API ci-dessus).
3. ✅ Preuve que le QR de A ne fait rejoindre que A (injection de B ignorée, DB à l'appui).
4. ✅ En prod sur https://sentys.fr. Backups + ce log.
