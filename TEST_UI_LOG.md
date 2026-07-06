# TEST_UI_LOG.md — Refonte Vision UI violet : Dashboard + Sidebar (LOCAL uniquement)

> Charte AUTORITÉ : thème unique bleu nuit / violet Vision UI (CHARTE_VIOLET_SECTION_2.md).
> Fond `#0B1437`, surfaces `#111C44`/`#1A2456`, bordures `#2A3568`, texte `#FFFFFF`/`#A0AEC0`, accent `#7551FF`→`#582CFF`, dégradés violet→cyan (`#582CFF`→`#2CD9FF`).
> Périmètre STRICT : **(1) dashboard principal, (2) sidebar gauche**. Aucun autre écran, aucun backend/auth/IA/scoring. 100% LOCAL, pas de push.
> Backup : `backups/test-ui-20260706/frontend.tgz`.

## Plan
1. **Dashboard** (`app/dashboard/page.tsx`) : **déjà refondu** au style Vision UI violet (passe précédente, thème scopé `.vision-dashboard`, composants `components/vision/*`, vraies données). Rien à refaire — vérifié conforme.
2. **Sidebar** (`components/Sidebar.tsx`) — à refondre :
   - Ajouter une classe scopée `.vision-theme` sur le `<aside>` : elle **redéfinit les tokens standard** (`--bg/--surface/--surface-2/--border/--text/--text-2/--text-3/--accent/--accent-hover/--accent-subtle/--on-accent/--danger…`) aux **valeurs violettes officielles**. Le sidebar devient violet via ses classes/tokens existants ; les autres pages (sans `.vision-theme`) restent inchangées.
   - **Item actif** = pastille/fond **dégradé violet** (`#7551FF`→`#582CFF`) + icône + texte blancs + ombre douce (au lieu du fond subtle + barre gauche).
   - Items inactifs : texte secondaire `--text-2/--text-3`, hover doux (surface qui s'éclaircit).
   - Corriger les couleurs en dur : glow logo vert `rgba(34,197,94,.4)` → violet ; `color="white"` (icône) → token via `currentColor` ; hover déconnexion `rgba(239,68,68,…)` → tokens danger.
3. Tokens violets définis **dans `globals.css`** (bloc `.vision-theme`, scopé) — seul endroit des hex (définition de tokens) ; le JSX n'utilise que `var(--*)`.

## Animations
Hover doux items (transition surface/couleur ~150ms), focus visible (ring accent violet, hérité via `--accent`), `prefers-reduced-motion` respecté (géré globalement). Dashboard : Reveal/Stagger/CountUp déjà en place.

## Avant → Après (sidebar)
- Avant : sidebar thème vert (tokens verts), item actif fond vert subtle + barre gauche verte, glow logo vert.
- Après : sidebar bleu nuit `#0B1437`, item actif **pill dégradé violet** + icône blanche + ombre, logo glow violet, hover doux. Contraste AA (blanc/`#A0AEC0`).

## Critère d'arrêt
Dashboard + sidebar au style Vision UI violet, `npm run build` OK, zéro hex en dur (JSX), contraste AA, reduced-motion OK, rendu local correct. → écrire "DASHBOARD + SIDEBAR PRETS" et s'arrêter (aucune autre page).

## Journal
1. ✅ Backup `backups/test-ui-20260706/frontend.tgz`.
2. ✅ `globals.css` : ajout du bloc scopé **`.vision-theme`** qui redéfinit les tokens standard (`--bg/--surface/--surface-2/--border/--text/--text-2/--text-3/--accent/--accent-hover/--on-accent/--accent-subtle/--accent-border/--danger*/--success*/--warning*/--info`) aux valeurs **violettes officielles** (section 2). Scopé → n'affecte que son sous-arbre.
3. ✅ `Sidebar.tsx` :
   - Classe `vision-theme` sur le `<aside>` → tout le sidebar passe en violet via ses tokens existants.
   - **Item actif** → pill **dégradé violet** (`linear-gradient(135deg, var(--accent), var(--accent-hover))`) + icône/texte blancs (`--on-accent`) + ombre douce ; items inactifs `--text-3`, hover doux (surface + léger `translateX`).
   - Couleurs en dur corrigées : glow logo vert `rgba(34,197,94,.4)` → `color-mix(var(--accent))` ; `color="white"` (icône Shield) → `currentColor` hérité de `--on-accent` ; hover déconnexion `rgba(239,68,68,…)` → `color-mix(var(--danger))` / `var(--danger-subtle)`.
   - Reste seulement le scrim mobile `rgba(0,0,0,.5)` (overlay neutre, pas une couleur de thème).
4. ✅ `dashboard/page.tsx` : **déjà** au style Vision UI (passe précédente) — inchangé, vérifié conforme.
5. ✅ Aperçu public `dashboard-preview` mis à jour avec une **réplique statique du sidebar violet** (les vrais hooks du Sidebar déclencheraient un 401→/login sur une page sans auth) → permet de valider dashboard **+** sidebar sans se connecter. Route + allowlist `proxy.ts` = outils de validation (non commités).

## Vérifications
- [x] `npm run build` (local) → **✓ Compiled successfully**.
- [x] **Zéro hex en dur** dans le JSX (sidebar + dashboard + composants vision) ; hex seulement dans les définitions de tokens (`globals.css`).
- [x] Contraste AA (blanc / `#A0AEC0` sur `#0B1437`/`#111C44`) ; focus visible (ring `--accent` violet, hérité dans `.vision-theme`) ; `prefers-reduced-motion` respecté (règle globale + Reveal/Stagger/CountUp + `isAnimationActive` charts).
- [x] Aperçu `/dashboard-preview` → HTTP 200, rendu dashboard + sidebar violet.
- [x] Aucune autre page / backend touché. Le sidebar étant partagé, il apparaît en violet partout où il s'affiche (conséquence assumée d'un composant unique) ; les **contenus** des autres pages restent inchangés.

## Bugs fonctionnels repérés ailleurs (notés, non corrigés)
(aucun rencontré durant cette passe)

## DASHBOARD + SIDEBAR PRETS — en attente de validation avant d'appliquer au reste du site
> Ne rien répliquer ailleurs tant que l'utilisateur n'a pas validé. Décision à prendre : étendre le thème violet au reste (topbar, autres pages) — à ce moment, on pourra promouvoir `.vision-theme` en `:root` global (comme le prévoit la charte section 2) plutôt que de le laisser scopé.
