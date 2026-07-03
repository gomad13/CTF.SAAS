# CLAUDE.md

> Fichier de contexte lu par Claude Code au début de chaque session.
> À mettre à jour dès qu'une règle, une convention ou l'architecture change.

---

## 1. Projet

- **Nom** : Viper
- **Type** : Plateforme de formation générique B2B multi-tenant (SaaS)
- **Modèle de contenu** : parcours → modules → challenges
- **Rôles** : `admin`, `user`

> Note : les dossiers techniques existants s'appellent `CTF.API` (backend) et `ctf-web` (frontend). Ces noms restent tels quels dans le code actuel ; le nom produit/commercial est **Viper**.

---

## 2. Stack

### Backend — `CTF.API`
- .NET 8 / C#
- Entity Framework Core
- PostgreSQL 18
- Migrations : EF Core classiques (`Add-Migration` / `Update-Database`)

### Frontend — `ctf-web`
- Next.js 16 (App Router)
- TypeScript (mode strict)
- Tailwind CSS
- **Icônes** : Lucide React (outline uniquement)
- **Font** : Inter (via `next/font`), fallback Plus Jakarta Sans

### Tests
- Aucun framework pour l'instant — à mettre en place plus tard.

### Environnement local
- Windows + PowerShell
- Visual Studio pour le .NET

---

## 3. Règles de travail (non négociables)

### R1 — Fais tout le boulot, pas la moitié
Chaque tâche demandée est exécutée intégralement. Jamais de "je te laisse finir cette partie", jamais de code incomplet "à compléter". Si la tâche implique plusieurs fichiers, je touche à tous les fichiers concernés.

### R2 — N'interromps pas pour poser des questions
J'exécute comme si l'utilisateur n'était pas là. En cas d'ambiguïté, je prends la décision la plus cohérente avec les règles de ce fichier et je continue. Je documente brièvement mes choix à la fin, jamais avant.

### R3 — Zéro erreur
Avant de rendre le travail, je relis ma production et je cherche activement les problèmes : compilation, runtime, typage, sécurité, logique métier, respect des règles du projet. Je corrige moi-même. Pas de "à toi de tester si ça marche".

---

## 4. Méthode de travail

### M1 — Plan d'abord, code ensuite
Avant la moindre ligne de code, j'écris le plan complet :
- Fichiers touchés
- Changements par fichier
- Ordre d'exécution
- Points de vérification

Si pendant l'exécution une hypothèse du plan s'avère fausse ou qu'un truc ne marche pas : **je m'arrête, je refais le plan, puis je reprends**. Jamais d'improvisation qui s'empile sur un plan cassé.

### M2 — Sous-agents pour le complexe, contexte principal propre
Toute tâche non triviale (exploration de codebase, refactor multi-fichiers, recherche de bug, lecture massive) est déléguée à un sous-agent. Le contexte principal reste minimal et focalisé sur la coordination et les décisions. **Jamais de dump de fichier complet dans le contexte principal si un sous-agent peut le digérer.**

### M3 — Capitalise les erreurs dans `TaskForLesson.md`
Chaque erreur rencontrée est transformée en règle durable ajoutée à `TaskForLesson.md`. Format :

```
## [YYYY-MM-DD] — Titre court
**Contexte** : ce que j'essayais de faire
**Erreur** : ce qui a foiré (symptôme + cause)
**Règle** : instruction permanente à appliquer désormais
```

**Au début de chaque session, je lis `TaskForLesson.md` avant de faire quoi que ce soit d'autre.** Les règles qui y sont écrites ont le même poids que celles de ce CLAUDE.md.

### M4 — Vérifie avant de marquer "terminé"
Aucune tâche n'est clôturée sans vérification réelle :
- Le build compile (backend ET frontend selon le périmètre)
- L'endpoint répond ce qui est attendu (curl / Swagger / requête directe)
- Les logs runtime ont été lus (pas supposés propres)
- La console navigateur est propre si UI touchée
- Les nouvelles migrations appliquées proprement si DB touchée

**"Normalement ça marche" n'est jamais une conclusion valide.**

---

## 5. Règles techniques

### 5.1 Backend

