# MAJ_SECU_QR_LOG — Journal d'exécution

> Prompt source : `PROMPT_MAJ_SECU_QR_EMAIL.md`
> Méthode : PLAN → EXECUTE → TEST → OK/KO → LOG
> Branche : `feat/maj-secu-qr-email` (créée depuis `main`)
> Date de démarrage : 2026-06-21

---

## CONTEXTE & DÉCISIONS D'ENVIRONNEMENT

Le prompt a été écrit pour une exécution **sur / avec accès SSH au serveur de prod** `ubuntu@5.196.64.101` (sentys.fr).
Or Claude Code tourne ici sur la **machine de dev locale Windows**. Conséquences décidées avec l'utilisateur :

- **V1 (MAJ système)** et **V2 (audit serveur)** : opérations SSH sur la prod **bloquées** depuis cette machine
  (classifier de sécurité + actions irréversibles sur une bêta en prod). → L'utilisateur les exécutera lui-même.
  Je fournis des **scripts prêts à l'emploi + checklist** (voir section V1/V2 en bas).
- **V3 (email)** et **V4 (QR invitation)** : code local → exécutés ici, plan→code→test→commit.
- Log local : ce fichier. Commits : un par volet sur la branche `feat/maj-secu-qr-email`
  (rollback local sûr puisque pas de backup GitHub). On ne committe QUE les fichiers de cette tâche
  (le working tree contient des modifs préexistantes non liées, laissées intactes).

---

## PLAN

### V4 — QR code d'invitation entreprise (fait en premier, ordre prompt V4 avant V3)

**Backend**
1. `Models/TenantInvite.cs` — entité (Id, TokenHash, TenantId, ExpiresAt, MaxUses, UsedCount, CreatedByUserId, CreatedAt, IsRevoked).
2. `Data/AppDbContext.cs` — DbSet + config fluent (index sur TokenHash unique, index TenantId).
3. `Contracts/InviteDtos.cs` — CreateInviteRequest, InviteDto (sortie, jamais le token en clair sauf à la création), RedeemInviteRequest.
4. `Controllers/InvitesController.cs` :
   - `POST /api/admin/invites` (admin) — crée, retourne token+joinUrl UNE fois.
   - `GET /api/admin/invites` (admin) — liste invitations actives du tenant.
   - `DELETE /api/admin/invites/{id}` (admin) — révoque.
   - `POST /api/invites/redeem` (authentifié) — valide + rattache user au tenant, role→"user", TeamId→null, UsedCount++.
5. `appsettings.json` — règle rate limit `POST:/api/invites/redeem` 10/min.
6. Migration EF `AddTenantInvite` + `database update`.

**Sécurité** : token 32 octets RandomNumberGenerator → base64url ; SHA-256 hashé en base ; validation 100 % serveur
(exists/non expiré/UsedCount<MaxUses/non révoqué) ; isolation tenant (admin = son tenant via claims) ; rate limit redeem.
Après redeem, le front appelle `/api/auth/refresh` (rebuild JWT depuis user.TenantId) → nouveau tenant_id.

**Frontend**
7. `npm i` lib QR (`react-qr-code`).
8. `src/lib/types/invites.ts` + `src/lib/hooks/useInvites.ts`.
9. `src/app/admin/invites/page.tsx` — générer (durée + maxUses), afficher QR + URL copiable, liste + révoquer.
10. `src/app/join/page.tsx` — public ; si connecté → rejoindre ; sinon → login?returnUrl ; messages clairs (expiré/invalide) ; mobile.

### V3 — Infrastructure email (Brevo + log)

L'abstraction `IMailService` + `LogOnlyMailService` existent déjà ; Program.cs a un stub Brevo (fallback log-only).
11. `Services/BrevoMailService.cs` — appel API HTTP Brevo (clé via config `Mail:BrevoApiKey`, expéditeur `Mail:SenderEmail`/`Mail:SenderName`), persiste MailLog (status sent/failed), jamais de clé en dur.
12. `Program.cs` — brancher BrevoMailService quand `Mail:Provider=Brevo` + clé présente (sinon log-only).
13. `appsettings.json` — section `Mail` documentée (placeholders vides, pas de secret).
14. Doc de branchement Brevo (clé + DNS) dans ce log.

### Vérifs
- `dotnet build` OK, migration appliquée, endpoints testés (curl/Swagger).
- `npm run build` / lint OK.

