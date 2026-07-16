# AUDIT_SECURITE_LOG.md

> **Audit de sécurité complet — avant ouverture à de vrais clients (santé / finance).**
> Date : 2026-07-15 · Branche : `feat/maj-secu-qr-email` · HEAD au démarrage : voir `.audit-backup/HEAD-*.txt`
> **Nature : DIAGNOSTIC UNIQUEMENT. Aucun fichier de code n'a été modifié. Aucune correction appliquée.**
> Travail 100 % local.

---

## 0. Cadre & méthode

- **Fichiers d'instructions demandés absents** : `PROMPT_AUDIT_SECURITE.md` et `Instructions.md` n'existent pas dans le repo. L'audit a été mené sur la **spécification inline fournie** (checklist A–H, 24 points, tests obligatoires, barème de gravité, format de sortie) — équivalente en contenu. `TaskForLesson.md` a été lu au préalable (règles CORS/PATCH, Ollama 127.0.0.1, prod parfois en avance sur le repo).
- **Backup** créé avant toute chose : `.audit-backup/backup-full-20260715-160137.bundle` (`git bundle --all`) + `HEAD-*.txt`. L'audit étant en lecture seule, le working tree est intact.
- **Structure** : backend = dossier `CTF. API/` (avec un espace dans le nom — chemin git réel), frontend = `ctf-web/`. `backend/` et `frontend/` sont des alias pointant sur le même contenu.
- **Méthode** : 5 investigations parallèles (sous-agents) sur périmètres indépendants, puis **vérification directe par relecture du code** des 3 constats les plus graves (historique git, `LogOnlyMailService`, `ChangePassword`).
- **Barème** : CRITIQUE (exploitable, impact majeur) / ÉLEVÉ (exploitable sous conditions ou faille sérieuse) / MOYEN (défense en profondeur manquante) / FAIBLE (durcissement) / INFO (conforme).

---

## 1. Synthèse par gravité

| Gravité | Nb | Points |
|---|---|---|
| **CRITIQUE** | **2** | C-01 mot de passe Postgres dans l'historique git · C-02 clé JWT dans l'historique git |
| **ÉLEVÉ** | **4** | E-01 tokens reset + codes 2FA + mots de passe temporaires en clair (logs + DB) via `LogOnlyMailService` actif · E-02 `ChangePassword` ne révoque pas les sessions · E-03 clé Brevo en clair sur disque · E-04 Next.js 16.1.6 (4 CVE High) |
| **MOYEN** | **2** | M-01 pas de rate limiting sur `reset-password`/`validate` · M-02 5 CVE moderate frontend |
| **FAIBLE** | **5** | F-01 side-channel temporel forgot-password · F-02 token reset en query string · F-03 cookie SameSite=Lax à valider · F-04 unicité SuperAdmin non contrainte · F-05 rate-limit en MemoryCache (mono-instance) |
| **INFO / OK** | 25+ | Autorisation admin, isolation multi-tenant, injection mail support, CORS, cookies, headers, DTO, `[JsonIgnore]`, hashing, Ollama, backend .NET 0 CVE… |

**Verdict global** : le cœur applicatif est **solide** — isolation tenant stricte (JWT + RLS PostgreSQL), gardes de rôle serveur sur tous les endpoints admin, vue nominative / classement complet réservés admin, flux de reset cryptographiquement correct, aucune fuite CVE côté backend. Les points bloquants pour un onboarding santé/finance sont **concentrés sur 2 axes** : (1) **secrets historiques à faire tourner**, (2) **le mode mail log-only qui transforme la table `MailLogs` en coffre de tokens d'authentification en clair**.

---

## 2. LISTE PRIORISÉE — à traiter avant tout onboarding client

> **Ordre recommandé d'exécution. Corrections PROPOSÉES uniquement — rien n'est appliqué.**

### 🔴 CRITIQUE

#### C-01 — Mot de passe PostgreSQL (superuser `postgres`) dans l'historique git
- **Constat (vérifié)** : la chaîne de connexion complète `Username=postgres;Password=<réel>` a été committée dans `ae503a2` ("Premier commit"), retirée du suivi en `e06672f`. **Absente du HEAD** (`git ls-files` ne liste plus `appsettings.Development.json`) mais **récupérable dans l'historique** (`git show "ae503a2:CTF. API/appsettings.Development.json"`). Compte **superuser** → contrôle total de la base.
- **Fichier** : `CTF. API/appsettings.Development.json` (commit `ae503a2`) + copie de build `CTF. API/bin/Debug/net8.0/appsettings.Development.json`.
- **Gravité** : **CRITIQUE** (accès total DB si le secret n'a pas été changé et si l'historique fuite).
- **Correction proposée** :
  1. **Rotation immédiate** du mot de passe Postgres.
  2. Créer un **user applicatif dédié sans privilèges DDL** (pas `postgres`) — comme prévu dans `.env.example`.
  3. Si le repo est ou deviendra partagé (client, prestataire, remote) : **purge d'historique** via `git filter-repo` / BFG, puis force-push coordonné.

