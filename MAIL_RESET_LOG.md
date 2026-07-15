# MAIL_RESET_LOG.md — Mot de passe oublié (sécurisé) + Mail support via Brevo

> 100 % LOCAL : pas de push, pas de déploiement, pas de migration prod. **AUCUN email réel à de vrais utilisateurs** (test only).
> Autorité : `Instructions.md` absent → **CLAUDE.md** (qualité §1, charte §2, sécu §3/§6/§7, RGPD §4, rapport §5).

## 0. Backup
- `backups/mail-reset-20260715_032512/` : `back/` (AuthController, User, RefreshToken), `schema.sql`.

## 1. Existant (réutilisé)
- **`BrevoMailService : IMailService`** déjà présent (`Services/BrevoMailService.cs`) :
  - Config : `Mail:BrevoApiKey` (idéalement env `Mail__BrevoApiKey`, jamais commitée), `Mail:SenderEmail`, `Mail:SenderName`. Toggle `Mail:Provider="Brevo"` sinon `LogOnlyMailService` (⇒ **tests sans envoi réel**).
  - Méthodes : `SendPasswordResetAsync(toEmail, resetLink)` (+ template), `SendFeedbackConfirmationAsync`, `SendTwoFactorCodeAsync`, `SendInvitationAsync`… + `MailLog` (statut, sans la clé, sans le token).
  - ⚠️ Template reset : bouton en **bleu #3B82F6** (ancienne charte) + « expire dans 1 heure » → à aligner **violet #7551FF** + durée réelle (15-30 min). (Les hex inline sont normaux/obligatoires en email.)
- **À AJOUTER à `IMailService`/Brevo** : `SendSupportMessageAsync(fromEmail, subject, message)` → vers l'adresse **support configurée** (`Mail:SupportEmail`).

## 2. Modèle du token de reset (choix de sécurité)
- Entité **`PasswordResetToken`** : `Id (Guid)`, `UserId (Guid)`, `TenantId (Guid)`, **`TokenHash (string)`** (SHA-256 du token brut — **jamais le token en clair en base**), `ExpiresAt`, `UsedAt (DateTime?)`, `CreatedAt`, `RequestIp (string?)`.
- **Token brut** = 32 octets via **`RandomNumberGenerator`** (PAS `Random`) → base64url. Envoyé UNIQUEMENT dans le lien email. En base : **hash SHA-256** (comparaison à temps constant).
- **Usage unique** (`UsedAt` posé au reset) + **expiration 30 min** + **invalidation des autres tokens en cours** du user au reset.

