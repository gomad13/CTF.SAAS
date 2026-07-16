# TaskForLesson.md

> Lu par Claude Code au début de chaque session, AVANT tout autre travail.
> Chaque erreur rencontrée devient une règle permanente ici.
> Les règles de ce fichier ont le même poids que celles de `CLAUDE.md`.

---

## Format d'une entrée

```
## [YYYY-MM-DD] — Titre court de l'erreur
**Contexte** : ce que j'essayais de faire
**Erreur** : ce qui a foiré (symptôme observé + cause identifiée)
**Règle** : instruction permanente à appliquer désormais, formulée comme un ordre
```

---

## Règles apprises

## [2026-04-21] — Tailwind v4 : un reset CSS unlayered neutralise toutes les utilities padding/margin
**Contexte** : fix de la « zone vide ~65 % du viewport » sur `/admin/compliance` et les autres pages modes. Bug déjà « corrigé » plusieurs fois, qui revenait systématiquement.
**Erreur** : `globals.css` contenait `*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}` **hors de tout `@layer`**. Tailwind v4 émet ses utilities dans `@layer utilities`. Spec CSS Cascading 5 §7 : une règle non layerisée bat toute règle layerisée — donc `.py-8`, `.px-6`, `.mx-auto`, etc. étaient silencieusement neutralisées sur tout le site. `gap-6` fonctionnait (non touché par le reset) ce qui masquait le problème. Les pages avec `style={{ padding: … }}` inline échappaient grâce à la spécificité inline, d'où une fausse impression de « ça marche sur certaines pages, pas d'autres ».
**Règle** : tout CSS custom qui touche une propriété aussi gérée par Tailwind utilities (`margin`, `padding`, `color`, `font-size`, `background`, bordures, etc.) **doit** être enveloppé dans `@layer base` (resets) ou `@layer components` (classes utilitaires personnalisées). Ne jamais écrire de règle top-level non layerisée dans `globals.css` à côté de `@import "tailwindcss"`. Signal de détection en DevTools : une classe Tailwind barrée dans Styles, remplacée par une règle issue du sélecteur universel `*`.

## [2026-04-21] — CORS : `WithMethods` doit lister TOUS les verbes HTTP effectivement utilisés
**Contexte** : fix des 5 toggles modes entreprise (Compétition, Analytics, Compliance, Équipes, Campagnes) qui étaient sans effet côté UI. Les mêmes endpoints, testés en curl, retournaient HTTP 200.
**Erreur** : `Program.cs` configurait la policy CORS avec `WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")` — sans PATCH — alors que les 5 endpoints toggles sont `[HttpPatch]`. Le browser envoyait un preflight OPTIONS qui recevait un `Access-Control-Allow-Methods` sans PATCH, donc rejetait la requête avant envoi. curl ne fait pas de preflight → fausse sensation que « l'API fonctionne ». CLAUDE.md §5.3 prescrivait la même liste incomplète, d'où répétition du bug à chaque session de nettoyage.
**Règle** : quand l'API expose des endpoints PATCH (ou autre verbe), vérifier que `WithMethods(…)` du CORS liste littéralement ce verbe. Règle dans CLAUDE.md §5.3 à lire comme « liste EXHAUSTIVE des verbes réellement utilisés par l'API, pas liste minimale théorique ». Liste de base requise aujourd'hui : `GET, POST, PUT, PATCH, DELETE, OPTIONS`. Avant de dire qu'un bug toggle est côté front, tester curl vs browser : divergence = problème CORS ou preflight.