#### C-02 — Clé de signature JWT réelle dans l'historique git
- **Constat (vérifié)** : `Jwt:Key = "ctf_dev_jwt_secret_key_…"` committée dans le **même** `ae503a2`, retirée en `e06672f`, absente du HEAD mais dans l'historique. Une clé JWT connue = **forge de jetons arbitraires** (usurpation de n'importe quel user/rôle, y compris admin/SuperAdmin) si cette clé est (ou a été) utilisée en prod.
- **Fichier** : `CTF. API/appsettings.Development.json` (commit `ae503a2`).
- **Gravité** : **CRITIQUE**.
- **Correction proposée** :
  1. **Rotation de la clé JWT** (invalide tous les jetons existants — comportement attendu).
  2. **Vérifier que la clé de PROD n'a jamais été cette valeur** (en prod elle est lue via env `JWT_KEY` — bon). Si elle l'a été : rotation prod obligatoire.
  3. Purge d'historique (même opération que C-01).

### 🟠 ÉLEVÉ

#### E-01 — `LogOnlyMailService` persiste tokens de reset, codes 2FA et mots de passe temporaires **en clair** (logs applicatifs **+ table `MailLogs`**)
- **Constat (vérifié par relecture)** : `LogOnlyMailService` écrit dans `ILogger.LogInformation` **et** persiste dans `MailLogs.Body` :
  - le **lien de reset complet avec token brut** — `LogOnlyMailService.cs:26-28`
  - le **code 2FA en clair** — `LogOnlyMailService.cs:38-40`
  - le **mot de passe temporaire d'invitation** — `LogOnlyMailService.cs:22-24`
  - persistance commune : `LogOnlyMailService.cs:46-61` (`_logger` L48 + `MailLogs.Add(... Body ...)` L52-60).
