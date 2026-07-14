# FLASHCARDS_PARCOURS_LOG.md — Flashcards comme module (épreuve) dans les parcours

> Objectif : type de module « Flashcards » en **mode ÉPREUVE uniquement**, option admin **noté / non noté**. 100 % LOCAL, pas de push, pas de déploiement.
> Autorité : `Instructions.md` absent → **CLAUDE.md** (charte §2, qualité §1, sécu §3/§7).

## 0. Backup (méthode §1)
- `backups/flashcards-parcours-20260714_022931/` : `front/` (paths, dashboard-parcours, components-paths, flashcards-src), `back/` (Challenge, Module, ChallengeCompletion, Progress), `schema.sql`.

## 1. Modèle existant (analyse)

### Unité jouable = **Challenge** (le « module » du parcours au sens du prompt)
- Hiérarchie : `LearningPath (Path)` → `Module (conteneur : Title, SortOrder)` → `Challenge (jouable)`.
- `Challenge` : `Type`, **`ContentType`** (ex. `mailbox`, `multichoice`, `phishing_ai`, **`flash_cards`**…), `ContentJson` ([JsonIgnore]), `Points`, `CorrectAnswer` ([JsonIgnore]), `Status`, `ModuleId`, `SortOrder`.
- ➡️ **Les flashcards existent DÉJÀ comme ContentType `flash_cards`** (challenge), avec un player `components/challenges/FlashCardsChallenge.tsx` (sous-types `match` / `flip`).

### Scoring & complétion (à RÉUTILISER — pas de barème parallèle)
- Endpoint : `POST /api/challenges/interactive/{challengeId}/submit-flash-cards` (`ChallengeInteractiveController`).
  - `match` : score serveur via `correct_right_id` masqué → `scorePct = (int)Math.Round(100.0 * correct / total)`.
  - `flip`/épreuve : `scorePct = (int)Math.Clamp(Math.Round(100.0 * known / total), 0, 100)`.
  - `pointsEarned = (int)Math.Round(challenge.Points * scorePct / 100.0)`. **Arrondi entier propre, aucun flottant sale.**