- **DTOs obligatoires** : une entité EF n'est JAMAIS renvoyée directement par un controller. Chaque ressource exposée a son `record XxxDto(...)` dans `Contracts/`.
- **async/await partout** : toute méthode qui touche la DB, HTTP ou I/O est `async Task<...>`. Jamais de `.Result` ni `.Wait()`.
- **`AsNoTracking()` sur toutes les lectures**. Le tracking EF est réservé aux écritures.
- **Filtrage par `tenantId` systématique** : chaque requête DB contient `.Where(x => x.TenantId == tenantId)`. Aucune exception, quel que soit le contexte.
- **Filtrage par `userId`** sur les endpoints de données personnelles : un user non-admin ne voit que ses propres données. Un admin voit tout le tenant.
- **Méthodes courtes** : max 30 lignes. Au-delà, on extrait.
- **Une seule responsabilité par fichier**.
- **Jamais faire confiance au client** pour une valeur de vérité : `IsCorrect`, `ScoreAwarded`, statut de progression, rôle effectif → calculés serveur uniquement.
- **Whitelist** sur toute valeur enum-like envoyée par le client (`type`, `status`, `role`, etc.) avant insertion DB.
- **Rate limiting** sur les endpoints sensibles. Ex. `POST /api/submissions` : 10 req/min par combinaison `userId + challengeId`, retour `429` avec `{ "error": "Too many attempts. Please wait before retrying." }`.
- **Champs sensibles en DB** : marqués `[JsonIgnore]` sur l'entité (`CorrectAnswer`, hashes, secrets) et absents des DTOs de sortie.
- **Pagination** : les endpoints de listing retournent un `PagedResult<T>`. Query params `page` (défaut 1) et `pageSize` (défaut 50).
- **Validation MIME** des uploads en plus de l'extension.
- **Middleware d'exception global** enregistré EN PREMIER dans `Program.cs` : en prod, aucune stack trace ne fuit — seulement `{ "error": "An unexpected error occurred." }`.

### 5.2 Frontend