- **Chemin ACTIF en production** : `Program.cs:261-277` sélectionne `LogOnlyMailService` dès que `Mail:Provider != "Brevo"` **ou** `Mail:BrevoApiKey` absente. Le commentaire `Program.cs:260` le confirme : *« Pour la bêta DSI tant que Brevo n'est pas branché, on reste sur log-only »*. La mémoire projet confirme « Brevo not yet configured ». → **c'est le service réellement utilisé**.
- **Impact** : quiconque a accès aux logs serveur **ou** à un simple `SELECT * FROM "MailLogs"` peut **rejouer un reset de mot de passe (30 min), un code 2FA (10 min, contourne la 2FA)** ou lire un **mot de passe temporaire** → **prise de contrôle de compte**, y compris admin. Le commentaire `AuthController.cs:371` (« jamais de token en log ») est **factuellement faux** sur ce chemin.
- **Gravité** : **ÉLEVÉ** (nécessite un accès logs/DB privilégié, mais expose des secrets d'authentification de grade « account takeover » — **rédhibitoire pour santé/finance**). À traiter **avant** tout onboarding.
- **Correction proposée** :
  1. Dans `LogOnlyMailService`, ne **jamais** mettre `resetLink` / `code` / `tempPassword` dans le body loggué **ni** persisté → logguer seulement `type` + `to` (+ `status`). (Le chemin Brevo, lui, est déjà propre : ne stocke que le sujet.)
  2. **Basculer Brevo en prod** (clé en variable d'env, cf. E-03) pour ne plus dépendre du mode log-only.
  3. Purger / restreindre l'accès à la table `MailLogs` existante (peut déjà contenir des tokens).

#### E-02 — `ChangePassword` ne révoque pas les sessions existantes
- **Constat (vérifié par relecture)** : `AuthController.cs:492-512` — `ChangePassword` vérifie l'ancien mot de passe, valide la robustesse, re-hash BCrypt (workFactor 12) puis `SaveChanges`. **Aucune rotation de `SecurityStamp`, aucune révocation des refresh tokens** (L509-511). Or le `SecurityStamp` est justement revérifié à chaque requête (`Program.cs:146-158`) et le flux **reset**, lui, fait bien les deux (`AuthController.cs:421` + `:431-438`).
- **Impact** : un utilisateur qui change son mot de passe *parce qu'il se sait compromis* **ne déconnecte pas l'attaquant** : les JWT courants restent valides (≤ 15 min) et surtout les **refresh tokens restent valides 7 jours** → l'attaquant continue de renouveler sa session.
- **Gravité** : **ÉLEVÉ**.
- **Correction proposée** : dans `ChangePassword`, répliquer la logique du reset — **rotation `SecurityStamp` + révocation des refresh tokens actifs** de l'utilisateur (en ré-émettant éventuellement la session courante pour ne pas déconnecter l'auteur du changement).

#### E-03 — Clé API Brevo réelle en clair sur le disque
- **Constat (vérifié)** : une **vraie clé Brevo** (`xkeysib-…`) est en clair dans `CTF. API/appsettings.Development.json:58`, sous `Coaching:Brevo:ApiKey`. **Non committée** (recherche exhaustive dans tout l'historique = 0 résultat) et actuellement gitignorée (`.gitignore` → `appsettings.*.json`). **Mais** : (a) ce fichier a **déjà été tracké par le passé** (`ae503a2`/`e06672f`) → fort risque de re-commit accidentel ; (b) **incohérence de clé de config** : le code lit `Mail:BrevoApiKey` (`BrevoMailService.cs:35`, `Program.cs:263`) alors que la vraie clé est rangée sous `Coaching:Brevo:ApiKey` → elle n'est probablement même pas utilisée là où elle est posée, mais reste exposée sur disque.
- **Gravité** : **ÉLEVÉ**.
- **Correction proposée** :
  1. **Rotation de la clé Brevo** par précaution.
  2. La sortir du fichier disque → **user-secrets** (dev) / variable d'env `Mail__BrevoApiKey` (prod), sous la clé effectivement lue par le code (`Mail:BrevoApiKey`).

#### E-04 — Next.js 16.1.6 : 4 CVE High (dont bypass middleware/proxy, CSRF Server Actions, XSS, SSRF)
- **Constat** : `ctf-web` utilise `next@16.1.6` (`package.json`), dans la plage vulnérable. Cumule ~20 advisories, dont exploitables à distance sur une app App Router (cas présent) : bypass middleware/proxy (GHSA-26hh-7cqf-hhc6, GHSA-492v-c6pp-mqqv), **CSRF Server Actions via origin `null`** (GHSA-mq59-m269-xvcx — pertinent, l'app pose un cookie de session), XSS App Router (GHSA-ffhc-5mcf-pf4q), SSRF WebSocket (GHSA-c4j6-fc7j-m34r), HTTP request smuggling (GHSA-ggv3-7p47-pfv8), plusieurs DoS.
- **Gravité** : **ÉLEVÉ**.
- **Correction proposée** : bump `next 16.1.6 → 16.2.10` (ou 16.3+) — `npm audit fix --force` puis **re-test `npm run build`** (changement de version mineure). Corrige aussi le postcss moderate imbriqué.

---

## 3. Détail par section (A–H)

### A — Auth / autorisation

#### A.1 — Reset de mot de passe (PRIORITÉ ABSOLUE) — **globalement conforme**
Cœur du flux **correctement implémenté** (vérifié) :
- **[OK] Token hashé au repos** : seul le SHA-256 (`TokenHash`, `[JsonIgnore]`, indexé) est stocké — `Models/PasswordResetToken.cs:16-18` ; écriture `AuthController.cs:362` ; lecture par hash `:384-385`, `:409-410`.
- **[OK] Unique & imprévisible** : 32 octets (256 bits) via `RandomNumberGenerator.Fill` (CSPRNG), base64url — `AuthController.cs:808-813`. Aucun `Random()`.
- **[OK] Usage unique** : `UsedAt` posé au reset (`:425`), tout token consommé refusé (`:411`), anciens tokens invalidés à chaque nouvelle demande (`:353-354`).
- **[OK] Expiration** : `ExpiresAt = UtcNow + 30 min` (`:363`), vérifiée serveur (`:385`, `:411`).
- **[OK] Pas d'énumération (message/HTTP)** : `forgot-password` renvoie toujours `200` + message neutre identique, y compris si rate-limité (pas de `429` révélateur) — `AuthController.cs:338-339, 375-376`.
- **[OK] Rate limiting sur la demande** : par email (3/15 min) + par IP (10/15 min) — `:344-345`, `IsResetRateLimited :817-827`.
- **[OK] Révocation après reset** : rotation `SecurityStamp` (`:421`) + révocation de tous les refresh tokens (`:431-438`), `SecurityStamp` revérifié à chaque requête (`Program.cs:146-158`).
- **[OK] Robustesse & hash du nouveau mdp** : `IsPasswordStrong` (≥8, maj/min/chiffre/spécial) `:401,842-848` ; BCrypt workFactor 12 `:419`.

Écarts (voir liste priorisée) : **E-02** (change-password), **E-01** (token en clair via log-only), **M-01** (rate-limit validation), **F-01/F-02**.

#### A.2 — Autorisation admin (rôle serveur) — **conforme (0 CRITIQUE / 0 ÉLEVÉ)**
- **[OK]** Tous les controllers admin portent une garde de rôle **serveur** : `[Authorize(Roles = "admin,SuperAdmin")]` (Analytics, EnterpriseAnalytics, TenantSettings, AdminDirectory, AdminScenarios, AdminCatalog, AdminTeams, Compliance, Campaigns, AdminModes, Invites, AdminCompetition) ; `[Authorize(Roles = "SuperAdmin")]` + double-check `IsSuperAdmin()` sur les controllers SuperAdmin. Aucun endpoint admin protégé par `[Authorize]` seul.
- **[OK]** Rôle `SuperAdmin` **re-dérivé serveur** depuis la table `SuperAdmins` (email actif) à chaque login/refresh/switch — `AuthController.cs:138,473,535,947`. Jamais depuis le client.
- **[OK]** Auto-promotion SuperAdmin **bloquée** partout par whitelist `user|admin` — `AdminDirectoryController.cs:253-255`, `SuperAdminController.cs:360-361` ; aucun endpoint ne crée/liste/retire un SuperAdmin (garde-fou explicite `SuperAdminController.cs:1278`).

### B — Injection / validation — **conforme**
- **[OK] Mail support** (route publique `/support`) : validation email (regex + ≤254), sujet 3–150, message 10–5000, **rejet CR/LF** (`HasHeaderInjection`) sur email + sujet → pas d'injection d'en-tête — `SupportController.cs:49-51,64-65`. Destinataire **fixe** (`Mail:SupportEmail`), l'email user sert seulement de `replyTo` (pas d'open relay) — `BrevoMailService.cs:81-82`. Corps **HTML-échappé** (`WebUtility.HtmlEncode`) `:169-176`. **Rate limiting** IP + email 5/15 min → 429 `:53-55,68-78`.
- **[OK] SQL** : EF paramétré partout. Unique SQL non-LINQ = `ExecuteSqlInterpolatedAsync($"SELECT set_app_tenant({tenantFromJwt})")` avec `tenantFromJwt` = `Guid` déjà parsé, passé **en paramètre** — `TenantMiddleware.cs:64-66`. Aucun `FromSqlRaw`/`ExecuteSqlRaw` avec entrée user.
- **[OK] Whitelist enum-like** : `NormalizeChallengeType` (switch + fallback) `CoachingService.cs:248-256` ; rôles via policies `Program.cs:164-165`.

### C — Exposition de données — **conforme sauf E-01**
- **[OK] DTO** : endpoints revus renvoient des `record …Dto` / objets anonymes, aucune entité EF renvoyée directement (Competition → `ScoreboardEntryDto`/`AdminLeaderboardDto`, Coaching → `CoachingFeedbackDto`, etc.).
- **[OK] Champs sensibles `[JsonIgnore]`** : `User.PasswordHash`/`SecurityStamp`, `Challenge.CorrectAnswer/ContentJson/VariantsJson`, `PasswordResetToken.TokenHash`, `RefreshToken.Token`, hash 2FA, hash invite — tous `[JsonIgnore]`, stockés hashés.
- **[OK] Middleware d'exception global EN PREMIER** : `app.UseMiddleware<ExceptionMiddleware>()` première entrée du pipeline `Program.cs:281-282` → masque les stack traces en prod. Swagger DEV-only.
- **[PROBLÈME] Logs** : `LogOnlyMailService` (E-01) — seul point noir de la section.

### D — HTTP / CORS / cookies — **conforme**
- **[OK] CORS** : `WithOrigins(config)` (fallback `localhost:3000`), **jamais `AllowAnyOrigin`**, `AllowCredentials()`, méthodes `GET, POST, PUT, PATCH, DELETE, OPTIONS` — **PATCH présent** (conforme à la règle `TaskForLesson`) — `Program.cs:43-57`.
- **[OK] Cookies** : `jwt` HttpOnly, `Secure=!IsDevelopment()` (Secure en prod), `SameSite=Lax`, expire 15 min, posé serveur uniquement — `AuthController.cs:1007-1017` ; `refresh_token` idem HttpOnly `:1033-1042`. `user_role` non-HttpOnly par design (non secret, lu par le middleware Next).
- **[OK] Headers sécu** : `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`, HSTS (prod) — `Program.cs:325-349`.
- **[OK] Bypass TenantMiddleware borné** : `/api/health`, `/api/auth`, `/api/test` (DEBUG-only), `/api/legal`, `/api/support`, `/api/feedback` (POST) — `TenantMiddleware.cs:27-38`. Rien de trop large.
- Point d'attention : **F-03** (SameSite=Lax) selon topologie de déploiement front/API.

### E — Secrets — **2 CRITIQUE + 1 ÉLEVÉ** (voir liste priorisée : C-01, C-02, E-03)
- **[OK]** `appsettings.json` (tracké, HEAD) : propre — `Mail:BrevoApiKey=""`, pas de mdp DB, pas de clé JWT.
- **[OK]** Lecture des secrets propre : Brevo `config["Mail:BrevoApiKey"]`, JWT `Environment.GetEnvironmentVariable("JWT_KEY")` + fallback, DB `GetConnectionString`. **Aucun secret hardcodé dans le `.cs`.**
- **[OK]** `.gitignore` complet : `appsettings.Development.json`, `appsettings.Production.json`, `appsettings.*.json` + `!appsettings.json`, `.env`/`.env.*` + `!.env.example`, `*.user`, `secrets.json`, `bin/`/`obj/`. Rien ne manque.
- **[OK]** Aucun `.env` réel sur disque ni tracké ; `.env.example` = placeholders. Aucun `ClientSecret` OAuth réel dans l'historique. Les occurrences `xkeysib` dans `docs/`, `scripts/`, `prompt-*.md` sont des placeholders.

### F — Multi-tenant — **conforme (0 CRITIQUE / 0 ÉLEVÉ)**
- **[OK] TenantId 100 % serveur** : `TenantMiddleware` lit le tenant **exclusivement** depuis le claim JWT `tenant_id`, refuse `400` si absent, + **RLS PostgreSQL** (`set_app_tenant`) — `TenantMiddleware.cs:49-66`. Aucun endpoint en scope n'accepte un tenantId client. (Les endpoints `SuperAdmin*` prennent un `tenantId` en route **par conception** — cross-tenant, rôle SuperAdmin only.)
- **[OK] Filtrage `.Where(TenantId==)`** partout ; validation d'ownership sur les IDs de route (`teamId`/`userId` → 404 si autre tenant) — `EnterpriseAnalyticsController.cs:44-52,257-282`, `AdminDirectoryController.cs:305-306`.
- **[OK] Pas de fallback tenant démo silencieux** : Analytics/Competition/Directory rejettent `Guid.Empty` (Unauthorized). Le GUID démo `0000…` sert **uniquement** de catalogue de contenu pédagogique partagé en lecture (hors scope, pas de PII cross-tenant). Inscription vers `DemoTenantId` explicite et documentée.
- **[OK] Vue nominative & classement complet réservés admin** : instances de scénario nominatives (`AdminScenariosController` `[admin,SuperAdmin]`), profils individuels (`EnterpriseAnalyticsController` `[admin,SuperAdmin]`) ; **classement public = top 5 anonymisé** (`CompetitionController :37-109`), **classement nominatif complet 1..N** isolé dans `AdminCompetitionController.GetFullLeaderboard` `[admin,SuperAdmin]` + log RGPD `:157-174`.
- **[OK] Données perso filtrées `userId`** : Submissions/Progress/RiskScore/Users — un non-admin ne voit que ses données ; filtre levé seulement si `IsInRole("admin")`.
- **[FAIBLE] F-04** : unicité SuperAdmin non contrainte applicativement (voir §4).

### G — Dépendances (CVE)
- **[OK] Backend .NET** : **0 vulnérabilité**, 0 package déprécié. Packages sensibles à jour : `JwtBearer 8.0.11`, `BCrypt.Net-Next 4.0.3`, `Google/MicrosoftAccount 8.0.11`, `Npgsql/EF 8.0.11`, `AspNetCoreRateLimit 5.0.0`.
- **[PROBLÈME] Frontend** : 10 vulnérabilités (0 critical, **4 High**, 5 moderate, 1 low). **E-04** = Next.js (runtime). **M-02** = postcss (XSS, moderate), minimatch/picomatch (ReDoS, **build/dev only**), @babel (low, dev). `dompurify 3.4.2` OK (pas de CVE applicable).

### H — Ollama — **conforme**
- **[OK]** Deux clients, tous deux **localhost:11434** par défaut : `OllamaLLMProvider.cs:32`, `OllamaChatbotService.cs:29`. Jamais `0.0.0.0` ni IP publique.
- **[OK] Entrée bornée** : `num_ctx` 2048/4096, `num_predict` 300/`MaxTokens`, timeouts (3 s health, 60+30 s coaching, 120 s chatbot), historique `TakeLast(10)`, instructions tronquées à 800 car (conforme aux 4 axes de `TaskForLesson`/OLLAMA_ROBUSTESSE).
- **[OK] Données** : le coaching n'envoie que des métadonnées de challenge construites serveur (+ garde-fou `ContainsForbiddenKeywords`) ; le chatbot envoie les messages user vers l'IA **locale** — aucune sortie vers un tiers externe.

---

## 4. MOYEN & FAIBLE (durcissement)

- **M-01 [MOYEN]** — Pas de rate limiting sur `POST /api/auth/reset-password` (`:392`) ni `GET …/validate` (`:380`). Bruteforce infaisable en pratique (token 256 bits) → défense en profondeur. *Reco : limite par IP sur ces 2 endpoints.*
- **M-02 [MOYEN]** — 5 CVE moderate frontend (postcss XSS corrigé par le bump Next ; minimatch/picomatch ReDoS = build/dev). *Reco : `npm audit fix` (non-`--force`) pour les transitifs dev.*
- **F-01 [FAIBLE]** — Side-channel temporel `forgot-password` : pour un compte réel, envoi mail synchrone (HTTP Brevo, 15 s) **avant** le `Task.Delay(250)` fixe ; pour un compte inexistant/rate-limité, saut direct au delay → la latence distingue les deux cas. *Reco : envoi mail en fire-and-forget ou compensation de temps.* (`AuthController.cs:347-377`)
- **F-02 [FAIBLE]** — Token de reset en query string (`?token=`) — peut fuiter via historique navigateur / `Referer` / logs proxy. Atténué par usage unique + expiration 30 min. *Reco : token en fragment `#` ou POST.*
- **F-03 [FAIBLE]** — Cookie `SameSite=Lax` : OK si front et API **same-site** (`app.x.fr`/`api.x.fr`). Si domaines distincts en prod → `fetch` cross-site n'enverra pas le cookie (il faudrait `SameSite=None; Secure`). *Reco : valider selon la topologie de déploiement.*
- **F-04 [FAIBLE]** — Unicité SuperAdmin non contrainte : la table `SuperAdmins` peut contenir N lignes `IsActive=true`. Détermination saine (pas d'email en dur, re-dérivé serveur, auto-promotion bloquée) mais aucune contrainte « un seul ». *Reco : index unique partiel sur `IsActive` OU documenter un modèle multi-SuperAdmin assumé.* (`SuperAdminController.cs:1278`)
- **F-05 [INFO/FAIBLE]** — `IsResetRateLimited` s'appuie sur `IMemoryCache` (compteur RAM non partagé) → inefficace en multi-instances derrière un load-balancer. *Reco : backend distribué (Redis) si scale horizontal.*

**Notes INFO** : comparaison de hash non constant-time (sans impact sur un hash 256 bits) ; retour de mots de passe temporaires en clair dans les réponses **admin-only** `AdminDirectoryController.cs:456`, `SuperAdminController.cs:464,494` (par conception, CSPRNG — s'assurer qu'ils ne sont pas journalisés côté serveur/proxy) ; `TestOAuthController` neutralisé en prod (`#if DEBUG` + garde runtime).

---

## 5. Tests concrets réalisés (statique + relecture code)

| Test demandé | Résultat |
|---|---|
| Token reset expiré refusé | **OK** — `ExpiresAt` vérifié serveur `:385,:411` |
| Token reset réutilisé refusé | **OK** — `UsedAt` posé `:425`, refusé `:411` |
| Pas d'énumération sur forgot-password | **OK** (message/HTTP identiques) — écart timing → F-01 |
| Rate limiting sur la demande de reset | **OK** — email 3/15min + IP 10/15min |
| Membre → endpoint admin refusé | **OK** — `[Authorize(Roles=...)]` serveur sur tous les endpoints admin |
| Membre → vue nominative refusée | **OK** — controllers nominatifs `[admin,SuperAdmin]` |
| Membre → classement complet refusé | **OK** — isolé dans `AdminCompetitionController`, public = top 5 anonymisé |
| Grep secrets en dur (code actuel) | **OK** — aucun secret hardcodé dans le `.cs` |
| Historique git (secrets commités) | **PROBLÈME** — DB pass + JWT key dans `ae503a2` (C-01/C-02) |

> **Limite méthodologique** : audit **statique** (relecture code + git + `dotnet list --vulnerable` + `npm audit`). Les tests dynamiques bout-en-bout (rejeu réel d'un token expiré via HTTP, mesure de timing réelle sur l'énumération, tentative live d'un membre sur un endpoint admin) **n'ont pas été exécutés contre une instance en fonctionnement** — cf. leçon `TaskForLesson` « l'e2e révèle ce que le grep rate ». Recommandé avant onboarding : une passe e2e sur ces scénarios. Non couvert dans cette passe : validation MIME des uploads binaires (import CSV `CsvImportService`).

---

## 6. Ordre d'exécution recommandé (quand tu valideras les corrections)

1. **Rotation des 3 secrets** : Postgres (C-01, + user applicatif non-superuser), JWT (C-02), Brevo (E-03). *Indépendants → parallélisables.*
2. **Fermer le trou log-only (E-01)** : retirer token/2FA/tempPassword des logs+`MailLogs`, purger la table existante, **brancher Brevo en prod** (clé en env).
3. **E-02** : révocation de session dans `ChangePassword` (rotation `SecurityStamp` + refresh tokens).
4. **E-04 / M-02** : `npm audit fix --force` (bump Next 16.2.10) puis `npm run build` + `npm run lint` ; `npm audit fix` pour les transitifs dev.
5. **Purge d'historique git** (C-01/C-02) si le repo est/deviendra partagé.
6. **Durcissement** : M-01 (rate-limit validate), F-01→F-05.
7. **Passe e2e dynamique** sur les scénarios du §5 avant d'ouvrir aux clients.

---

## 7. Corrections appliquées — 2026-07-15 (validées « on corrige tous 1 par 1 »)

> Backup : `.audit-backup/backup-full-20260715-160137.bundle`. Backend build **0 erreur / 0 warning**.
> Frontend build **OK**. Tests backend **126/127** (le seul échec est environnemental — HTTPS redirect du host de test, sans rapport avec l'auth). Fichiers touchés lint clean.

| # | Finding | Statut | Fichiers | Vérif |
|---|---|---|---|---|
| E-01 | Secrets (reset/2FA/mdp temp) en clair logs+DB | ✅ Corrigé | `Services/LogOnlyMailService.cs` | body persisté neutre ; secret en console **dev only** |
| E-02 | `ChangePassword` sans révocation de session | ✅ Corrigé | `Controllers/AuthController.cs` | rotation `SecurityStamp` + `IssueRefreshTokenAsync` (révoque autres, garde session courante) ; build OK |
| M-01 | Pas de rate-limit `reset-password`/`validate` | ✅ Corrigé | `AuthController.cs` | limite IP (60 validate / 20 consume) + rappel : `AspNetCoreRateLimit` couvre déjà reset-password 10/1h |
| F-01 | Side-channel temporel `forgot-password` | ✅ Corrigé | `AuthController.cs` | envoi mail en tâche de fond (scope dédié) + temps de réponse constant ~500 ms |
| E-03 | Clé Brevo en clair sur disque (config morte) | ✅ Corrigé (code) | `appsettings.Development.json` | clé scrubbée, bon emplacement documenté. **Rotation Brevo = action manuelle (§8)** |
| F-04 | Unicité SuperAdmin non contrainte | ✅ Corrigé (code) | `Data/AppDbContext.cs` + migration `EnforceSingleActiveSuperAdmin` | index unique partiel `filter "IsActive"=true`. **Migration NON appliquée (§8)** |
| F-03 | Cookies `SameSite=Lax` non configurable | ✅ Corrigé | `AuthController.cs`, `Services/SsoFlowService.cs` | `Auth:CrossSiteCookies` (défaut Lax) → `None`+`Secure` si cross-site. (OAuth `Program.cs:74/354` : voir §8) |
| F-02 | Token de reset en query string | ✅ Corrigé | `AuthController.cs` + `ctf-web/.../reset-password/page.tsx` | lien en fragment `#token=` (non transmis serveur/proxy/Referer) + fallback rétrocompat `?token=` |
| E-04 | Next.js 16.1.6 (CVE High runtime) | ✅ Corrigé | `ctf-web/package.json` + lock | bump `next`/`eslint-config-next` → `16.2.10` ; build OK. Script `lint` migré `next lint`→`eslint .` (Next 16 supprime `next lint`) |
| M-02 | CVE frontend transitifs | ✅ Réduit 10→2 | lockfile | `npm audit fix`. **2 moderate restants** = postcss imbriqué dans Next (XSS build-time), non corrigible sans downgrade Next → résiduel accepté, à lever au prochain bump Next |
| F-05 | Rate-limit `IMemoryCache` mono-instance | ⏸️ Accepté (beta) | — | OK en mono-instance. Passer à un store distribué (Redis) si scale horizontal. Pas de code. |
| C-01 | Mot de passe Postgres dans l'historique git | ⏳ Action manuelle | — | **Repo poussé sur GitHub → rotation NON négociable + purge historique (§8)** |
| C-02 | Clé JWT dans l'historique git | ⏳ Action manuelle | — | idem C-01 (§8) |

**Effet de bord assumé (E-04)** : `eslint .` remonte 34 erreurs lint **pré-existantes** dans des fichiers hors sécurité (`useTheme.ts`, dialogs…), dues aux règles react-hooks durcies de `eslint-config-next@16.2.10`. Non corrigées ici (hors périmètre sécurité, risque de régression) → tâche de nettoyage séparée. Les fichiers modifiés par cet audit sont lint clean.

---

## 8. Actions MANUELLES restantes (infra / irréversible — non exécutées)

**⚠️ Le repo est poussé sur `github.com/gomad13/CTF.SAAS.git` → les secrets de `ae503a2` sont hors de ta machine. Rotation obligatoire quel que soit l'état public/privé du repo.**

1. **Rotation Postgres (C-01)** : changer le mot de passe du compte DB, idéalement créer un user applicatif **sans privilèges DDL** (pas `postgres`). Mettre à jour `ConnectionStrings` en env/user-secrets.
2. **Rotation JWT (C-02)** : générer une clé et la poser en env `JWT_KEY` (dev : `dotnet user-secrets set "Jwt:Key" <clé>`). Génération : `openssl rand -base64 64`. Invalide tous les jetons existants (attendu).
3. **Rotation Brevo (E-03)** : révoquer l'ancienne clé côté Brevo, générer une nouvelle, la poser en env `Mail__BrevoApiKey` (prod) / `dotnet user-secrets set "Mail:BrevoApiKey" <clé>` (dev), avec `Mail__Provider=Brevo` pour activer l'envoi réel (ferme définitivement le risque log-only E-01).
4. **Purge historique (C-01/C-02)** — APRÈS rotation, si le repo reste partagé :
   ```
   # sauvegarde déjà faite : .audit-backup/backup-full-*.bundle
   git filter-repo --path "CTF. API/appsettings.Development.json" --invert-paths
   git filter-repo --path "CTF. API/bin" --invert-paths   # copies de build
   # puis re-add remote + push --force-with-lease, et prévenir tout clone existant
   ```
   Ne PAS lancer sans coordination : réécrit l'historique + force-push.
5. **Appliquer la migration F-04** : d'abord vérifier `SELECT count(*) FROM "SuperAdmins" WHERE "IsActive"=true;` = 1, puis `dotnet ef database update` (rebuild inclus, PAS `--no-build`).
6. **Cookies cross-site (F-03)** : si front et API sont sur des **domaines distincts** en prod, poser `Auth:CrossSiteCookies=true` (env `Auth__CrossSiteCookies=true`). Dans ce cas, ajuster aussi la corrélation OAuth (`Program.cs:74` `options.Cookie.SameSite` et `:354` `MinimumSameSitePolicy`) en `None` — à tester (sinon le SSO casse). Si same-site (`app.x.fr`/`api.x.fr`), laisser le défaut.
7. **Purger la table `MailLogs`** : elle peut déjà contenir des tokens/2FA/mdp en clair issus de l'ancien `LogOnlyMailService` — `DELETE`/anonymiser les lignes historiques.

**STOP** — aucune de ces 7 actions n'a été exécutée (infra/irréversible). Elles attendent ta main.