## [2026-04-21] — L'e2e multi-cas révèle les oublis qu'un audit de code rate
**Contexte** : prompt "progression universelle", supposition = hardcoding d'ID médical. Grep du code a confirmé qu'il n'y avait pas de hardcoding. Tentation : conclure "pas de bug".
**Erreur** : un audit de code exhaustif est nécessaire mais pas suffisant. Le bug réel (`CompleteFreeText` oublie `RefreshPathProgressAsync`) n'aurait pas été trouvé par grep. Il est apparu seulement en exécutant un **e2e qui traverse 2 parcours couvrant les 6 types de challenges**. Le premier parcours (Medical, 9 challenges mais aucun free_text) passait. Le second (Sensibilisation, 6 challenges dont un free_text en dernier) a révélé la divergence détail=100% vs liste=83%.
**Règle** : un bug "feature marche pas sur X" nécessite un e2e qui **couvre réellement** X dans toute sa variabilité, pas juste un "smoke test". Pour un système à N variantes (types de challenges, types de tenants, types de modes), l'e2e doit avoir au moins 2 cas par variante qui diffèrent. Un audit de code + un e2e trop étroit peut "prouver" qu'il n'y a pas de bug alors qu'il y en a un. Règle pratique : **si l'audit conclut "pas de bug", lancer l'e2e le plus varié possible avant de clôturer** — c'est là qu'on trouve les 15% de cas qui échappent au grep.

## [2026-04-21] — Pour une IA locale, UX perçue ≠ latence brute : streamer + prompt court
**Contexte** : ARIA Ollama local, TTLT 34 s perçu comme « app figée », TTFT 1.6 s seulement. Tentation : changer de modèle.
**Erreur** : le premier instinct était « c'est lent, il faut un modèle plus rapide ». Bench montre que llama3.2 et qwen2.5 ont TTFT et tokens/sec équivalents sur la même machine. Le vrai gain vient d'**un autre endroit** : le **prompt system** (de 2000 → 180 tokens → TTFT réel perçu divisé par 4-5 avec streaming) et le **streaming SSE** (transforme 34s d'attente aveugle en 1.5s avant premier mot + 10s de texte qui défile).
**Règle** : avant d'optimiser une latence IA locale, mesurer les **trois temps** séparément : **prompt-eval** (= TTFT), **generation** (= tokens/sec), **response length** (= num_predict). Le gain UX majeur est souvent : (a) **streamer** pour transformer la latence totale en latence perçue du premier token, (b) **raccourcir le system prompt** (chaque token y est évalué à chaque requête), (c) **capper num_predict** pour les Q&R pédagogiques. Changer de modèle vient **après**, et seulement si le modèle cible est ≥ en qualité sur un bench métier (pas juste un benchmark général).

## [2026-04-21] — Soft delete silencieux = impression d'échec côté utilisateur
**Contexte** : SuperAdmin « Supprimer » sur tenant — rapport user « ça marche pas ».
**Erreur** : le controller faisait `tenant.IsActive = false` (soft delete) et retournait 200. Le tenant restait dans la liste avec badge « Suspendu » → l'utilisateur pensait l'action ratée. En plus, blocage supplémentaire non documenté `if activeUsers > 0 → 400`.
**Règle** : soft delete est acceptable **seulement** si l'UI l'affiche correctement ("Corbeille", filtre `?deleted=true`, etc.). Pour un bouton intitulé « Supprimer » sans contexte de corbeille, l'utilisateur attend un **hard delete**. Décider dès le design : soft ou hard, documenter le choix, et rendre l'UX cohérente. Un soft delete qui ne dit pas son nom est pire qu'une erreur.

## [2026-04-21] — Formule de progression stockée ≠ formule affichée = bombe à retardement
**Contexte** : parcours à 9/9 affiché 100% sur détail et 97% sur liste.
**Erreur** : deux chemins de calcul divergents. Le détail (endpoint récent) utilisait binaire `completed/total`. La persistance `Progresses.Percent` (endpoint ancien) utilisait pondéré `earnedPoints/maxPoints`. La liste lisait la valeur stockée → 97. Le détail recalculait à la volée → 100.
**Règle** : tout indicateur agrégé (progression, score moyen, pourcentage de complétion) **doit** passer par **un seul** service/fonction. Les endpoints lecture appellent ce service ; les endpoints écriture appellent la méthode `...AndPersistAsync()` du même service pour garantir que la valeur stockée et la valeur calculée à la volée sont identiques. Toute duplication de formule est à refactorer immédiatement. Quand on change la formule, prévoir la migration one-shot pour recalculer les rows existantes.

## [2026-04-21] — Garde-fou métier ≠ bug : remplacer l'UX native, pas la logique
**Contexte** : audit SuperAdmin, `alert("Cannot delete demo tenant")` natif visible sur screenshot.
**Erreur** : tentation initiale de « fixer » l'endpoint qui retournait 400 — alors qu'il s'agit d'une **protection volontaire** (tenant Demo = contenu public de la plateforme). Supprimer le 400 = casser la démo.
**Règle** : avant de toucher à un endpoint qui retourne une erreur, distinguer **bug** (404/500 ou action qui ne persiste pas) vs **garde-fou métier** (400 avec message explicatif). Les garde-fous : **on préserve la logique, on améliore l'UX** (remplacer `alert()/confirm()/prompt()` natifs par des modals propres avec message métier humanisé, ex. traduire "Cannot delete demo tenant" en "Le tenant Demo est le tenant de démonstration…"). Dans un panneau admin, une UX métier passe par un dialog cohérent avec le thème, **jamais** par `window.alert`.

## [2026-04-21] — Deux tables pour une « complétion » : toujours unir les deux à la lecture
**Contexte** : bug « progression parcours 0/9 » après complétion de 3 challenges médicaux. DB à jour, UI à 0.
**Erreur** : les challenges `type=interactive` écrivent dans `ChallengeCompletions`, les non-interactifs dans `Submissions`. Le front ne lisait que `/api/submissions/recent` — blind spot total sur les 7 composants interactifs. Pire : cet endpoint est limité à `.Take(5)` côté back, donc même un parcours 100 % non-interactif afficherait faux au-delà de 5 complétions.
**Règle** : pour tout indicateur de progression, l'endpoint de lecture **doit unir** `Submissions.IsCorrect` et `ChallengeCompletions` — jamais lire une seule table. Les endpoints `TOP n` / `*/recent` sont à bannir pour nourrir un indicateur de complétion (réservés aux feeds d'activité). Progression = calcul serveur dans `Progresses.Percent`, servi tel quel au front, jamais recalculé côté client.

