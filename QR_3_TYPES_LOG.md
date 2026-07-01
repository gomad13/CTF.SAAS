# QR_3_TYPES_LOG.md

Refonte du système de QR code en **3 types distincts**, sécurisé, avec isolation tenant stricte.
Serveur `ubuntu@5.196.64.101` — backend `/home/ubuntu/sentys/backend/` (`sentys-backend`), frontend `/home/ubuntu/sentys/frontend/` (`sentys-frontend`), BDD `ctf_training`. Site https://sentys.fr.

---

## [2026-07-01] — Backups (avant modif)
- DB : `/home/ubuntu/backups/ctf_training_before_qr3types_20260701_171604.dump`
- Code : `/home/ubuntu/backups/sentys_code_before_qr3types_20260701_171604.tgz`

## Choix de conception (reporting §1 & §2)

**Distinction des 3 types** : colonne **`InviteType`** ajoutée à `TenantInvite` (`app` | `enterprise_signup` | `enterprise_join`), + **`TenantId` rendu nullable** (NULL pour le Type 1). Le type est whitelisté serveur ; le tenant d'une invitation entreprise vient **toujours** du token stocké (jamais du body/query).

| Type | InviteType | TenantId | URL encodée | Généré par |
|---|---|---|---|---|
| **1 — Application** | `app` | NULL | `…/register` (inscription générale) | **SuperAdmin** uniquement |
| **2 — Entreprise / Inscription** | `enterprise_signup` | société active (scellé) | `…/join?token=` | Admin/Owner (sa société) |
| **3 — Entreprise / Rejoindre** | `enterprise_join` | société active (scellé) | `…/join?token=` | Admin/Owner (sa société) |

**Type 2 vs Type 3 — choix : un seul lien `/join?token=` « intelligent »** (recommandation du prompt). La page `/join` détecte l'état d'authentification et bascule : non connecté → propose connexion **ou** inscription pré-remplie (`/register?token=`, société verrouillée via `invite-preview`) ; connecté → « Rejoindre l'entreprise X ? » puis rattachement. L'`InviteType` distingue l'**intention** de l'admin (étiquetage/liste) ; le token entreprise reste scellé au tenant quel que soit le cas. Rôle attribué au rattachement = **User** (jamais admin via QR).

## Backend (réutilise l'existant, ne duplique pas)
- `Models/TenantInvite.cs` : `InviteType` (défaut `enterprise_join`) + `TenantId` nullable + `InviteTypes` (constantes/whitelist).
- Migration `20260701172204_AddInviteType` : `TenantId` → nullable, `InviteType` NOT NULL default `enterprise_join` (les 3 invitations existantes rétro-classées `enterprise_join`).
- `Controllers/InvitesController.cs` :
  - `POST /api/admin/invites` (`type` requis, whitelisté) : **App** → SuperAdmin obligatoire, `TenantId=null`, URL `/register` ; **Entreprise** → `TenantId` = société active de l'admin, contrôle « gère cette société » (UserTenant admin/owner ; SuperAdmin exempté), URL `/join?token=`.
  - `GET /api/admin/invites` : invitations de la société courante (+ invitations `app` pour le SuperAdmin) ; renvoie `type`, `tenantId`, `tenantName`.
  - `DELETE /api/admin/invites/{id}` : admin = sa société uniquement ; SuperAdmin = toutes.
  - `POST /api/invites/redeem` : refuse les tokens **App** ; sinon rattache **exclusivement** à `invite.TenantId` (cloisonnement), incrément atomique anti-TOCTOU, `UserTenant` rôle User.
  - `GET /api/auth/invite-preview` : renvoie `type` + `tenantName` (verrouillage société côté inscription).
- **Rate limiting** déjà en place sur `POST /api/invites/redeem` (10/min) + `POST /api/auth/register` (5/h). Token = 32 octets aléatoires, stocké **hashé SHA-256**.

## Frontend
- `components/invites/InvitesManager.tsx` : **sélecteur de type** en cartes (App visible **uniquement** pour le SuperAdmin via `/api/auth/me`), libellés/aides clairs, badge de type sur le QR généré et **colonne Type** dans la liste, QR téléchargeable PNG + URL copiable + révocation. Responsive (`grid sm:grid-cols-3`, `overflow-x-auto`).
- `lib/types/invites.ts` : `InviteType` + champs `type`/`tenantId`/`tenantName`.
- `/join` et `/register` : inchangés — gèrent déjà connecté/non-connecté et le verrouillage société (aucune régression).

## Tests réels de bout en bout (13/13 PASS)

| # | Scénario | Résultat |
|---|----------|----------|
| T1 | **Type 1** génération (SuperAdmin) → `joinUrl=/register`, invite sans tenant (NULL) | ✅ |
| T1c | Type 1 : inscription générale → compte **sans entreprise** (Demo uniquement) | ✅ |
| T1d | Redeem d'un token App → **refusé** (mène à l'inscription générale) | ✅ |
| T2 | **Type 2** génération + `invite-preview` verrouille « Prepa Bloc » (type `enterprise_signup`) | ✅ |
| T2b | Type 2 : nouveau compte + société pré-remplie → **rattaché à Prepa Bloc (rôle user)** (Demo + A) | ✅ |
| T3 | **Type 3** : compte existant (CyberMed) **rejoint Prepa Bloc en User**, multi-sociétés préservé (B + A) | ✅ |
| CLO | **Cloisonnement** : QR de A + **injection `tenantId=B`** (body+query) → rejoint **A uniquement** | ✅ |
| SEC1 | App par admin non-SuperAdmin → **403** | ✅ |
| SEC2 | Token entreprise **expiré** → 400 | ✅ |
| SEC3 | **MaxUses=1** : 1er OK, 2e épuisé (400) | ✅ |
| SEC4 | **Révocation** → redeem refusé (400) | ✅ |
| SEC5 | Redeem **sans authentification** → 401 | ✅ |

## Sécurité vérifiée
- Token 32 octets aléatoires, **hashé** en base ; validation 100 % serveur ; expiration + maxUses + révocation appliqués serveur ; **rate limiting** sur redeem (10/min).
- **Cloisonnement strict** : le tenant provient exclusivement du token stocké ; injection body/query ignorée (CLO).
- **Isolation** : un admin ne crée/liste/révoque que pour sa société active ; App réservé SuperAdmin.

## Déploiement & propreté
- `dotnet build` 0 erreur, migration appliquée, `dotnet publish` + restart `sentys-backend` (health 200). `npm run build` OK + restart `sentys-frontend` (`/join`, `/register` 200, site 200).
- Comptes de test (`*@sentys-test.local`) et invitations de test **supprimés** (0 restant) ; seules les 3 invitations pré-existantes demeurent. Aucune donnée réelle modifiée. Patch robustesse Ollama (tâche précédente) intact ; Ollama toujours `127.0.0.1` only.

## Reporting final
1. **Distinction/génération** : `InviteType` (app / enterprise_signup / enterprise_join) + `TenantId` nullable ; App = SuperAdmin, Entreprise = admin de la société active (scellé au tenant).
2. **Type 2 vs 3** : **un seul QR entreprise intelligent** (`/join?token=`) — la page bascule inscription pré-remplie/verrouillée (nouveau) ou rejoindre (existant). Simplicité admin + robustesse.
3. **Preuves** : 13/13 tests e2e ci-dessus, dont cloisonnement A≠B prouvé par injection.
4. **À tester manuellement** : scan réel des 3 QR sur téléphone (rendu mobile déjà responsive).