## 3. Sécurité (exigences)
- **Pas d'énumération** : `forgot-password` renvoie TOUJOURS le même message neutre (« Si un compte existe, un email a été envoyé »), délai similaire (traitement identique).
- **Rate limiting** : par email ET par IP sur `forgot-password` et `support`. Réutiliser le mécanisme existant _(à confirmer — sous-agent)_.
- **Nouveau mot de passe** : **BCrypt** (convention projet) + **complexité validée serveur** (réutiliser le validateur existant).
- **Révocation** : après reset réussi → invalider **toutes les sessions/JWT existants** du user _(mécanisme à confirmer : SecurityStamp/TokenVersion vs RefreshTokens.RevokedAt — sous-agent)_ + tous les autres tokens de reset.
- **Logs** : jamais de token ni de mot de passe (§3.3).
- **Lien** : HTTPS, domaine configuré (`FrontendUrl`), token usage unique/expirant.
- **Support** : validation + longueur des champs, **sanitisation** (pas d'injection d'en-têtes/HTML dans le mail), pas de relais ouvert (destinataire = support fixe configuré, jamais l'entrée utilisateur).

## 4. Plan (ordre imposé)
1. Service d'envoi : `SendSupportMessageAsync` (Brevo + LogOnly) + config `Mail:SupportEmail`. Template reset → violet + durée.
2. Modèle/migration `PasswordResetToken` (**LOCAL**) + DbSet.
3. Endpoints `AuthController` : `forgot-password`, `reset-password` (+ `reset-password/validate`), avec token hashé/unique/expirant, pas d'énumération, rate limiting, révocation sessions, BCrypt + complexité.
4. Endpoint support (`SupportController` ou Auth) : `contact` — rate limit, validation, sanitisation.
5. Pages front (charte violet) : « Mot de passe oublié », « Nouveau mot de passe », formulaire support.

## 5. Rapport final

### 5.1 Ce qui a été livré (100 % local, aucun push/déploiement)

**Backend — `CTF. API`**
- `Models/PasswordResetToken.cs` : entité (hash SHA-256 seul, usage unique, expiration).
- `Models/User.cs` : `SecurityStamp` (nullable, `[JsonIgnore]`) — empreinte de session pour révocation immédiate des JWT.
- `Data/AppDbContext.cs` : `DbSet<PasswordResetToken>` + index (`TokenHash`, `UserId`).
- `Migrations/20260715014205_AddPasswordResetAndSecurityStamp` : colonne `SecurityStamp` + table `PasswordResetTokens` (appliquée **en local uniquement**).
- `Controllers/AuthController.cs` :
  - `BuildJwt(...)` ajoute le claim `sstamp` (si le compte a un stamp) ; propagé à login/2FA, register, refresh, switch société.
  - `forgot-password` réécrit : token CSPRNG base64url, stocké **hashé** (SHA-256), expiration **30 min**, envoi via `IMailService`, **aucune énumération** (message + délai identiques), rate limit **par email (3) et par IP (10)** /15 min, invalidation des anciens tokens du user.
  - `reset-password` réécrit : lookup par hash, refus si absent/expiré/utilisé, BCrypt (workFactor 12) + complexité serveur, `UsedAt` posé, invalidation des autres tokens, **révocation de tous les refresh tokens**, **rotation du `SecurityStamp`** (invalide tous les JWT), aucun token/mdp en log.
  - `reset-password/validate` (GET) : validité du token pour l'UI (ne révèle rien d'autre).
- `Controllers/SupportController.cs` (nouveau) : `POST /api/support/contact` — validation + longueurs, **anti-injection d'en-têtes (CR/LF)**, rate limit (IP + email, 5/15 min), destinataire **fixe** (`Mail:SupportEmail`, pas de relais ouvert), reply-to = expéditeur.
- `Security/TenantMiddleware.cs` : bypass `/api/support` (formulaire public).
- `Program.cs` : événement JWT `OnTokenValidated` → compare `sstamp` du JWT au `SecurityStamp` en base ; échec si divergence (révocation immédiate). Comptes historiques (`SecurityStamp` null) : non impactés tant que non défini.
- `Services/` : `SendSupportMessageAsync` ajouté à `IMailService` + `BrevoMailService` (contenu **échappé** `HtmlEncode`) + `LogOnlyMailService`. Template reset : bouton **violet `#7551FF`** (charte Vision UI, contraste AA blanc/violet — hex en dur obligatoire en email). `Mail:SupportEmail` ajouté à `appsettings.json` (non secret).

**Frontend — `ctf-web`** (charte réelle = **violet Vision UI** `--accent:#7551FF`, tokens `var(--...)`, jamais de hex en dur ; alignée sur `login/page.tsx`)
- `app/forgot-password/page.tsx` : refonte charte, message neutre, mention **30 min**, plus de `tenantId`, suppression du `DevNotice`/placeholder invisible.
- `app/reset-password/page.tsx` : refonte charte + **validation du token au montage** (`/validate`) → écran « Lien invalide » propre, jauge de robustesse, confirmation, redirection `/login?reset=1`.
- `app/support/page.tsx` (nouveau) : formulaire (email, sujet, message) + compteur, gestion `429`, confirmation visuelle.

### 5.2 Tests de sécurité exécutés (réels, backend lancé, DB locale)