## [2026-04-21] — Tailwind-400 sur card blanche est systématiquement illisible
**Contexte** : fix contraste admin. Les KPIs dashboard (`#4ade80`, `#60A5FA`, `#f87171`) apparaissaient comme des chiffres fantômes.
**Erreur** : les variantes 400 Tailwind (emerald-400, red-400, amber-400, blue-400) ont un contraste 1.7-2.8:1 sur fond blanc — **très en-dessous de WCAG AA (4.5:1)**. Pattern hérité du mode dark appliqué sans relecture.
**Règle** : sur card blanche, n'utiliser que des tokens 600-700 pour du texte coloré (`#047857`, `#B91C1C`, `#B45309`, `#2563EB`). Les 400-500 sont acceptables uniquement comme **fonds** (ex : `bg-emerald-500/10 text-emerald-700`). Interdit pour du texte direct sur fond clair.

## [2026-04-21] — Ne pas combiner bg-{couleur}/alpha avec text-white sur un élément interactif
**Contexte** : bug récurrent boutons « transparents » quand sélectionnés. Options de quiz disparaissaient au click.
**Erreur** : motif `bg-primary/10 text-white` — 10% d'opacité de primary donne un fond quasi blanc, et le texte blanc par-dessus est invisible (ratio ~1.05:1). Copié dans au moins 3 fichiers. De même, `hover:bg-primary/90` fade le bouton au lieu de changer de teinte.
**Règle** : **jamais d'alpha sur un bg de bouton/option/onglet**. État sélectionné = teinte pleine (`bg-primary text-white`, ratio 3.68:1 accepté comme variance brand). Hover = token dédié (`hover:bg-primary-hover`), pas d'alpha. Lien ghost = changement de couleur au hover (`hover:text-primary-hover`), pas `hover:opacity-*`. `disabled:opacity-50` reste la seule exception légitime.

