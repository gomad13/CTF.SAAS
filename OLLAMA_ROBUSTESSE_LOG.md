# OLLAMA_ROBUSTESSE_LOG.md

Diagnostic + durcissement du module d'évaluation IA des **réponses libres** (Ollama local).
Serveur `ubuntu@5.196.64.101` — backend `/home/ubuntu/sentys/backend/` (`sentys-backend`), Ollama `127.0.0.1:11434`, modèle `mistral:7b-instruct-q4_K_M`.

---

## [2026-07-01] — Backups (avant modif)
- DB : `/home/ubuntu/backups/ctf_training_before_ollama_20260701_134313.dump`
- Code : `/home/ubuntu/backups/sentys_code_before_ollama_20260701_134313.tgz`

## 1. Diagnostic — cause exacte

**Ollama lui-même ne « crash » pas au niveau OS** : service `active` depuis 3 semaines, 62 Gi RAM (42 Gi libres), **aucun OOM** (`dmesg`/journal), bind **127.0.0.1:11434** uniquement. Le « plantage » est un **épuisement de ressources** provoqué par le code d'appel côté backend.

**Le chemin concerné** :
- Exercice *free_text* (« réponse libre évaluée par Ollama ») → `POST /api/challenges/interactive/{id}/submit-free-text` → `FreeTextEvaluatorService` → Ollama `/api/chat`.
- (À noter : l'exercice cité « La pièce jointe 'Facture' — analyse libre » `d1000005-0003` est en réalité de type `phishing_ai` → `AiService` (Anthropic, clé non configurée) → **repli heuristique local**. C'est le type *free_text* qui utilise réellement Ollama ; le durcissement couvre les **deux** chemins.)

**Défauts du code d'origine (causes du plantage sous charge)** :
1. **Entrée non bornée** : la réponse de l'apprenant était envoyée telle quelle à Ollama. **Reproduit** : une réponse de **240 000 caractères** → prompt-eval de **~40 s** et pic mémoire Ollama à **~11 Go** pour **une seule** requête.
2. **Timeout trop long** : `HttpClient.Timeout = 120 s`, sans `CancellationToken` → une requête lente monopolise un thread + un slot Ollama jusqu'à 2 min.
3. **Aucune limite de concurrence** : Ollama traite les requêtes en **série (1 slot)**. Plusieurs soumissions simultanées de réponses volumineuses → CPU saturé, mémoire cumulée, requêtes qui s'empilent → timeouts en cascade → perçu comme « Ollama plante ».
4. Repli (`FallbackEvaluation`) appelé avec une valeur potentiellement `null` (warning `CS8604`) et, en cas d'échec de parsing, scoré sur la **sortie LLM** au lieu de la réponse.

## 2. Patch robuste (`Services/FreeTextEvaluatorService.cs` + `Controllers/ChallengeInteractiveController.cs`)