- `UpsertCompletionAsync(userId, tenantId, challenge, pointsEarned, scorePct)` : upsert `ChallengeCompletion` (**meilleur score gagne**) puis `RefreshPathProgressAsync` → `ProgressCalculationService` (**Progress.Percent = #completed/#total binaire**).
- ➡️ **Complétion = binaire** (ligne `ChallengeCompletion` existe ⇒ étape complétée, progression avance) ; **score = séparé** (via `PointsEarned`).

### Deux implémentations flashcards (à réconcilier)
1. **Ancienne** : ContentType `flash_cards` (match/flip), scorée + intégrée parcours, tokens **globaux** (`--accent`/`--surface`/`--success`…), pas violet vision.
2. **Nouvelle (3 modes)** : `components/flashcards/` (`EpreuveMode`, `RevisionMode`, `MemoryMode`), riche, animations juste/faux, `/flashcards-test`, **données démo** (`lib/flashcards-demo.ts`). C'est celle que le prompt veut brancher (mode Épreuve).

## 2. Plan d'intégration (à finaliser après inventaire scoring parcours + player + jeux de cartes — sous-agents)
- **Réutiliser** le ContentType `flash_cards` + `submit-flash-cards` + `UpsertCompletionAsync` (scoring/complétion/progression identiques aux autres exercices).
- **Config module** (dans `ContentJson`) : `mode: "epreuve"` (fixe), **`note: bool`**, + le jeu de cartes (Terme/Définition ou Risque/Parade) — réf ou embarqué selon ce qui existe.
- **Noté** : `Points > 0` → score normal (barème/arrondi existants).
- **Non noté** : `Points = 0` → complétion créée (étape complétée, progression avance) mais **0/0 = aucun impact score** (à valider selon la formule d'agrégation du score de parcours).
- **UI membre** : jouer l'`EpreuveMode` (nouveau, violet, animations) dans le player de parcours quand `ContentType == flash_cards` & `mode == epreuve` ; à la fin → `submit-flash-cards` (correct/total) → résultat + progression.
- **UI admin** : ajouter un module/challenge flashcards à un parcours, choisir noté/non-noté + le jeu de cartes.

## 3. Points de scoring (rappel exact à respecter)
- `scorePct` : `int` = `Math.Round(100.0 * correct / total)` (clamp 0-100).
- `pointsEarned` : `int` = `Math.Round(Points * scorePct / 100.0)`.
- Complétion via `UpsertCompletionAsync` (best score) + `ProgressCalculationService`.

## 4. DÉCISIONS FIGÉES (analyse terminée)
- **Score de parcours** : il n'existe **pas** de score de parcours persisté — seulement la **complétion** (`Progress.Percent` = #completed/#total binaire) + le **score par challenge** (`ScorePercent`/`PointsEarned`). Donc « le score » = le **barème de points**.
  - **Noté** = challenge `Points > 0` → `pointsEarned` normal (barème/arrondi existants).
  - **Non-noté** = challenge `Points = 0` → complétion créée (progression avance) mais `pointsEarned = round(0·%) = 0` → **aucun impact barème**. (Le `ScorePercent` est stocké pour info ; sans points il ne pèse sur aucun total de points.)
- **Dispatch player** : `dashboard/challenge/[id]/page.tsx` l.383-399, `contentType==="flash_cards"` → `FlashCardsChallenge`. J'ajoute : si `content.subtype==="epreuve"` → nouveau `EpreuveFlashCardsChallenge`, sinon `FlashCardsChallenge` (match/flip inchangé).
- **Livraison contenu** : `GET .../content` strippe les clés sensibles (`correct_right_id`, `is_correct`…). `correct_index` **n'y est pas** → livré au client (nécessaire au feedback immédiat de l'épreuve, comme les `back` du flip). Le **compte de bonnes réponses** part au serveur via la branche flip (`{knownCount, total}`), qui **possède l'arrondi + points + complétion** (modèle identique au flip existant).
- **Jeux de cartes** : aucune entité DB — seules des **données démo** existent (`lib/flashcards-demo.ts` : 8 cartes QCM cyber ; `flashcards-memory-demo.ts` : risque/parade + terme/déf). Type `Flashcard = {id, category, front, back, choices[], correctIndex}`. → L'admin choisit un **jeu existant** ; le front envoie les cartes au endpoint de création (stockées en `ContentJson`).
- **EpreuveMode** : `{cards, onExit}` ; joue le QCM, feedback juste/faux, `SessionRecap` (correct/total). **Utilise les tokens GLOBAUX verts** (`--accent`=#22C55E, `--success`…) → **à convertir en `--v-*` (violet)** + ajout d'un callback `onFinish(correct,total)`. Rendu dans un conteneur `.vision-dashboard`. `/flashcards-test` wrappé `.vision-dashboard` pour rester fonctionnel (hors déploiement).
- **UI admin** : pas d'éditeur de parcours existant → nouvelle surface admin ciblée (choisir un parcours **du tenant** → un module → noté/non-noté + jeu de cartes → créer). Endpoint : `POST /api/admin/paths/modules/{moduleId}/flashcards-epreuve` ([Authorize] admin, module→path `TenantId==tenant`, DTO, pas de N+1).

## 5. Corrections / Rapport

### Implémentation (ordre imposé)
1. **Modèle/type (backend)** : réutilise `ContentType="flash_cards"` + `ContentJson` avec `subtype:"epreuve"`, `note:bool`, `cards[]`. **Aucune migration.**
2. **Séquence/progression** : le challenge flashcards s'insère comme les autres (même module, `SortOrder`, complétion binaire via `ChallengeCompletion` → `ProgressCalculationService`).
3. **Scoring** : **réutilise `POST /api/challenges/interactive/{id}/submit-flash-cards`** (branche flip, `{knownCount, total}`) — non modifié. `scorePct=(int)round(100·correct/total)`, `pointsEarned=(int)round(Points·scorePct/100)`. **Noté → `Points=10`** ; **Non-noté → `Points=0`** ⇒ complétion créée (progression avance) et `pointsEarned=0` (aucun impact barème). Arrondi entier identique aux autres exercices — pas de flottant sale.
4. **UI membre** : `EpreuveMode` + `SessionRecap` **convertis en tokens violet `--v-*`** (étaient en vert global `--accent`=#22C55E) + callback `onFinish(correct,total)`. Nouveau wrapper `EpreuveFlashCardsChallenge` (joue l'épreuve dans `.vision-dashboard`, soumet le résultat, gère le retour parcours + état vide). Dispatch player : `flash_cards` + `subtype==="epreuve"` → wrapper (sinon match/flip inchangé). `/flashcards-test` wrappé `.vision-dashboard` (reste fonctionnel).
5. **UI admin** : nouvelle page `/admin/flashcards` (charte violet) — choisir un **parcours du tenant** (`GET /api/admin/paths/editable`) → un module → **noté/non-noté** → **jeu de cartes** (set QCM existant) → `POST /api/admin/paths/modules/{moduleId}/flashcards-epreuve`. `[Authorize] admin`, path `TenantId==tenant` & `!IsCatalog`, DTO, validation (whitelist choix/index), pas de N+1, pas de démo.

### Vérifs
- Build backend **OK** (0/0). Frontend `tsc` **OK**, `npm run build` **OK** (route `/admin/flashcards` générée). Endpoints admin **401** sans auth ; health 200 ; aucune erreur de démarrage.
- **Scoring** garanti par la logique EXISTANTE réutilisée sans modification (arrondi entier `Math.Round`, `UpsertCompletionAsync` best-score + sync progression). Non-noté = `Points=0` ⇒ 0 point, étape complétée.
- **Sécu** : tenant JWT, `[Authorize] admin`, DTO, validation serveur, pas de fallback démo, pas de N+1. Non-admin refusé. Moteur de parcours/scoring **non modifié** (réutilisation pure).
- **Charte** : flashcards épreuve en **violet `--v-*`**, animations juste/faux conservées, `prefers-reduced-motion` respecté, **zéro vert cyber** dans les composants convertis.

### Bugs / limitations (non corrigés, par consigne)
- ⚠️ **Préexistant** (hors périmètre) : `dashboard/challenge/[id]/page.tsx` a une alerte eslint `react-hooks/purity` (`Date.now()` dans `useRef`, l.355) + 2 imports inutilisés (`BriefStep`, `TypeBadge`). **Non introduits ici** ; `npm run build` passe malgré tout. Noté, non corrigé.
- ℹ️ L'UI admin ne cible que les **parcours propres au tenant** (`!IsCatalog`) — le catalogue partagé n'est volontairement pas éditable (effet multi-tenant). Un tenant sans parcours propre voit un état vide.
- ℹ️ Jeux de cartes : seule source réelle = set QCM démo (`flashcards-demo.ts`), proposé comme « jeu existant ». Extensible (`CARD_SETS`).

### Rapport final
Un admin peut **ajouter une épreuve flashcards (noté / non noté)** à un module d'un parcours de son tenant ; un membre la **joue dans le parcours** (EpreuveMode violet, animations juste/faux) avec **progression** correcte ; le **scoring noté** s'intègre via la mécanique existante (arrondi entier propre) et le **non-noté** complète l'étape sans scorer (`Points=0`). 2 builds OK, sécu+charte OK, zéro barème parallèle, aucune migration. **100 % LOCAL**, commit local. STOP.
