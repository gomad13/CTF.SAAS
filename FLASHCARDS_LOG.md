# FLASHCARDS_LOG.md — Menu de test isolé `/flashcards-test` (LOCAL uniquement)

> Périmètre STRICT : nouvelle route isolée, données démo en dur, **aucun** parcours réel / scoring / auth / backend touché.
> 100% LOCAL : aucun déploiement, aucun push, aucune modif serveur.
> Charte racine (CLAUDE.md) = AUTORITÉ : noir/gris/blanc + vert cyber, tokens CSS, zéro hex en dur, WCAG AA, `prefers-reduced-motion` respecté.
> Backup avant modif : `backups/flashcards-20260705/frontend.tgz` (ctf-web hors node_modules/.next).

---

## PLAN

### Fichiers créés (une responsabilité par fichier)
| Fichier | Rôle |
|---|---|
| `ctf-web/src/lib/flashcards-demo.ts` | Type `Flashcard` + jeu de cartes cyber DÉMO en dur (front/back + choix QCM) |
| `ctf-web/src/components/flashcards/useSessionTimer.ts` | Hook chrono de session (mm:ss), start/stop/reset |
| `ctf-web/src/components/flashcards/FlipCard.tsx` | Carte à retournement 3D recto/verso (animation FLIP) |
| `ctf-web/src/components/flashcards/RevisionMode.tsx` | Mode Révision (flip libre + nav précédente/suivante) |
| `ctf-web/src/components/flashcards/EpreuveMode.tsx` | Mode Épreuve (QCM + JUSTE/FAUX + timer + score) |
| `ctf-web/src/components/flashcards/SessionRecap.tsx` | Récap de fin (score, temps, bonnes/mauvaises, rejouer) |
| `ctf-web/src/components/flashcards/FlashcardsMenu.tsx` | Menu d'accueil (choix des 2 modes) |
| `ctf-web/src/app/flashcards-test/page.tsx` | Orchestrateur état (`menu` \| `revision` \| `epreuve`) |

### Ordre d'exécution
1. ✅ Backup + plan (ce fichier).
2. Structure : route + menu + cartes démo + type.
3. Mode Révision + animation FLIP (rotateY 3D, perspective, backface-hidden, ~450ms).
4. Mode Épreuve + JUSTE (pulse/rebond vert + check) + FAUX (shake + flash rouge + croix) + timer + récap.
5. Polissage : charte tokens, responsive, focus clavier, `prefers-reduced-motion`.

### Décisions d'animation (framer-motion)
- **FLIP** : `motion.div` `animate={{ rotateY: flipped ? 180 : 0 }}`, `transformStyle: preserve-3d`, deux faces `backfaceVisibility: hidden` (verso `rotateY(180deg)`), conteneur `perspective`. ~0.5s `easeInOut`.
- **JUSTE** : keyframes `scale [1,1.06,1]` + overlay glow `var(--accent)` (opacité in/out) + icône `CheckCircle2` accent animée.
- **FAUX** : keyframes `x [0,-8,8,-6,6,0]` (shake) + overlay flash `var(--danger)` + icône `XCircle` danger animée.
- **reduced-motion** : `useReducedMotion()` → flip/scale/shake désactivés (bascule instantanée + apparition d'icône), feedback couleur conservé (accessibilité).

### Couleurs
- 100% tokens : `--bg`, `--surface`, `--surface-2`, `--border`, `--text`, `--text-2`, `--text-3`, `--accent`/`--on-accent`/`--accent-subtle`, `--danger`/`--danger-subtle`/`--danger-t`, `--success`. Vert cyber = bonne réponse, rouge danger = mauvaise. **Aucun hex en dur.**

### Points de vérification
- `npm run build` local passe. Grep hex = 0 dans les fichiers flashcards. Contraste AA. Focus visible + navigation clavier. Timer mm:ss dans le récap.

---

## JOURNAL D'EXÉCUTION

1. ✅ Backup `backups/flashcards-20260705/frontend.tgz` (5,5 Mo, hors node_modules/.next).
2. ✅ Données démo `lib/flashcards-demo.ts` : 8 cartes cyber (phishing, 2FA, ransomware, mot de passe, arnaque au président, Wi-Fi public, MAJ sécurité, signalement) avec QCM 4 choix.
3. ✅ Hook `useSessionTimer` (mm:ss, start/stop selon `running`, `reset`).
4. ✅ `FlipCard` : flip 3D `rotateY` (perspective 1200, `preserve-3d`, `backfaceVisibility: hidden`), bouton focusable clavier (Entrée/Espace), `aria-pressed`.
5. ✅ `RevisionMode` : flip libre + nav précédente/suivante + compteur, boutons Retourner/Suivante.
6. ✅ `EpreuveMode` : QCM, feedback **JUSTE** (pulse `scale [1,1.06,1]` + glow `--accent` + `CheckCircle2`) / **FAUX** (shake `x [0,-8,8,-6,6,0]` + flash `--danger` + `XCircle`), chrono visible, barre de progression, score, explication + bouton Suivante/Voir le résultat.
7. ✅ `SessionRecap` : trophée, score `CountUp`, justes/faux/temps, boutons Menu / Rejouer.
8. ✅ `FlashcardsMenu` : accueil (2 cartes de choix, hover, stagger d'entrée).
9. ✅ `app/flashcards-test/page.tsx` : orchestrateur d'état `menu | revision | epreuve` (remount de l'épreuve via `key` → chrono/score réinitialisés).
10. ✅ `prefers-reduced-motion` géré partout (`useReducedMotion` → flip/scale/shake désactivés, feedback couleur + icône conservés).

**Note d'environnement (non bloquante) :** le build local échouait sur `tw-animate-css` — dépendance **déjà déclarée** dans `package.json` (`^1.4.0`) mais absente du `node_modules` local (désynchronisé). Résolu par `npm install` (setup local, `package.json`/`package-lock.json` **inchangés**). Aucune modif serveur.

---

## RAPPORT FINAL — passe Flashcards TERMINÉE ✅

### Critère d'arrêt — atteint
- [x] Route `/flashcards-test` accessible en local (générée au build : `○ /flashcards-test`).
- [x] 2 modes fonctionnels : **Révision** (flip libre) + **Épreuve** (QCM scoré).
- [x] 3 animations en place et fluides : **FLIP** (retournement 3D), **JUSTE** (pulse/rebond vert + check), **FAUX** (shake + flash rouge + croix).
- [x] Timer de session mm:ss visible en épreuve + présent dans le récap de fin.
- [x] `npm run build` (local) → **✓ Compiled successfully**.
- [x] **Zéro hex en dur** dans les fichiers flashcards (grep = 0) — tout en tokens.
- [x] Contraste AA (tokens charte), focus visible + navigation clavier, `prefers-reduced-motion` respecté.

### Comment tester
1. `cd ctf-web && npm run dev`
2. Ouvrir `http://localhost:3000/flashcards-test`.
3. **Révision** : cliquer la carte (ou Entrée/Espace) pour la retourner ; Précédente/Suivante.
4. **Épreuve** : répondre au QCM → animation juste/faux + chrono qui tourne ; en fin → récap (score, temps, rejouer).
5. Tester en mode clair/sombre (toggle thème) et avec « réduire les animations » activé dans l'OS.

### Périmètre respecté
Route 100% **isolée**, données démo en dur, **aucune** table BDD, **aucun** parcours réel / scoring / auth / backend touché. Aucun déploiement, aucun push, aucune modif serveur.

## Bugs fonctionnels repérés ailleurs (NOTÉS, non corrigés)
(aucun rencontré durant cette passe)
