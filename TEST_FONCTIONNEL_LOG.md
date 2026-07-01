# TEST_FONCTIONNEL_LOG.md

Campagne de test fonctionnel — **priorité absolue au QR code** — puis sweep des autres fonctionnalités.
Serveur : `ubuntu@5.196.64.101` — backend `sentys-backend` (`/home/ubuntu/sentys/backend/`), frontend `sentys-frontend` (`/home/ubuntu/sentys/frontend/`), BDD `ctf_training`. Site : https://sentys.fr.

> ⚠️ Note : le fichier `PROMPT_TEST_FONCTIONNEL_QR.md` demandé n'existait **nulle part** (ni local, ni serveur, ni git) et aucune URL n'était fournie. J'ai exécuté la campagne à partir du brief détaillé collé dans la consigne. Tests **non destructifs**, sur des **comptes de test dédiés** (`*@sentys-test.local`) supprimés en fin de campagne.

---

## [2026-07-01] — Backups (avant tout test/fix)
- DB : `/home/ubuntu/backups/ctf_training_before_testfonc_20260701_133116.dump`
- Code : `/home/ubuntu/backups/sentys_code_before_testfonc_20260701_133116.tgz`
- Publish (avant redéploiement du fix) : republié depuis `CTF. API.csproj`.

---

## 1. QR CODE — test de bout en bout (PRIORITÉ)

Batterie automatisée (`/tmp/qr_test.py`) contre l'API live `127.0.0.1:5202`. Tenants : **A = Prepa Bloc**, **B = CyberMed**, **Demo**.

| # | Scénario | Résultat |
|---|----------|----------|
| T1  | **Génération** QR : `POST /api/admin/invites` → token + `joinUrl` (`https://sentys.fr/join?token=…`) + `maxUses` | ✅ PASS |
| T1b | L'invite **porte le TenantId du créateur** (A) en base | ✅ PASS |
| T2  | `invite-preview(token valide)` → `valid:true` + `tenantName` (pré-remplissage société) | ✅ PASS |
| T3  | `invite-preview(token bidon)` → `valid:false` | ✅ PASS |
| T4  | **Rejoindre — compte existant** : redeem → 200, société A ajoutée (`UserTenant`) | ✅ (voir note*) |
| T4b | **Switch société active** vers la société rejointe (`POST /api/me/active-tenant`) | ✅ PASS |
| T5  | **Cloisonnement** : QR de A + **injection `tenantId=B`** (body + query) → rejoint **A uniquement** | ✅ PASS |
| T6  | **Nouveau compte, société pré-remplie** : `invite-preview` (nom société) → `register` (ouvert) → `redeem` → rejoint A (memberships = Demo + A) | ✅ PASS |
| T7  | **Épuisement** (`maxUses=1`) : 1er redeem OK, 2e refusé (400) | ✅ PASS |
| T7b | `invite-preview(épuisée)` → `valid:false` | ✅ PASS |
| T8  | **Expiration** : redeem d'un QR expiré → 400 | ✅ PASS |
| T8b | `invite-preview(expirée)` → `valid:false` | ✅ PASS |
| T9  | **Révocation** : `DELETE /api/admin/invites/{id}` (204) → redeem refusé (400) | ✅ PASS |
| T9b | `invite-preview(révoquée)` → `valid:false` | ✅ PASS |
| T10 | Redeem **2e fois** (déjà membre) → 400 « Vous faites déjà partie… » | ✅ PASS |
| T11 | Redeem **sans authentification** → 401 | ✅ PASS |

**Score : 15/16 PASS.** (\*) Le seul « FAIL » de la batterie n'était **pas** un défaut du QR : le redeem a bien rattaché la société A. L'assertion échouait car elle attendait aussi la société B — ce qui a révélé le **bug latent ci-dessous**.

**Mobile** : les 3 pages du parcours QR sont mobile-first (primitives responsive présentes : `100svh`/`clamp`/`min-h`/cibles tactiles 44px sur `/join` ; classes `sm:`/`flex-col`/`w-full`/`max-w` sur `/register`, `InvitesManager` et `/admin/entreprise`). Le QR est rendu par `react-qr-code` depuis `joinUrl` et téléchargeable en **PNG** (SVG→canvas). Rendu visuel final à confirmer manuellement sur un vrai mobile.

