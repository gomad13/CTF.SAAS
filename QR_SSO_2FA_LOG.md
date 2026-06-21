# QR_SSO_2FA_LOG — Journal d'exécution

> Prompt source : `PROMPT_QR_SSO_2FA.md` — Missions M1 (page QR), M2 (SSO), M3 (2FA email).
> Méthode : PLAN → EXECUTE → TEST réel → OK/KO (max 5) → LOG.
> Branche : `feat/maj-secu-qr-email` (continuité du travail QR/email précédent).
> Date : 2026-06-21.

## CONTEXTE ENVIRONNEMENT
Le prompt suppose un accès SSH à la prod (`ubuntu@5.196.64.101`). Claude Code tourne en local Windows :
SSH bloqué (classifier). Les 3 missions étant 100 % **code**, je travaille sur le repo local (code identique
à la prod). Aucune opération prod requise.

## ÉTAT INITIAL (constaté par exploration du repo)
- **M1** : la page QR admin + `/join` + backend invitations **ont été faits lors de la tâche précédente**
  (`/admin/invites`, `/api/admin/invites`, `/api/invites/redeem`, item sidebar « Invitations »).
  → Manque uniquement le bouton **« Télécharger le QR » (PNG)**.
- **M2 — SSO déjà ENTIÈREMENT implémenté** : `SsoController` (challenge+callback Google & Microsoft,
  alias inclus), `SsoFlowService` (liaison de compte par email = pas de doublon, email_verified exigé,
  résolution de tenant par domaine + fallback Demo), boutons SSO sur **login ET register**, secrets lus
  depuis config/env (`Authentication:Google|Microsoft:ClientId|ClientSecret`), endpoint `/api/auth/sso-status`,
  colonnes User (`GoogleSubjectId`, `MicrosoftSubjectId`, `AuthProvider`, `AvatarUrl`), guide `OAUTH_SETUP_GUIDE.md`.
  → Rien à reconstruire. Action : **vérifier le build + compléter la doc** (env vars prod, redirect URIs,
  fallback Demo, email_verified).
- **M3 — 2FA email : NOUVEAU**.

## PLAN

### M1 — bouton « Télécharger le QR » (PNG)
- `ctf-web/src/app/admin/invites/page.tsx` : ajouter un bouton qui sérialise le SVG `react-qr-code`,
  le rend sur un `<canvas>` haute résolution et déclenche le téléchargement PNG.