---

## EXÉCUTION (journal chronologique)

### V4 — QR invitation : FAIT ✅

**Backend** (branche `feat/maj-secu-qr-email`, commit `761db60`)
- `Models/TenantInvite.cs` (NEW), `Contracts/InviteDtos.cs` (NEW), `Controllers/InvitesController.cs` (NEW).
- Migration `20260621184137_AddTenantInvite` créée + **appliquée** (`dotnet ef database update`).
  Table `TenantInvites` vérifiée en base : index UNIQUE sur `TokenHash`, index `(TenantId, IsRevoked)`.
- Édits sur fichiers partagés (laissés **non-stagés**, intriqués avec du travail non commité préexistant) :
  - `Data/AppDbContext.cs` : `DbSet<TenantInvite>` + config fluent.
  - `appsettings.json` : règle rate-limit `POST:/api/invites/redeem` = 10/min.

**Décisions** (R2) :
- Routes `/api/admin/invites` (CRUD admin) + `/api/invites/redeem` (user) — tenant pris depuis les
  claims JWT, **pas** depuis un paramètre de route (isolation, on ne fait pas confiance au client).
  Déviation assumée vs le `/api/tenants/{tenantId}/invites` littéral du prompt.
- Token : 32 octets `RandomNumberGenerator` → base64url ; **SHA-256 hashé** en base ; clair renvoyé 1× à la création.
- Redeem = bascule de tenant : `user.TenantId = invite.TenantId`, `Role = "user"` (pas d'escalade),
  `TeamId = null` ; `UsedCount++`. Le front appelle ensuite `/api/auth/refresh` (reconstruit le JWT
  depuis `user.TenantId`) → nouveau `tenant_id`. Réutilise l'existant, zéro duplication de logique JWT.

**Tests backend (live, curl)** — `dotnet run` sur :5202, compte séminé `claire.dupont` (Employe@2026) :
| Cas | Attendu | Obtenu |
|---|---|---|
| `GET /api/admin/invites` sans auth | 401 | ✅ 401 |
| `POST /api/invites/redeem` sans auth | 401 | ✅ 401 |
| `POST /api/admin/invites` en tant que **user** | 403 | ✅ 403 |
| `GET /api/admin/invites` en tant que **user** | 403 | ✅ 403 |
| `POST /api/invites/redeem` token invalide | 400 + message clair | ✅ 400 « invalide, expirée ou épuisée » |
| 12 redeem en rafale | 429 après 10/min | ✅ 429 |

> Le **happy-path** (admin crée → user d'un autre tenant rejoint → `UsedCount`++ → bascule tenant) a été
> validé **par revue de code** : le setup DB requis (promouvoir un user en admin + insérer un user de test
> avec hash) a été **bloqué par le classifier de sécurité** (mutation d'état séminé partagé). Recette de
> test manuel fournie plus bas.

**Frontend** (commit `a76687e`)
- NEW : `src/app/admin/invites/page.tsx` (génère durée+maxUses, **QR `react-qr-code`** + URL copiable,
  liste, révoquer), `src/app/join/page.tsx` (publique, connecté→rejoindre / sinon→`/login?returnUrl=`,
  messages expiré/invalide, mobile), `src/lib/hooks/useInvites.ts`, `src/lib/types/invites.ts`.
- Édits sur fichiers partagés (**non-stagés**, intriqués avec travail préexistant) :
  - `src/app/login/page.tsx` : prise en charge `returnUrl` (garde anti open-redirect).
  - `src/components/Sidebar.tsx` : item nav « Invitations » + import `QrCode`.
  - `package.json` / `package-lock.json` : dépendance `react-qr-code@^2.2.0`.
- `register` **non modifié** : l'inscription publique est **fermée en bêta** (page « bêta privée »),
  donc la chaîne register→join est sans objet pour l'instant (ajout trivial quand l'inscription rouvrira).
- Vérifs : `tsc --noEmit` ✅, `eslint` (fichiers touchés) ✅, **`next build`** ✅ (routes `/join` et
  `/admin/invites` générées).

### V3 — Email (Brevo + log) : FAIT ✅ (commit `3d9273b`)

- L'abstraction `IMailService` + `LogOnlyMailService` **existaient déjà** ; Program.cs avait un stub Brevo.
- NEW : `Services/BrevoMailService.cs` — API HTTP Brevo (`POST https://api.brevo.com/v3/smtp/email`,
  header `api-key` **jamais logué**), templates sobres charte Sentys (dont **reset mot de passe**),
  trace `MailLog` (status `sent` / `failed:*`) sans la clé.
- Édits partagés (**non-stagés**) : `Program.cs` branche `BrevoMailService` si `Mail:Provider=Brevo`
  + clé présente, sinon `LogOnlyMailService` ; `appsettings.json` section `Mail` (placeholders, **0 secret**).
- `IMailService` n'a pas encore de consommateur → conforme à l'objectif V3 (« **préparer** l'abstraction »).
- Vérif : `dotnet build` ✅ ; backend redémarre proprement avec le nouveau branchement (health 200).

#### 🔌 Brancher Brevo (à faire quand le compte Brevo sera créé)
1. Créer un compte Brevo, valider le domaine `sentys.fr` : ajouter les DNS demandés par Brevo —
   enregistrement **SPF** (`v=spf1 include:spf.brevo.com ...`), **DKIM** (clé fournie par Brevo),
   et idéalement **DMARC** (`_dmarc.sentys.fr` → `v=DMARC1; p=quarantine; ...`).
2. Vérifier que l'expéditeur `noreply@sentys.fr` est validé dans Brevo.
3. Fournir la clé API **en variable d'environnement** (jamais dans le code/git) :
   dans le unit systemd `sentys-backend.service` →
   `Environment=Mail__Provider=Brevo` et `Environment=Mail__BrevoApiKey=xkeysib-...`
   (et au besoin `Mail__SenderEmail`, `Mail__SenderName`). Puis `systemctl restart`.
4. Sans ces variables : mode **log-only** automatique (aucun email réel envoyé).

### V1 / V2 — exécutées par l'utilisateur (prod) : LIVRABLE FOURNI ✅
- Script annoté prêt à l'emploi : **`scripts/prod/maj_secu_v1_v2.sh`** (`bash maj_secu_v1_v2.sh [v1|v2|all]`).
  - V1 : backup BDD+code AVANT, `apt upgrade` (pas dist-upgrade, avec confirmation), `npm audit fix`
    (sans `--force`), `dotnet list package --vulnerable/--outdated`, rebuild+restart+tests, **bloc rollback**.
  - V2 : ufw / fail2ban / `ss` (5432 & 11434 non exposés) / sshd / headers TLS / audit code / secrets / rapport.
- **Constat sécu déjà remonté en local** (vaut pour V2) : `Scriban 7.1.0` = **vulnérabilité HAUTE**
  (NU1903, GHSA-24c8-4792-22hx). À upgrader côté backend (compatible .NET 8).

---

## 🧪 RECETTE DE TEST MANUEL — happy-path QR (à faire avec un vrai compte admin)
1. Se connecter en **admin** d'un tenant → menu **Invitations** → générer (ex. 1h / 5 usages).
2. Le QR + l'URL `https://.../join?token=...` s'affichent ; copier l'URL.
3. Dans une **autre session** (utilisateur d'un AUTRE tenant), ouvrir l'URL → « Rejoindre l'entreprise ».
4. Vérifier : redirection dashboard, l'utilisateur voit le **nouveau** tenant ; en base `UsedCount` passe à 1.
5. Régénérer puis **révoquer** → rejoindre → refus « invalide/expirée/épuisée ».
6. Forcer l'expiration (en base, `ExpiresAt` au passé) → rejoindre → refus.
7. Mobile : scanner le QR au téléphone → la page `/join` doit être nickel.

## ⚠️ NOTE GIT IMPORTANTE
Le working tree contenait **déjà** beaucoup de modifs non commitées (sans lien avec cette tâche) :
`AppDbContext.cs` (+132 l.), `appsettings.json` (+34 l.), `Program.cs`, `Sidebar.tsx` (« Viper »→« Sentys »,
« Mes équipes »), `login/page.tsx` (non suivi), `package.json`, toutes les migrations, le ModelSnapshot…
→ Les commits de cette tâche ne contiennent que **mes fichiers neufs isolables**. Mes édits aux fichiers
partagés ci-dessus sont **laissés non-stagés** pour ne pas happer votre travail ; ils sont indispensables
au fonctionnement (DbSet, rate-limit, branchement mail, returnUrl, nav, dépendance QR) et documentés ici.
Pour activer pleinement la feature : conserver/committer vous-même ces fichiers partagés.