| # | Test | Résultat |
|---|------|----------|
| T1 | Anti-énumération : compte existant vs inexistant → **réponse identique** | ✅ |
| T2 | Token **falsifié** → refusé (400) | ✅ |
| T3 | Reset valide → accepté | ✅ |
| T4 | Token **déjà utilisé** (rejoué) → refusé | ✅ |
| T5 | Login avec **nouveau** mot de passe + `/me` = 200 | ✅ |
| T6 | Token **expiré** (forcé en base) → refusé | ✅ |
| T7 | 2ᵉ reset → **rotation SecurityStamp**, **ancien JWT rejeté immédiatement (401)**, **refresh tokens actifs = 0** | ✅ |
| T8 | **Ancien** mot de passe refusé / **nouveau** accepté | ✅ |
| T9 | Rate limit **par email** : 3 tokens créés puis blocage (prouvé en isolé : calls 4-5 → 200 neutre, 0 token) | ✅ |
| T10 | `/validate` → `false` sur token bidon | ✅ |
| T11 | Support : envoi valide (LogOnly, **aucun email réel**) | ✅ |
| T12 | Support : **injection d'en-tête** (CR/LF sujet) → refusée (400) | ✅ |
| T13 | Support : rate limit → `429` au-delà de 5 | ✅ |
| T14 | **Aucun envoi réel** : tous les `MailLogs` = `logged-only` | ✅ |
| T15 | Token **jamais en clair** en base (colonne = hash SHA-256) | ✅ |

> Note T9 dans la suite complète : le compteur **par IP** (10/15 min, partagé par tous les emails de test depuis localhost) est atteint plus tôt → comportement **attendu et correct** du rate limiting. Le rate limit **par email** a été isolé et validé séparément (3 puis blocage).

### 5.3 Points de sécurité clés
- **Secrets** : `Mail:BrevoApiKey` jamais en dur (env `Mail__BrevoApiKey` / `appsettings.Development.json` **gitignoré** — vérifié). Clé Brevo **absente des logs** (0 occurrence). En test : `Provider=""` → `LogOnlyMailService`, **aucun email réel**.
- **Token** : CSPRNG, **hashé SHA-256** en base (jamais en clair — T15), usage unique, 30 min. Le token brut n'existe que dans le lien ; en prod (`BrevoMailService`) il n'est **ni loggé ni persisté** (le `MailLog` ne stocke que le sujet). En local, `LogOnlyMailService` écrit le lien en console/`MailLog` : **simulation de l'email** (dev), inexistante en prod.
- **Mot de passe / logs** : aucun mot de passe ni token dans les logs applicatifs (0 occurrence). Le controller ne loggue que `user={UserId}`.
- **Révocation** : rotation `SecurityStamp` (JWT) + révocation des refresh tokens → déconnexion **multi-appareils immédiate** (T7).

### 5.4 Écarts vs plan initial (documentés)
- **Charte** : la charte en vigueur est bien le **violet Vision UI** (`globals.css` : bloc « THÈME UNIQUE Vision UI — VIOLET GLOBAL » qui redéfinit `--accent:#7551FF` sur `:root, html, html.dark…` et gagne la cascade sur les anciennes valeurs vertes). Les pages front utilisent les **tokens** (`var(--accent)`…) → violet automatique, comme `login`. Le bouton du template email est en **`#7551FF`** (hex en dur obligatoire en email).
- **Révocation JWT** : choix `SecurityStamp` (claim `sstamp` re-vérifié en base à chaque requête) — justifié car travail local, pas de contrainte de déconnexion de masse au déploiement.
- **Drift DB local** : la DB locale était en retard sur le modèle de la branche (migrations `AddChallengeCompletionDuration`, `AddUserTenantMultiSociete`, `AddInviteType` non appliquées → `RefreshTokens.ActiveTenantId` absente, ce qui cassait `reset-password`). Resynchronisée **en local** de façon idempotente (colonnes/tables additives + historique EF). Backup préalable : `backups/mail-reset-20260715_032512/` (+ `full_predump_*.dump` partiel — RLS sur `assignments`). **Aucune migration prod.**

### 5.5 Vérifications
- Build backend : **OK** (0 erreur). Build frontend : **OK** (toutes routes, incl. `/support`, `/forgot-password`, `/reset-password` ; lint via `next build`).
- Endpoints exercés réellement (curl + inspection psql), logs runtime lus.
- **Aucun push, aucun déploiement, aucune migration prod.** Modifications non commitées (commit laissé à l'appréciation de l'utilisateur).

> ⚠️ Données de démo locales : lors des tests, le mot de passe du compte démo `j.marchand@cybermed-innovations.fr` a été réinitialisé (dernière valeur de test `N3w!Password2`). Sans impact prod (DB locale).