- **JWT en cookie HttpOnly uniquement**. Jamais `localStorage`, jamais `sessionStorage`, jamais `document.cookie` écrit côté client. Le cookie est posé par le serveur via `Set-Cookie`, le front n'y touche pas.
- **`credentials: "include"`** sur TOUS les `fetch` vers l'API (sinon le cookie ne part pas en cross-origin).
- **Gestion centralisée du 401** dans `apiFetch` : sur `401` (hors `/api/auth/*`), redirection automatique vers `/login` + throw.
- **Logout = appel API** (`POST /api/auth/logout`) puis `window.location.replace("/login")`. Jamais de "suppression côté client" seule.
- **Protection des routes par appel API** dans `RequireAuth` (ping silencieux d'un endpoint protégé au montage). Pas de check sur un token local.
- **Zéro `any`** en TypeScript. Utiliser `unknown` + narrowing (`err instanceof Error`), ou des types explicites.
- **Typage explicite** des props, états, retours de fonction non-triviaux.

### 5.3 Sécurité & RGPD

- Isolation multi-tenant par `tenantId` sur chaque requête (point 5.1).
- Isolation par `userId` sur les données personnelles (point 5.1).
- `CorrectAnswer` et équivalents : `[JsonIgnore]` + jamais dans un DTO de sortie.
- Secrets JAMAIS versionnés. `.gitignore` doit contenir au minimum :
  ```
  appsettings.Development.json
  appsettings.*.json
  !appsettings.json
  .env
  .env.local
  *.user
  secrets.json
  ```
- CORS : `AllowCredentials()` activé, origines explicites (jamais `*`), méthodes limitées à `GET, POST, PUT, DELETE, OPTIONS`.
- `TenantMiddleware` : bypass pour `/api/health` et `/api/auth/*` (sinon l'auth elle-même est bloquée).

---

### 5.4 Charte graphique — Design System Viper (obligatoire)

**Philosophie** : interface Cyber-SaaS pro. Couleurs froides pour la structure, couleurs vibrantes pour l'action. Sobre, épuré, professionnel — inspiration Apple / Stripe / Linear. Chaque écran doit être beau, propre, pro.

> **CHARTE ACTUELLE (2026-07) : noir / gris / blanc + accent vert cyber, mode SOMBRE et CLAIR.**
> Remplace l'ancienne charte bleue/teal. Système de **tokens CSS** (`globals.css`) : mode clair = `:root`, mode sombre = `html.dark` (défaut). Toggle sombre/clair persistant (`useTheme` / `ThemeToggle`, cookie/localStorage `ctf_theme`). Contraste **WCAG AA** garanti dans les 2 modes. **Ne jamais réintroduire le bleu #3B82F6 (sauf état « info ») ni le teal #03b5aa.**

#### Palette — Mode SOMBRE (défaut)

| Rôle | Variable CSS | Hex |
|---|---|---|
| Fond principal | `--bg` | `#0A0A0B` |
| Surface / carte | `--surface` | `#161618` |
| Surface élevée | `--surface-2` | `#1F1F22` |
| Bordures | `--border` | `#242427` |
| Texte principal | `--text` | `#F5F5F7` |
| Texte secondaire | `--text-2` | `#A1A1A6` |
| Texte tertiaire | `--text-3` | `#6B6B70` |
| **Accent (vert cyber)** | `--accent` | `#22C55E` |
| Accent hover | `--accent-hover` | `#16A34A` |

#### Palette — Mode CLAIR

| Rôle | Variable CSS | Hex |
|---|---|---|
| Fond principal | `--bg` | `#FFFFFF` |
| Surface / carte | `--surface` | `#F5F5F7` |
| Surface élevée | `--surface-2` | `#EBEBED` |
| Bordures | `--border` | `#E4E4E7` |
| Texte principal | `--text` | `#0A0A0B` |
| Texte secondaire | `--text-2` | `#6B6B70` |
| Texte tertiaire | `--text-3` | `#A1A1A6` |
| **Accent (vert cyber)** | `--accent` | `#16A34A` |
| Accent hover | `--accent-hover` | `#15803D` |

#### États système (communs) & tokens Tailwind

| État | Hex | Succès `#22C55E` · Danger `#EF4444` · Alerte `#F59E0B` · Info `#3B82F6` |
|---|---|---|

- **Toujours utiliser les tokens** (`bg-surface`, `text-fg-heading`/`--text`, `bg-primary`/`--accent`, `border-border`, etc.) — jamais de hex en dur (sinon l'élément ne suit pas le mode sombre/clair et casse le contraste). Le vert cyber = identité (boutons primaires, liens actifs, focus, badges, highlights). Base neutre monochrome pour le reste.
- Tokens historiques (`--pr`, `--primary`, `canvas`, `card`, `--h1`…) repointés sur ce système : le code existant suit automatiquement le thème.

#### Typographie

- **Police principale** : Inter (chargée via `next/font/google`). Fallback : Plus Jakarta Sans puis system-ui.
- **Titres H1** : `font-bold` (700), `text-[#1E293B]`.
- **Sous-titres / labels** : `font-medium` (500), `text-[#64748B]`.
- **Corps** : 14px ou 16px, `leading-relaxed` (line-height 1.5).
- **Header de tableau** : `uppercase`, `text-xs` (12px), `tracking-wider`.

#### Composants — règles standards

**Cartes KPI (dashboards)**
- Fond `bg-surface` (#FFFFFF).
- Coins `rounded-xl` (12px).
- Ombre : `shadow-[0_4px_6px_-1px_rgb(0_0_0_/_0.1)]`.
- Padding interne minimum `p-6` (24px).
- Icône Lucide filaire en haut à droite, `opacity-20`.

**Cartes de parcours (listes horizontales)**
- Fond `bg-surface`, bordure fine `border border-[#E2E8F0]`.
- Coins `rounded-xl`, padding `p-6`.
- Titre à gauche, bouton d'action à droite (`flex items-center justify-between`).
- Barre de progression : hauteur `h-1.5` (6px), fond `bg-[#E2E8F0]`, remplissage `bg-primary`, **coins `rounded-full` (pill-shape obligatoire, jamais de coins carrés)**.

**Tableaux d'administration**
- Header : `bg-[#F1F5F9]`, texte `uppercase text-xs tracking-wider text-[#64748B]`.
- Lignes : bordures **horizontales uniquement** (`divide-y divide-[#E2E8F0]`). **Jamais de bordures verticales.**
- Lignes au survol : `hover:bg-[#F8FAFC]`.

**Pills de statut (style Apple/Stripe)**
- Actif / succès : `bg-success/10 text-success` → fond vert 10 %, texte vert foncé.
- Inactif / danger : `bg-danger/10 text-danger`.
- En cours / alerte : `bg-warning/10 text-warning`.
- Forme : `rounded-full px-2.5 py-0.5 text-xs font-medium`.

**Boutons primaires**
- `bg-primary text-white hover:bg-primary-hover`.
- `rounded-lg px-4 py-2 font-medium`.
- Transition obligatoire : `transition-colors duration-200`.

#### Détails UX/UI non négociables

1. **Glassmorphism léger** sur sidebar et menus déroulants : `backdrop-blur-md` (8px) + `bg-white/80` (blanc 80 % d'opacité) — ou `bg-sidebar/80` pour la sidebar sombre.
2. **Espacement (white space)** : minimum **24px de padding interne** (`p-6`) sur toute carte, container ou section. **Jamais de texte collé au bord.**
3. **Micro-interactions** : tout élément interactif (bouton, lien, item de nav) a `transition-colors duration-200` (ou `transition-all` si plusieurs propriétés changent). Hover = nuance plus foncée de la même teinte.
4. **Icônes** : **Lucide React uniquement**, **toujours en outline/filaire**. Jamais mélanger avec des icônes pleines. Jamais importer une autre lib (Heroicons, FontAwesome, etc.) dans le même projet.
5. **Aucune ombre lourde** : rester sur `shadow-sm` ou l'ombre custom définie pour les cartes KPI. Pas de `shadow-2xl`.

#### Déclaration centrale

- **`tailwind.config.js`** → `theme.extend.colors` avec tous les tokens de la palette.
- **`src/app/globals.css`** → `:root` avec les mêmes tokens en CSS variables (`--color-primary`, etc.) pour interop hors Tailwind.
- **`src/app/layout.tsx`** → import de la font Inter via `next/font/google` et application sur `<html>` ou `<body>`.

---

## 6. Structure des dossiers

### `CTF.API`
```
CTF.API/
├── Contracts/        # DTOs, records de requête/réponse, PagedResult<T>
├── Controllers/      # Routing + orchestration uniquement
├── Data/             # AppDbContext, configurations EF Fluent API
├── Middleware/       # ExceptionMiddleware, middlewares custom
├── Models/           # Entités EF (JAMAIS exposées directement)
├── Security/         # TenantMiddleware, JWT, helpers d'auth
└── Program.cs        # Pipeline : ExceptionMiddleware en premier
```

### `ctf-web`
```
ctf-web/src/
├── app/              # Pages App Router (login, dashboard, admin, paths/..., ...)
├── components/       # Composants React (AppShell, RequireAuth, ...)
└── lib/              # apiFetch, helpers, types partagés
```

---

## 7. Commandes utiles (PowerShell)

### ⚠️ À exécuter à CHAQUE nouvelle session PowerShell
Le PATH PostgreSQL ne persiste pas entre sessions :
```powershell
$env:PATH += ";C:\Program Files\PostgreSQL\18\bin"
```

### Backend
```powershell
cd CTF.API
dotnet restore
dotnet build
dotnet run

# Migrations EF Core
dotnet ef migrations add NomDeLaMigration
dotnet ef database update

# Revenir sur une migration antérieure
dotnet ef database update NomDeLaMigrationCible
```

### Frontend
```powershell
cd ctf-web
npm install
npm run dev          # http://localhost:3000
npm run build
npm run lint
```

---

## 8. Checklist avant de marquer une tâche terminée

- [ ] Le plan initial a été suivi (ou refait proprement si cassé)
- [ ] Le build backend compile sans warning bloquant
- [ ] Le build frontend compile et `npm run lint` passe
- [ ] Chaque requête DB touchée filtre par `tenantId`
- [ ] Les endpoints sensibles filtrent aussi par `userId` (sauf admin)
- [ ] Aucune entité EF n'est exposée directement par un controller
- [ ] Aucun champ sensible n'apparaît dans un DTO de sortie
- [ ] Aucun `any` TypeScript introduit
- [ ] Aucune méthode ne dépasse 30 lignes
- [ ] Les couleurs UI utilisent les **tokens CSS** (`var(--bg/--surface/--text/--accent/--border)` ou classes Tailwind mappées) — **aucun hex en dur** (sinon casse le mode sombre/clair). Vert cyber pour les accents ; jamais de bleu `#3B82F6` (hors état info) ni de teal `#03b5aa` / navy `#0F172A`
- [ ] Icônes en Lucide React filaires uniquement
- [ ] Padding interne ≥ 24px sur toute nouvelle carte/container
- [ ] Transitions `duration-200` sur tout élément interactif ajouté
- [ ] Les logs runtime ont été lus réellement
- [ ] Si erreur rencontrée : règle ajoutée à `TaskForLesson.md`

---

## 9. Fichiers liés

- **`TaskForLesson.md`** — règles apprises des erreurs passées. Lu au début de chaque session, avant tout autre travail.
- **`.gitignore`** — doit contenir les entrées listées en 5.3.

---

## 10. Optimisation du workflow (tokens & parallélisation)

> Méta-outillage : ces règles s'AJOUTENT à celles ci-dessus, elles ne remplacent aucune règle de sécurité/qualité/charte/RGPD. (Numérotée 10 car 8 = Checklist et 9 = Fichiers liés existent déjà.)

- **Économie de tokens** : lire ciblé (`grep` avant de lire, plages de lignes utiles, pas de fichier entier), éditer chirurgical (diff/str_replace), filtrer les sorties (`head`/`tail`/`grep`), réponses concises, ne pas relire/re-dumper ce qui est déjà connu.
- **Parallélisation** : traiter en parallèle les tâches **indépendantes** (frontend/backend, recherches multiples, fichiers différents, sous-agents) ; **séquentialiser** les tâches **dépendantes ou risquées** — migrations EF (`add`→rebuild→`update`), déploiement (`publish`/`build`→restart→health), modifications du même fichier, chaîne entité→migration→usage.
- **La vitesse ne compromet jamais la sécurité** : backup avant modif destructrice, isolation tenant, tests réels — priorité **sécurité > exactitude > vérif > vitesse > tokens**.
- **Skills associées** (dans `.claude/skills/`) : `economie-tokens`, `parallelisation-taches`, `workflow-efficace-sentys` (synthèse).