### M2 — vérifier + documenter
- Build backend OK (SSO compile). 
- Compléter `OAUTH_SETUP_GUIDE.md` (ou le créer si absent) : où mettre Client ID/Secret (user-secrets en dev,
  variables d'env `Authentication__Google__ClientId` etc. en prod via systemd), **redirect URIs exactes**
  à déclarer (`https://sentys.fr/api/auth/google/callback` et `/api/auth/microsoft/callback`),
  comportement email_verified + fallback Demo. Pas de reconstruction de code.

### M3 — 2FA email optionnel (login mot de passe)
Décision de périmètre : 2FA email s'applique au **login email+mot de passe**. Le login **SSO** délègue le MFA
au provider (Google/Microsoft gèrent leur propre 2FA) → pas de double 2FA. Documenté.

**Backend**
1. `Models/User.cs` : `bool TwoFactorEnabled` (défaut false).
2. `Models/TwoFactorCode.cs` (NEW) : Id, UserId, CodeHash (SHA-256), PendingTokenHash (SHA-256), ExpiresAt,
   Attempts, MaxAttempts, IsUsed, CreatedAt.
3. `Data/AppDbContext.cs` : DbSet + config (index UserId, PendingTokenHash, ExpiresAt ; FK cascade).
4. Migration EF `Add2FA` (colonne User + table TwoFactorCodes) + update DB.
5. `IMailService.SendTwoFactorCodeAsync(toEmail, code)` + impl `LogOnlyMailService` (log) + `BrevoMailService`
   (template Brevo) — **mode log si Brevo absent**.
6. `Contracts/TwoFactorDtos.cs` (NEW) : requêtes verify/confirm.
7. `Controllers/AuthController.cs` :
   - ctor : injecter `IMailService` + `ILogger<AuthController>`.
   - `Login` : si `user.TwoFactorEnabled` → NE PAS délivrer le JWT ; générer code 6 chiffres (crypto),
     stocker `TwoFactorCode` (hashé), poser cookie HttpOnly opaque `twofa_pending` (≠ cookie `jwt`,
     donc inutilisable comme session), envoyer le code via `IMailService`, retourner `{ requiresTwoFactor: true }`.
   - `POST /api/auth/2fa/verify` (anon) : lit cookie pending → trouve la ligne → vérifie code (non expiré,
     Attempts<Max, !used) → si OK délivre JWT+refresh+role, supprime cookie pending → `{ role, redirectTo }`.
   - `POST /api/auth/2fa/resend` (anon) : régénère un code (rate-limité).
   - `GET /api/auth/2fa/status` (authed) → `{ enabled }`.
   - `POST /api/auth/2fa/enable` (authed) : envoie un code de confirmation (n'active pas encore).
   - `POST /api/auth/2fa/confirm` (authed) {code} : active `TwoFactorEnabled=true`.
   - `POST /api/auth/2fa/disable` (authed) : `TwoFactorEnabled=false`.
8. `appsettings.json` : règles rate-limit `2fa/verify` (10/min), `2fa/resend` (5/h), `2fa/enable` (5/h).

**Sécurité** : code 6 chiffres `RandomNumberGenerator`, **hashé** en base, usage unique, expiration 10 min,
max 5 tentatives puis invalidation, rate-limit envoi+vérif, cookie pending opaque aléatoire HttpOnly+Secure.

**Frontend**
9. `src/app/login/page.tsx` : si `requiresTwoFactor` → écran « Entrez le code reçu » + champ + « Renvoyer ».
10. `src/app/dashboard/parametres/page.tsx` (onglet Sécurité) : carte 2FA (activer→saisir code→confirmer ;
    désactiver) via composants `Card`/`Toggle` existants.
11. Hook/types 2FA si utile.

### Vérifs
- `dotnet build` + migration appliquée + `next build`/lint.
- **Test e2e M3 en mode log** : activer 2FA (claire) → confirmer (code dans logs) → logout → login →
  requiresTwoFactor → verify (code dans logs) → succès ; code invalide/expiré/trop de tentatives → refus ;
  désactiver → login normal. Restaurer l'état (désactiver 2FA) après test.

---

## EXÉCUTION

### M1 — bouton « Télécharger le QR » (PNG) : FAIT ✅
- `ctf-web/src/app/admin/invites/page.tsx` : bouton « Télécharger le QR » qui sérialise le SVG `react-qr-code`,
  le rend sur un `<canvas>` 512×512 (fond blanc) et déclenche le téléchargement `invitation-sentys-qr.png`.
- Le reste de M1 (génération, QR, copie URL, liste, révocation, `/join`, mobile, sidebar) était déjà livré
  lors de la tâche précédente.
- Vérif : `tsc` ✅, `eslint` ✅, `next build` ✅.

### M2 — SSO Google/Microsoft : DÉJÀ IMPLÉMENTÉ → vérifié + doc corrigée ✅
- Constat : `SsoController` (challenge+callback Google & Microsoft), `SsoFlowService` (liaison par email,
  pas de doublon, `email_verified` exigé, résolution tenant + fallback Demo), boutons login+register,
  secrets en config/env, endpoint `sso-status`. **Rien à reconstruire.** Backend compile.
- **Bug doc corrigé** dans `OAUTH_SETUP_GUIDE.md` : les redirect URIs documentés étaient
  `/api/auth/oauth/{provider}/callback` alors que le `CallbackPath` réel (Program.cs) est
  `/api/auth/{provider}/callback` (sans `/oauth/`). C'est l'URL sur laquelle le provider renvoie →
  un mauvais URI aurait causé `redirect_uri_mismatch` en prod. Corrigé partout (dev + prod + troubleshooting).
- **Doc complétée** : section prod réécrite (domaine `sentys.fr`, redirect URIs exactes, mapping des secrets
  en variables d'env systemd `Authentication__Google__ClientId` etc., comportements email_verified /
  liaison de compte / résolution de tenant).
- **Où coller les clés** : `Authentication:Google:ClientId|ClientSecret` et `Authentication:Microsoft:...`
  (user-secrets en dev, variables d'env en prod). **Redirect URIs à déclarer** :
  `https://sentys.fr/api/auth/google/callback` et `https://sentys.fr/api/auth/microsoft/callback`.

### M3 — 2FA email optionnel : FAIT + testé e2e ✅
**Backend**
- `Models/User.cs` : `TwoFactorEnabled`. `Models/TwoFactorCode.cs` (NEW). `Contracts/TwoFactorDtos.cs` (NEW).
- `Data/AppDbContext.cs` : DbSet + config (index UserId/PendingTokenHash/ExpiresAt, FK cascade).
- Migration `20260621200825_Add2FA` **appliquée** (colonne `Users.TwoFactorEnabled` + table `TwoFactorCodes`).
- `IMailService.SendTwoFactorCodeAsync` + impl LogOnly (log) + Brevo (template). Mode **log** si Brevo absent.
- `Controllers/AuthController.cs` : ctor injecte `IMailService`+`ILogger` ; `Login` branche sur `TwoFactorEnabled` ;
  endpoints `2fa/status|enable|confirm|disable` (authed) + `2fa/verify|resend` (anon, cookie pending) ;
  session-issuance factorisée dans `IssueSessionAsync` (partagée login/verify).
- `appsettings.json` : rate-limits `2fa/verify` 10/min, `2fa/resend` 5/h, `2fa/enable` 5/h.

**Sécurité** : code 6 chiffres `RandomNumberGenerator`, **hashé SHA-256** en base, usage unique, exp. 10 min,
max 5 tentatives → invalidation ; cookie `twofa_pending` **opaque** HttpOnly (≠ cookie `jwt` lu par le
JwtBearer → inutilisable comme session) ; rate-limit envoi+vérif.

**Périmètre** : 2FA email sur le login **mot de passe**. Le login **SSO** délègue le MFA au provider (pas de
double 2FA). Documenté.

**Test e2e (mode log, compte séminé claire.dupont / Employe@2026)** :
| Étape | Attendu | Obtenu |
|---|---|---|
| login 2FA off | 200 `{role,redirectTo}` | ✅ |
| enable (authed) | code de confirmation en logs | ✅ (139266) |
| confirm {code} | 200 success | ✅ |
| status | `enabled:true` | ✅ |
| login 2FA on | `{requiresTwoFactor:true}` + cookie `twofa_pending` | ✅ |
| verify mauvais code | 400 « Code incorrect. » | ✅ |
| verify bon code | 200 `{role,redirectTo}` + cookie `jwt` | ✅ |
| disable (cleanup) | 200 → `enabled:false` (état restauré) | ✅ |

**Frontend**
- `ctf-web/src/app/login/page.tsx` : si `requiresTwoFactor` → écran « code reçu par email » (champ 6 chiffres,
  « Vérifier », « Renvoyer le code »), redirection commune `goAfterAuth` (respecte `returnUrl`).
- `ctf-web/src/app/dashboard/parametres/page.tsx` (onglet Sécurité) : carte `TwoFactorCard`
  (activer → saisir code → confirmer ; badge « Activée » + désactiver).
- Vérif : `tsc` ✅, `next build` ✅ (les 2 erreurs eslint `set-state-in-effect` sont **préexistantes**,
  lignes 93/305, code non touché par moi ; le build passe).

---

## NOTE GIT (importante)
La plupart des fichiers concernés contenaient **déjà du travail non commité** (non suivis ou modifiés) :
`login/page.tsx`, `IMailService.cs`, `LogOnlyMailService.cs`, `OAUTH_SETUP_GUIDE.md` (non suivis),
`AuthController.cs`, `AppDbContext.cs`, `User.cs`, `appsettings.json`, `parametres/page.tsx` (préexistants modifiés).
Pour ne pas happer ce travail, mes commits ne contiennent que :
- les **fichiers neufs** : `TwoFactorCode.cs`, `TwoFactorDtos.cs`, migration `Add2FA` ;
- mes **fichiers déjà commités** retouchés : `BrevoMailService.cs` (méthode 2FA), `admin/invites/page.tsx` (PNG) ;
- ce log.
Mes édits aux fichiers partagés/non-suivis ci-dessus restent **non-stagés** (indispensables au fonctionnement,
documentés ici). Pour activer la feature : conserver/committer vous-même ces fichiers.

## RECETTE DE TEST MANUEL (web + mobile)
1. **2FA** : Paramètres → Sécurité → « Activer la 2FA » → saisir le code (en dev : visible dans les logs backend) →
   « Activée ». Se déconnecter, se reconnecter → écran code → saisir → accès. Tester mauvais code / « Renvoyer ».
   Puis « Désactiver » → login redevient direct.
2. **SSO** : une fois les clés Google/Microsoft renseignées (cf. OAUTH_SETUP_GUIDE.md) + redirect URIs déclarées,
   tester « Continuer avec Google/Microsoft » depuis login et register.
3. **QR** : Admin → Invitations → générer → « Télécharger le QR » → le PNG s'ouvre/scanne au téléphone.