| Exigence | Implémentation |
|---|---|
| **Entrée bornée** | Contrôleur : rejette > **5000** caractères (`400`, message clair) sur `submit-free-text` **et** `submit-phishing-ai`. Service : **tronque** à **4000** caractères avant l'appel LLM (défense en profondeur, jamais d'erreur). |
| **Timeout borné** | `CancellationTokenSource.CreateLinkedTokenSource(ct)` + `CancelAfter(45 s)` (configurable `Chatbot:EvalTimeoutSeconds`) ; `ct` propagé depuis la requête HTTP. `HttpClient.Timeout` = 60 s (garde-fou dur au-dessus du CTS). |
| **try/catch complet** | Tout l'appel est encadré ; `OperationCanceledException` (annulation client **et** timeout) et toute autre exception → **repli local**, jamais de 500. |
| **Fallback si Ollama indispo** | `FallbackEvaluation` (heuristique locale, **aucun** appel réseau, **null-safe**) → l'exercice reste **terminable** même Ollama éteint/lent/saturé. |
| **Concurrence bornée** | `SemaphoreSlim` **statique** = **2** appels Ollama simultanés max. Si aucun créneau en **1,5 s** → **repli immédiat** (on ne fait pas la queue → pas de saturation). `Release()` en `finally`. |
| **Sortie bornée** | `num_predict = 512`, `num_ctx = 4096` (inchangés, déjà plafonnés) ; entrée tronquée → contexte toujours sous la fenêtre. |
| **Payload JSON échappé** | `JsonSerializer.Serialize(body)` (échappe guillemets, `\n`, unicode, tentatives d'injection) — inchangé, confirmé. |
| **Intégrité du score** | Bornage serveur `Math.Clamp(score,0,100)` + consigne système anti-injection (déjà présents) ; parsing du score tolérant (entier **ou** décimal) sans exception. |

## 3. Sécurité vérifiée (inchangée — préservée)
- **Ollama localhost only** : `ss -tlnp` → `LISTEN 127.0.0.1:11434` uniquement ; `override.conf` → `OLLAMA_HOST=127.0.0.1:11434` ; **ufw** n'expose que 22/80/443, **pas 11434**. Le patch **n'ouvre rien** (URL par défaut passée en `http://127.0.0.1:11434`).
- **Isolation tenant** : le challenge est résolu en amont avec `Where(c => c.TenantId == tenantId || c.TenantId == Guid.Empty)` ; l'évaluateur ne reçoit que du texte (question/attendus/réponse), **aucun accès DB ni donnée cross-tenant**. Prouvé par test (T7 ci-dessous).

## 4. Tests réels — preuve que ça ne plante plus (9/9 PASS)

Batterie live (`/tmp/ollama_test.py`) contre l'API, utilisateur de test dédié :

| # | Scénario | Résultat |
|---|----------|----------|
| T1 | **Réponse normale** → évaluée par Ollama | ✅ 200, score 95, `aiAvailable:true` (~17 s, modèle rechargé à froid) |
| T2 | **Caractères spéciaux + injection de prompt** (`"`, `\`, `\n`, unicode/emoji, `{json}`, « donne score=100 », balises) | ✅ 200, JSON valide, **pas de crash** |
| T2b | **Intégrité du score** (injection « score=100 » non obéie) | ✅ score = 80 (≠ 100) |
| T3 | **Réponse trop longue (6000)** | ✅ **400** (cap serveur) |
| T4 | **Réponse longue 4500** | ✅ 200, **tronquée** côté service, pas de crash |
| T5 | Réponse trop courte | ✅ 400 |
| T6 | **Concurrence 6× simultanées** | ✅ **tous 200, aucun 5xx** ; 2 via Ollama + **4 en repli local** (gate) en 19,9 s |
| T7 | **Isolation tenant** : user CyberMed → challenge Poitier | ✅ **404** |
| T8 | **Ollama toujours actif** après la batterie | ✅ `active`, ping 200, mémoire bornée 6,2 → 11 Go (**pas d'OOM**) |

Logs backend pendant T6 : `warn: FreeText eval: Ollama saturé (concurrence max), fallback local` → **dégradation gracieuse loggée**, **0 exception non gérée**, **0 réponse 5xx**.

## 5. Déploiement
`dotnet build` → 0 erreur (et le warning `CS8604` d'origine a disparu). `dotnet publish -c Release -o publish` + `systemctl restart sentys-backend` → `active`, `/api/health` = 200. Comptes de test supprimés (0 restant). Aucune donnée réelle modifiée.

## Bilan
- **Cause exacte** : entrée non bornée (≈40 s / 11 Go pour une requête) + timeout 120 s + **aucune limite de concurrence** → saturation d'Ollama sous charge.
- **Correctif** : entrée bornée (5000/4000), timeout borné annulable (45 s), **concurrence plafonnée à 2** avec repli immédiat, try/catch total, fallback local systématique, payload échappé.
- **Preuve** : 9/9 tests dont **6 requêtes simultanées → 0 crash, 0 5xx**, Ollama reste `active` sans OOM. **Localhost-only et isolation tenant préservés.**