### 🐞 Bug trouvé & corrigé — `SuperAdmin CreateUser` ne créait pas de `UserTenant`
- **Symptôme** : un utilisateur créé via le panel SuperAdmin (`POST /api/superadmin/users`) avait `user.TenantId` renseigné mais **aucune ligne `UserTenant`**. Or le **sélecteur de sociétés** (`GET /api/me/tenants`) et l'onglet Multi-sociétés lisent `UserTenants` → l'utilisateur apparaissait **sans aucune société** (login OK via fallback `user.TenantId`, mais switcher vide).
- **Impact réel** : 0 utilisateur existant affecté (les comptes réels avaient été rétro-remplis lors du chantier Poitier) → **bug latent** touchant tout futur compte créé par un SuperAdmin.
- **Correctif** : `CreateUser` insère désormais un `UserTenant` (`IsDefault=true`, rôle = rôle demandé), comme le fait déjà `Register`. Fichier `Controllers/SuperAdminController.cs`.
- **Vérif après déploiement** : nouveau compte créé par SuperAdmin → `GET /api/me/tenants` renvoie bien sa société (CyberMed, `isDefault:true`). ✅

---

## 2. Sweep des autres fonctionnalités (comptes réels admin/user + compte de test)

Smoke-test API live (`/tmp/smoke2.py`) — utilisateur régulier (CyberMed) + admin (boujut, Prepa Bloc).

| Domaine | Endpoints testés | Résultat |
|---|---|---|
| **Auth** | `login`, `refresh`, `change-password` (aller-retour), `login(new pwd)`, `logout`, `2fa/status`, `me` | ✅ tous 200 |
| **Multi-sociétés** | `GET /api/me/tenants`, `POST /api/me/active-tenant` (bascule), boujut = 2 sociétés | ✅ switch OK |
| **Parcours / progression** | `GET /api/user/parcours`, `GET /api/paths`, `GET /api/paths/{id}/progress` | ✅ 200 |
| **Compétition** | `status`, `scoreboard`, `leaderboard/individual`, `my-rank` | ✅ 200 |
| **Équipes** | `GET /api/user-teams`, `/api/user-teams/mine`, `GET /api/teams` (admin) | ✅ 200 |
| **Paramètres** | `GET /api/tenant/settings` (admin) | ✅ 200 |
| **Admin** | `GET /api/admin/invites` | ✅ 200 |

- `GET /api/progress` renvoie **404** : **comportement attendu**, l'endpoint exige `?pathId=…` et renvoie 404 s'il n'existe pas encore de ligne de progression (le front l'appelle avec un `pathId` réel). **Pas un bug.**
- **Aucune erreur 5xx** ; logs backend propres après redéploiement.

### Observation (non corrigée — décision documentée)
- **Register — onglet « entreprise » (code d'accès)** : le backend `Register` **ignore volontairement** `req.TenantId` (`[PENTEST]` : empêche un client de se rattacher à un tenant arbitraire) et crée toujours dans **Demo**. Conséquence : saisir un « code entreprise » à l'inscription ne rattache pas à l'entreprise — le rattachement passe **uniquement** par le QR/invite (flux sécurisé et testé ci-dessus). C'est un **choix de sécurité assumé**, pas un bug ; je ne l'ai pas modifié (le corriger réintroduirait un vecteur de self-attachment). À traiter côté UX si l'on veut masquer/clarifier cet onglet — hors périmètre QR.

---

## 3. Déploiement & propreté
- Fix backend compilé (`dotnet build`, 0 erreur), publié (`dotnet publish "CTF. API.csproj" -c Release -o publish`), `sentys-backend` redémarré → `active`, `/api/health` → **200**, `https://sentys.fr/login` → **200**. Frontend inchangé ce cycle (build M1 précédent conservé).
- **Nettoyage** : tous les comptes `*@sentys-test.local` et leurs `UserTenants`/`RefreshTokens`/`UserConsents`/`Assignments`, ainsi que les invitations de test, **supprimés** (vérifié : 0 restant). **Aucune donnée réelle modifiée.**

## Bilan
- ✅ **QR** : génération + PNG, rejoindre (compte existant & nouveau compte à société pré-remplie), sécurité (expiration / épuisement / révocation / cloisonnement A≠B / déjà-membre / non-authentifié) — **15/16 PASS**, le 16e ayant révélé un bug corrigé.
- ✅ **1 bug corrigé** : `UserTenant` manquant à la création SuperAdmin (switcher).
- ✅ **Sweep** : auth, multi-sociétés, parcours, compétition, équipes, paramètres — tous fonctionnels, 0 régression, 0 5xx.
- ✅ **En prod** sur https://sentys.fr. Backups + ce log.
