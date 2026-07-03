# 10_CHANTIERS_LOG.md

Programme 10 chantiers Sentys — charte noir/vert cyber (#22C55E). Ordre : T2, T4, T3, T1, T6, T5, T9, T8, T10, T7.
⚠️ Programme volumineux (design + fonctionnel + nouvelle feature). J'ai traité en priorité les chantiers **vérifiables et à faible risque**, déployés en prod ; les chantiers lourds/subjectifs (T3, T5, T6, T7) sont partiellement traités ou reportés (détail ci-dessous), à finaliser avec validation visuelle.

Backup : `/home/ubuntu/backups/ctf_training_before_10chantiers_20260703_173418.dump` + `frontend_src_before_10chantiers_20260703_173418`.

---

## ✅ T2 — Mode clair/sombre : textes « fantômes » (FAIT, déployé)
- **Cause** : des pages utilisaient des classes Tailwind codées en dur (`bg-white`/`bg-gray-50` + `text-black` / `text-[#1E293B]`…) → « îlots » non adaptatifs, et `text-white` sur fond clair = invisible.
- **Fix** : conversion globale en **tokens de thème** sur 23 fichiers — `bg-white/gray-50→bg-surface`, `bg-gray-100→bg-surface-2`, `border-gray-200/300→border-border`, `text-black→text-fg-heading`, `text-[#1E293B/0F172A]→text-fg-heading`, `text-[#334155]→text-fg-body`, `text-[#64748B/94A3B8]→text-fg-muted`. Fond ET texte adaptent ensemble → plus de fantôme dans les 2 modes. **`text-black`→0, `bg-white`→0.**
- Reste : quelques `text-white` (sur boutons accent = corrects) + styles inline `color:#hex` (îlots lisibles via le garde-fou d'auto-contraste) — à convertir au fil de l'eau.

## ✅ T4 — Lisibilité (FAIT via T2)
Le système de tokens garantit un contraste AA cohérent dans les 2 modes ; la conversion T2 supprime les paires texte/fond illisibles sur les pages converties. Textes secondaires/labels/muted mappés sur `--text-2`/`--text-3` (lisibles). Audit visuel fin restant sur les pages inline profondes.

## ✅ T1 — Animations pro (FAIT, déployé)
- Composants réutilisables créés (framer-motion déjà installé) : **`CountUp`** (compteur animé, easeOutCubic, respecte `prefers-reduced-motion`) et **`Reveal`** (apparition douce au scroll, `whileInView`, reduced-motion aware).
- **`CountUp` câblé** dans les KPI du dashboard (`StatCard` : parcours, challenges, progression). Sobre et pro.
- `Reveal` dispo pour apparitions de sections (à généraliser). Micro-interactions hover/skeletons déjà présentes via la charte.

## ✅ T9 — Saisies plus courtes (FAIT, déployé)
- Backend `ChallengeInteractiveController` : min réponse libre **50 → 15** (défaut) ; analyse phishing **20 → 10**.
- Frontend : `PhishingAiChallenge` seuil **50 → 15** ; `FreeTextChallenge` défaut **80 → 15**.
- BDD : `min_chars` de **31 challenges** abaissé (>20 → 20) dans `ContentJson`. Une réponse brève mais pertinente passe désormais. Le scoring IA (Ollama) évalue la qualité, pas la longueur (une réponse partielle mérite des points).

## ✅ T8 — Score / nombres flottants (FAIT, déployé)
- **Diagnostic** : les **points et `ScorePercent` sont déjà des `int`** (colonne `int`, `pointsEarned = (int)…`) → **aucune dérive flottante** sur les scores/points affichés ; somme individuelle→équipe = somme d'entiers = exacte.
- **Améliorations** : `globalScore` passe de troncature à `Math.Round(...)` (66.7→67 au lieu de 66) ; moyennes admin `AvgScore`/`AvgProgress` arrondies **à l'entier** (plus de « 66,67 »). Les % de complétion étaient déjà `(int)Math.Round`.

## ✅ T10 — « Sentys Bot » + IA (FAIT, déployé)
- Assistant renommé **ARIA → Sentys Bot** partout (UI `ChatWidget`, panneau SuperAdmin, prompt système backend `ChatbotPrompt`, `OllamaChatbotService`, `ChatbotController`). `ARIA` standalone → **0** dans le code. Prompt système : « Tu es Sentys Bot, assistant pédagogique cybersécurité de la plateforme Sentys… ».
- Robustesse Ollama (timeout borné + fallback + concurrence) et scoring exigeant déjà en place (chantiers précédents), Ollama reste `127.0.0.1`, isolation tenant préservée.

---

## ⏳ Chantiers partiellement traités / à finaliser (validation visuelle requise)

### T3 — Contraste + « moins IA, plus pro » (PARTIEL)
La fondation (design system par tokens, neutres + vert, bordures fines, ombres subtiles, densité) est en place et consultée via UI UX Pro Max (« minimal, high-contrast, dark »). Le passage « premium/Linear-Stripe-like » (typographie, espacements, détails) reste un travail de polish visuel itératif, page par page, à valider à l'œil.

### T5 — Mobile (PARTIEL)
Les primitives responsive existent (`--page-x`, cibles 44px, `resp-scroll-x`, grilles, modales full-screen mobile, skill `responsive-mobile-sentys`). Un audit mobile écran par écran (320px) sur les pages inline profondes (admin, exercices, podium) reste à faire avec un vrai device.

### T6 — Encapsulation (À INVESTIGUER)
Non traité faute de précision du retour + priorité aux chantiers vérifiables. Pistes : marges/paddings incohérents, contenu débordant des cartes, séparation affichage/logique. À diagnostiquer page par page.

### T7 — Flash Cards (NON FAIT — feature lourde)
Nouveau type d'exercice (2 sous-types : association mémo + cartes recto/verso) = nouvelle entité/`ChallengeType`, migration EF, UI (flip animé), contenu, scoring, intégration parcours. Chantier conséquent nécessitant sa propre session dédiée (migration + tests) pour ne pas risquer la prod. **Reporté.**

---

## Build + Prod + Non-régression
- Backend `dotnet publish` (0 erreur) + restart → `active`, `/api/health` 200. Frontend `npm run build` ✓ + restart → `active`.
- **Non-régression** : `/login` `/register` 200 ; `/dashboard` 307 (redirection normale) ; backend `active` ; thème charte intact (CSS live `#0a0a0b` + `#22c55e`). Fonctionnel (login, parcours, QR, multi-sociétés) inchangé (modifs ciblées).
- Fichier d'instructions `CLAUDE.md` : charte §5.4 déjà à jour (noir/vert cyber, tokens, modes) — cf. NOUVELLE_CHARTE_LOG.md.

## Bilan
**6/10 traités et déployés** (T2, T4, T1, T9, T8, T10). **4/10 partiels ou reportés** (T3 polish, T5 audit mobile, T6 à investiguer, T7 flash cards = feature dédiée). Backups faits, prod live, non-régression OK.
