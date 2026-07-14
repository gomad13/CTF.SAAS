# AUDIT_REPONSES_LOG.md — Audit des process de vérification de réponses

> **DIAGNOSTIC UNIQUEMENT — aucune correction appliquée.** 100 % LOCAL.
> Déclencheur : bug « bonne réponse QCM affichée comme fausse ». Objectif : isolé ou systémique ?
> Autorité : `Instructions.md` absent → **CLAUDE.md** (gravité §6). `PROMPT_AUDIT_REPONSES.md` absent → spec inline appliquée.

## 0. Backup (méthode)
- `backups/audit-reponses-20260714_035230/` : `back/` (ChallengeInteractiveController, SubmissionsController), `front/challenges/` (tous les composants), `schema.sql`.

## 1. Cartographie des types de réponse
Comptage local (tenant CyberMed) — `ContentType` :
| Type | Nb | Grading | Base de vérité |
|---|---|---|---|
| multichoice | 39 | serveur | `choices[].is_correct` par **id** |
| free_text | 16 | **IA** | LLM (voir Part B) |
| phishing_ai | 14 | **IA** | LLM + fallback (voir Part B) |
| mailbox | 14 | serveur | `emails[].is_dangerous` par **id** |
| ceo_fraud | 13 | serveur | `choices[].is_correct` par **id** |
| password_quiz | 8 | serveur | `rounds[].choices[].is_correct` par **id** |
| flash_cards (epreuve) | *nouveau* | **client** | `cards[].correctIndex` par **position** |

## 2. PART A — QCM : shuffle, comparaison, affichage

### 2.1 Le shuffle (priorité du prompt)
- `ChallengeInteractiveController.StripSensitiveKeys` (l.773-799) est appelé par `GET /content` (l.179) pour servir le contenu au client. Il :
  1. **strippe** les clés sensibles `SensitiveKeys` (l.21-24) = `is_correct, is_dangerous, red_flags, ai_system_prompt, expected_elements, correct_right_id` ;
  2. **mélange** (Fisher-Yates, l.802-809) tout tableau dont la propriété ∈ `ShuffleKeys` (l.768-771) = **`choices`, `emails`**.
- Commentaire l.766-767 : « validation par id → scoring préservé ». **Vrai UNIQUEMENT pour les types id-based.**