## [2026-04-21] — Ne jamais réécrire le hash du compte admin principal pour débugger
**Contexte** : diagnostic Bug 2, besoin d'un admin CyberMed loguable par mes scripts. Première tentative : UPDATE le hash de `h.madoumier@orange.fr`.
**Erreur** : j'ai écrasé le PasswordHash de l'utilisateur réel, puis dû le restaurer depuis la valeur que j'avais lue juste avant. Failure mode : si j'avais oublié de capturer l'ancien hash, je cassais l'accès admin permanent.
**Règle** : pour tout diag nécessitant un login, **créer un user test dédié** (`test-fix@<tenant>-test.local` par exemple) avec un hash de password connu (ex. réutiliser le hash d'un employé seed avec password connu), et `DELETE` ce user à la fin de la session. Ne JAMAIS modifier les credentials des comptes réels, même « temporairement » — le risk de non-restauration est réel et la sanction grande.

## [2026-07-01] — Serveur prod : build via le .csproj (pas le .sln), et POST authentifiés bloqués par CSRF + middleware consentement
**Contexte** : features M1 (onglet SuperAdmin multi-sociétés) + M2 (cloisonnement QR) sur `ubuntu@5.196.64.101`. Le code prod du serveur est **en avance** sur le repo local (`UserTenant`, multi-sociétés, `/api/me/tenants` n'existent que côté serveur).
**Erreur** : (1) `dotnet build` sur `backend/CTF. API.sln` échoue — la solution référence `../CTF.Api.Tests/CTF.Api.Tests.csproj` absent. (2) Un `curl` de login renvoie `403 "Missing X-Requested-With header (CSRF protection)"`. (3) Le premier `POST /api/invites/redeem` renvoie `{"requiresConsent":true,...}` : un `RequireUpToDateConsentMiddleware` global intercepte toute action tant que l'user n'a pas accepté CGU/DPA/politique-confidentialité.
**Règle** : sur ce serveur, (1) builder/publier **le projet** `dotnet publish "CTF. API.csproj" -c Release -o publish`, jamais le `.sln`. (2) Tout appel API en curl doit porter l'en-tête `-H "X-Requested-With: XMLHttpRequest"` (CSRF) en plus du cookie. (3) Pour tester un flux authentifié qui déclenche une action métier, d'abord accepter les consentements du user de test : `POST /api/me/consents/re-accept` avec `{"consents":[{documentSlug,documentVersion,accepted:true}]}` pour chaque doc listé dans `missingDocuments`. Toujours vérifier l'état réel côté serveur (SSH) avant de coder : le repo local peut être derrière la prod.

## [2026-07-01] — « Ollama plante » = épuisement de ressources côté appelant, pas un crash OS
**Contexte** : exercice IA « réponse libre évaluée par Ollama » signalé comme faisant « planter Ollama ». Premier réflexe : chercher un OOM/crash du service Ollama.
**Erreur** : Ollama était `active` depuis 3 semaines, aucun OOM, 42 Gi libres. Le vrai problème était dans le **code d'appel** (`FreeTextEvaluatorService`) : **entrée non bornée** (une réponse de 240 k caractères = ~40 s de prompt-eval + 11 Go pour UNE requête), **timeout 120 s** sans `CancellationToken`, et **aucune limite de concurrence** alors qu'Ollama traite en série (1 slot). Sous charge → saturation CPU/mémoire → timeouts en cascade → perçu comme un crash.
**Règle** : tout appel à un LLM local (Ollama) DOIT être borné sur **4 axes** : (1) **taille d'entrée** (rejet contrôleur + troncature service), (2) **timeout court et annulable** (`CreateLinkedTokenSource` + `CancelAfter`, jamais le seul `HttpClient.Timeout` à 120 s), (3) **concurrence plafonnée** (`SemaphoreSlim` statique, ex. 2, avec **repli immédiat** si aucun créneau — ne PAS faire la queue), (4) **fallback local systématique** (heuristique sans réseau) pour que l'exercice reste terminable. Diagnostiquer un « crash Ollama » commence TOUJOURS par `free -h`, `journalctl -u ollama`, `dmesg | grep -i oom`, `systemctl show ollama -p MemoryCurrent` et **reproduire avec une entrée pathologique** (énorme / caractères spéciaux / concurrence) avant de toucher Ollama. Garder Ollama en `127.0.0.1` (vérifier `ss` + `ufw`), l'évaluateur ne reçoit que du texte (pas de données tenant).

## [2026-07-15] — `migrations add --no-build` génère une migration VIDE, et la DLL périmée fait échouer le remove/re-add
**Contexte** : ajout d'un index unique partiel sur `SuperAdmins` (config Fluent dans `AppDbContext`), puis `dotnet ef migrations add EnforceSingleActiveSuperAdmin --no-build`.
**Erreur** : (1) `--no-build` a réutilisé l'assembly compilé AVANT l'ajout de la config Fluent → migration générée avec `Up()`/`Down()` **vides** (aucun `CreateIndex`). (2) `migrations remove --no-build` a supprimé les `.cs` mais l'ancienne classe de migration restait **compilée dans la DLL bin/**, donc le `migrations add --no-build` suivant refusait : « The name '…' is used by an existing migration ». Cercle vicieux tant qu'on ne rebuild pas.
**Règle** : pour TOUTE opération EF qui dépend du modèle courant (`migrations add`, `remove`, `database update`), **rebuild d'abord** (ou ne PAS passer `--no-build`). Après avoir modifié une config Fluent/entité : `dotnet build` → puis `dotnet ef migrations add …`. Toujours **vérifier le contenu du `Up()`** généré (`grep CreateIndex/AddColumn`) avant de considérer la migration bonne : une migration vide = signal d'assembly périmé. `--no-build` n'est sûr que si l'on vient de builder juste avant, sans aucune modif de code entre-temps.

## [2026-07-15] — Next 16 supprime `next lint` : `npm run lint` casse après un bump, et la nouvelle config durcit react-hooks en erreurs
**Contexte** : bump `next` 16.1.6 → 16.2.10 (CVE High) + `eslint-config-next` aligné.
**Erreur** : (1) le script `"lint": "next lint"` échoue avec « Invalid project directory provided, no such directory: …/lint » — `next lint` est **retiré dans Next 16**. (2) après migration vers `eslint .`, la version récente d'`eslint-config-next` passe des règles react-hooks **d'opinion perf** en ERREUR (`react-hooks/set-state-in-effect`, `react-hooks/purity`, `immutability`) → 34 erreurs surgissent dans du code pré-existant non modifié.
**Règle** : quand on bump Next à une majeure, migrer le script lint (`next lint` → `eslint .`) et s'attendre à ce que la nouvelle config surface du debt lint pré-existant. Dans une passe ciblée (sécu, feature), **ne pas corriger en masse** ce debt non lié (risque de régression) : vérifier seulement que **les fichiers modifiés** sont lint clean (`npx eslint <fichier>`), garder le build vert (gate de shippabilité), et documenter le debt surfacé comme tâche séparée. Ne jamais affaiblir une règle juste pour « faire passer » le lint global.

## [2026-06-21] — `dotnet ef database update --no-build` après un nouveau migration n'applique rien
**Contexte** : V4, après `dotnet ef migrations add AddTenantInvite`, j'ai lancé `dotnet ef database update --no-build` pour gagner du temps.
**Erreur** : « No migrations were applied. The database is already up to date » alors que la migration venait d'être créée. Cause : `--no-build` réutilise l'assembly compilé AVANT l'ajout de la migration → EF ne voit pas la nouvelle migration dans la DLL.
**Règle** : après `migrations add`, toujours **rebuild** (ou lancer `database update` SANS `--no-build`) avant d'appliquer. Le fichier de migration sur disque ne suffit pas : EF lit l'assembly compilé.