### 2.2 Types id-based (multichoice, mailbox, ceo_fraud, password_quiz) → ✅ SAINS
- Le grading se fait **côté serveur** sur le **ContentJson ORIGINAL** (`ResolveContentJson`, non mélangé), en comparant l'**`id`** sélectionné au flag `is_correct`/`is_dangerous` porté par l'**objet** (submit-multichoice l.573-586 ; submit-mailbox l.~282 ; submit-ceo-fraud l.~230 ; submit-password-quiz l.649-666).
- Le shuffle ne réordonne que l'affichage ; `id↔label↔is_correct` restent liés. Le client soumet des **id** → l'ordre est sans effet.
- **Affichage** (MultichoiceChallenge l.112-124) : `isGood = isCorrect && wasSelected` → vert ; `isBad = !isCorrect && wasSelected` → rouge. Mapping **correct, pas d'inversion**.
- Donnée réelle vérifiée : `{"choices":[{"id":"degraded","is_correct":true,...}]}` → id-based confirmé.
- **Gravité : néant.** Le flag « correcte » **suit** la bonne option (via l'id).

### 2.3 flashcards ÉPREUVE (subtype `epreuve`) → ❌ CASSÉ (CAUSE RACINE)
- Contenu : `cards[].choices[]` + **`correctIndex`** (une **position**), + grading **côté client** dans `EpreuveMode.tsx` (l.29) : `ok = choiceIndex === card.correctIndex`.
- `GET /content` mélange le tableau **`choices` de chaque carte** (propName `choices` ∈ `ShuffleKeys`) **MAIS** `correctIndex` est un **nombre** : il **n'est ni strippé ni re-mappé** (`correct_index`/`correctIndex` **absents** de `SensitiveKeys`).
- Conséquence : le client reçoit des `choices` **mélangés** + un `correctIndex` pointant sur la **position d'avant mélange** → la bonne réponse est identifiée sur la **mauvaise option**. Le highlight vert ET le comptage (client) sont faux.
- **C'est exactement le cas « flag sur position fixe vs option mélangée ».**

#### Test concret (simulation du strip+shuffle réel sur le jeu QCM déployé)
- Carte « mot de passe robuste » (`mdp-fort`, correctIndex=1, bonne réponse « Long, unique par service… ») : après shuffle, l'option en position 1 n'est presque jamais la bonne.
- **1000 tirages × 8 cartes → 74,8 % des sessions** affichent/comptent la bonne réponse comme **FAUSSE** (4 options ⇒ 3/4 de désync). Par carte : 73-76 %.
- `correctIndex` **est bien envoyé au client** (non strippé) et `choices` **est bien mélangé** → désynchronisation certaine.
- **Gravité : ÉLEVÉ** sur ce type (réponses fausses + scores faux), mais **périmètre limité** (voir §4).

## 3. PART B — IA (LLM)
Deux types IA-gradés : **free_text** (Ollama local) et **phishing_ai** (Anthropic Claude). Les deux clampent `score` 0-100 et sont server-authoritative sur le chemin nominal.

### 3.1 free_text — Ollama (`FreeTextEvaluatorService.cs`)
- Prompt avec **barème explicite** (90-100/70-89/50-69/0-49) + garde anti-injection (délimiteurs). Nominal : correct.
- Extraction : `score` défaut **50** si champ manquant (`ParseEvaluation` ~l.201). Accepte number/string, try-catch → fallback.
- **Fallback (Ollama indisponible / saturé >2 concurrents / timeout / JSON invalide)** → `FallbackEvaluation` (word-count) : `<5 mots=20 · <15=45 · <30=65 · ≥30=75`. **Aucune évaluation du fond** → **toute réponse de 30+ mots obtient 75 %** (hors-sujet inclus). Gravité **ÉLEVÉ** quand le fallback est actif.

### 3.2 phishing_ai — Anthropic (`ChallengeInteractiveController` l.316-480 + `AiService.cs`)
- Extraction : `score` défaut **0** si champ manquant (l.395) ; JSON malformé → **HTTP 502, pas de fallback** (l.392) → l'utilisateur doit resoumettre. Incohérence avec free-text (défaut 50).
- **Fallback local** (`EvaluatePhishingAiLocally` l.436-480) déclenché si **clé Anthropic absente** OU erreur API :
  - si **`expected_elements` vide** → score = **longueur seule** `len·100/max(min_chars·2,80)` → **la verbosité seule fait passer** (ex. 55 car. sans mot-clé ≈ 68 %). Gravité **CRITIQUE**.
  - sinon `coverage·75 + lenRatio·25` : couverture partielle + longueur → passe facilement.
- Prompt phishing auto-généré **sans barème explicite de sévérité** → laxisme LLM possible. Gravité **ÉLEVÉ**.

### 3.3 Constat d'activation (aggravant)
- **Clé Anthropic = `REMPLACEZ_PAR_VOTRE_CLE_ANTHROPIC` (placeholder) en local** → **tous les `phishing_ai` sont notés par le fallback laxiste** (pas ponctuel). À **vérifier en prod** : si la clé n'y est pas posée, idem pour les 14 phishing_ai réels.
- free_text : le fallback n'est atteint que si Ollama est down/saturé — sinon barème correct.

## 3bis. Test « réponse clairement fausse » (fallback, déterministe)
| Cas | Type | Score fallback | Attendu | Verdict |
|---|---|---|---|---|
| « je ne sais pas ce que c'est mais voilà » (30+ mots de blabla) | free_text | **75** | faux | ❌ passe |
| « bla bla bla » 55 car., `expected_elements` vide | phishing_ai | **~68** | faux | ❌ passe |
| réponse hors-sujet 60+ car., 0 mot-clé, min_chars 45 | phishing_ai | ~25 (lenRatio) | faux | ✅ échoue (limite) |
→ Le laxisme se manifeste surtout via **longueur/verbosité** et **absence d'`expected_elements`**.

## 4. PART C — Isolé ou systémique ?
- **QCM : ISOLÉ au flashcards ÉPREUVE.** Les 74+ challenges id-based (multichoice/mailbox/ceo_fraud/password_quiz) sont **corrects** — shuffle sans effet, mapping vert/rouge bon.
- Le bug touche **uniquement** le nouveau type `flash_cards/epreuve` (index + grading client). En prod : le parcours de test créé (Prepa Bloc) est concerné ; peu d'autres instances.
- ➡️ **Les scores/analytics globaux ne sont PAS faussés** (hors épreuves flashcards). Pas de correction rétroactive massive nécessaire.
- Part B (IA) : verdict séparé ci-dessous — un problème IA (laxisme) serait, lui, transversal aux 30 challenges free_text/phishing_ai.

## 4bis. PART C — révision du verdict (avec IA)
Deux problèmes **indépendants** :
1. **Bug QCM signalé = ISOLÉ** au `flash_cards/epreuve` (index fixe vs `choices` mélangés). Les 74+ QCM id-based sont **corrects**. Scores/analytics **non faussés** globalement.
2. **Laxisme IA = TRANSVERSAL** aux 30 challenges IA-gradés — mais **uniquement sur le chemin fallback** (systématique pour `phishing_ai` si clé Anthropic absente ; ponctuel pour `free_text`). Là, les scores **sont gonflés** (verbosité). C'est un problème **distinct**, non lié au bug QCM.

## 5. CAUSE RACINE + ÉTAT IA + CORRECTIONS PRIORISÉES (aucune appliquée)

### Cause racine du bug signalé
`GET /content` mélange les tableaux `choices` (`ShuffleKeys`) mais le type `flash_cards/epreuve` identifie la bonne réponse par **`correctIndex` (position fixe, envoyée telle quelle au client)** au lieu d'un flag `is_correct` porté par l'objet. Après mélange, `correctIndex` pointe la mauvaise option → **~75 % des épreuves affichent/comptent la bonne réponse comme fausse**. Grading **côté client** de surcroît.

### État IA
Fonctionnel en nominal (barème free-text correct). **Laxiste en fallback** : verbosité récompensée (free-text 75 % à 30 mots ; phishing longueur-seule sans `expected_elements`). **En local (et peut-être prod) la clé Anthropic manque → phishing_ai 100 % en fallback laxiste.**

### Corrections priorisées (à valider AVANT toute passe de correction)
| # | Prio | Correction proposée | Fichier(s) |
|---|---|---|---|
| 1 | **ÉLEVÉ** | **Fix QCM épreuve** — option recommandée : aligner l'épreuve sur le modèle **id-based** (flag `is_correct` par choix + grading **serveur** comme multichoice), supprimant à la fois la désync du shuffle ET le grading client. Alternatives + rapides : (b) exclure les `choices` d'épreuve du `ShuffleKeys`, ou (c) re-mapper `correctIndex` après shuffle côté serveur. | `ChallengeInteractiveController` (StripSensitiveKeys/submit), `EpreuveMode`, `EpreuveFlashCardsChallenge` |
| 2 | **CRITIQUE** | **Durcir le fallback phishing** : refuser le score longueur-seule ; exiger `expected_elements` ; réduire le poids longueur ; définir le comportement si Anthropic non configurée (bloquer plutôt que noter laxiste). **Vérifier/poser la clé Anthropic en prod.** | `ChallengeInteractiveController.EvaluatePhishingAiLocally`, config `Anthropic:ApiKey` |
| 3 | **ÉLEVÉ** | **Durcir le fallback free-text** : ne pas donner 75 % sur simple longueur ; intégrer une vérif de mots-clés/`expected_elements`. | `FreeTextEvaluatorService.FallbackEvaluation` |
| 4 | **ÉLEVÉ** | **Barème explicite** dans le prompt phishing auto-généré (règles de sévérité). | `ChallengeInteractiveController` (prompt phishing) |
| 5 | MOYEN | Harmoniser le **défaut de score manquant** (free-text 50 vs phishing 0) ; choisir une valeur cohérente et sûre. | 2 endpoints |
| 6 | FAIBLE | phishing JSON malformé → fallback gracieux au lieu de 502. | submit-phishing-ai |

### Périmètre de correction rétroactive
- QCM : purger/rejouer uniquement les complétions d'**épreuves flashcards** (peu nombreuses — parcours de test). Rien d'autre.
- IA : si le fallback laxiste a tourné en prod (clé Anthropic absente), les scores `phishing_ai` (et free_text en cas d'indispo Ollama) sont **surévalués** → à réévaluer après durcissement.

## 6. STOP (diagnostic)
Diagnostic terminé. En attente de validation → **validée**, corrections appliquées ci-dessous.

## 7. CORRECTIONS APPLIQUÉES (passe suivante, validée) — LOCAL, backend uniquement
| # | Correction | Fichier | Test |
|---|---|---|---|
| **1** | **Bug QCM épreuve** : `StripSensitiveKeys` ne mélange `choices`/`emails` que si ce sont des **tableaux d'OBJETS** (id-based, sûrs). Les tableaux de **primitives** (épreuve : `choices`=strings via `correctIndex`) ne sont **plus mélangés** → `correctIndex` reste valide. | `ChallengeInteractiveController.cs` (case Array) | Simu strip+shuffle : **0 % désync** sur l'épreuve (vs 74,8 %) ; multichoice **mélange toujours** (24 ordres) + `is_correct` strippé → aucune régression. |
| **2** | **Fallback phishing durci** : score = **couverture** des `expected_elements` ; la **longueur ne donne plus de points** (garde uniquement) ; sans référentiel → **40 max** (jamais passant). | `EvaluatePhishingAiLocally` | Sim : hors-sujet 0 mot-clé → **0** (avant ~25) ; sans référentiel → **40** (avant ~68) ; bonne (3/4) → 75. |
| **3** | **Fallback free-text durci** : idem — couverture des `expected_elements` (propagé au fallback), longueur = garde seule, sans référentiel → 40. | `FreeTextEvaluatorService.FallbackEvaluation` | Même logique que #2 (couverture-driven). |
| **4** | **Barème STRICT explicite** ajouté au prompt phishing auto-généré (« longueur seule = 0 point », « hors-sujet < 40 »). | `SubmitPhishingAi` (systemPrompt) | Revue de prompt. |
| **5** | **Score manquant → fallback** (au lieu de défauts arbitraires 50/0). free-text et phishing retombent sur la couverture. | 2 emplacements | Code : plus de `score=50`/`:0` par défaut. |
| **6** | **JSON IA malformé phishing → fallback gracieux** (au lieu de HTTP 502). | `SubmitPhishingAi` (parse) | Code : `try/catch` + `aiUsable`. |

**Non modifiés** : le moteur QCM id-based (déjà correct), le grading nominal (barèmes corrects), le frontend (`EpreuveMode` reçoit désormais un `correctIndex` valide car le serveur ne mélange plus les strings).
**Vérifs** : build backend **OK** (0/0) ; health 200 ; endpoints 401 ; aucune erreur de démarrage ; tests concrets ci-dessus.
**À faire (config, hors code)** : **poser la clé `Anthropic:ApiKey` en prod** — sinon les 14 `phishing_ai` restent notés par le fallback (désormais durci, mais l'IA reste préférable). **Rétroactif** : réévaluer les complétions d'**épreuves flashcards** (peu) et, si le fallback laxiste a tourné en prod avant ce fix, les `phishing_ai` surévalués.
**100 % LOCAL** — pas encore déployé.
