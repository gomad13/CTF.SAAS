# FIX_PARAMS_LOG.md — Refonte page « Paramètres entreprise » (charte violet Vision UI)

> Périmètre STRICT : page `/admin/entreprise` (infos entreprise, TenantId + copier, invitations QR, SSO, sécurité, équipes) + son composant `InvitesManager` (utilisé UNIQUEMENT par cette page). Travail 100 % LOCAL.
> Autorité charte : `Instructions.md` **n'existe pas** à la racine → autorité = **CLAUDE.md §2** (charte violet Vision UI), comme les refontes précédentes.

## 0. Backup (méthode §1)
- `backups/params-entreprise-20260707_190228/` : `page.tsx` + `InvitesManager.tsx` (état avant).

## 1. Fichiers
- `ctf-web/src/app/admin/entreprise/page.tsx` (page)
- `ctf-web/src/components/invites/InvitesManager.tsx` (QR — importé seulement par la page)
- ➕ `ctf-web/src/components/vision/VisionForm.tsx` (primitives réutilisables : Section/Field/Input/Textarea/Select/Button/Toggle en tokens `--v-*`)
- `ctf-web/src/app/globals.css` : ajout d'une règle scopée `.vision-dashboard …::placeholder{color:var(--v-text-2)}` (inline ne peut pas cibler `::placeholder`).

## 2. CONSTAT AVANT — couleurs peu lisibles / hors charte

### Problème racine
La page n'est **pas** dans `.vision-dashboard` et utilise les **tokens neutres historiques** (`text-fg-heading/body/muted`, `bg-surface`, `bg-primary`, `text-primary`, `bg-table-head`…). Sur le fond violet nuit du reste du site, ces gris sont **trop sombres → contraste insuffisant / peu lisible**.

### Couleurs à corriger (repérées)
| Emplacement | Avant | Problème | Après (token Vision) |
|---|---|---|---|
| Titres, valeurs, labels | `text-fg-heading` / `text-fg-body` | gris trop sombre | `--v-text` (#FFFFFF) |
| Sous-titres, aides, placeholders | `text-fg-muted` | gris trop sombre | `--v-text-2` (#A0AEC0), AA |
| Cartes | `bg-surface` + `border-border` (plat) | pas verre dépoli | `VisionCard` (surface #111C44/82%, blur, bordure #2A3568) |
| Champs | `bg-surface` (clair) | peu lisible | `--v-surface-2` (#1A2456), texte #FFF, focus ring accent |
| Boutons primaires | `bg-primary text-white` | plat | dégradé `--v-grad` (#7551FF→#582CFF) |
| **Pastille « Copié »** | `text-success` | **= vert cyber #22C55E (INTERDIT)** | `--v-success` (#01B574) |
| **Badge type « Inscription »** (InvitesManager) | `bg-success/10 text-success` | **vert cyber #22C55E** | `--v-success` |
| **Statut « Active »** (InvitesManager) | `bg-success/10 text-success` | **vert cyber #22C55E** | `--v-success` |
| Badge « Application » | `bg-info/10 text-info` | bleu | conservé comme **état info** (autorisé), via token |
| Badge « Rejoindre » / actif | `text-primary` | ok mais neutre | `--v-accent` (#7551FF) |
| Statuts Expirée/Épuisée | `text-fg-muted` / `text-warning` | gris sombre | `--v-text-2` / `--warning` (amber) |
| `ctx.fillStyle="#ffffff"` (canvas QR) | — | **légitime** (fond blanc du PNG QR) | conservé (hors UI) |

### Bugs fonctionnels repérés
- (aucun pour l'instant — à compléter si détecté ; non corrigé par consigne)

## 3. Corrections appliquées

### Lisibilité (tokens)
- Page entière wrappée dans `.vision-dashboard` → tokens `--v-*` actifs (fond #0B1437, texte #FFFFFF).
- Titres/valeurs/labels → `--v-text` (#FFFFFF). Sous-titres/aides/placeholders → `--v-text-2` (#A0AEC0, AA).
- Placeholders : règle scopée `::placeholder{color:var(--v-text-2)}` (inline impossible).
- Remplacement de TOUS les `text-fg-*`, `bg-surface`, `bg-primary`, `text-primary`, `text-success`… par des tokens Vision. **Plus aucun vert cyber #22C55E** (pastilles « Copié », « Inscription », « Active » → `--v-success` #01B574).

### DA / fluidité (cartes verre, champs, boutons)
- Nouveau `components/vision/VisionForm.tsx` : `VisionSection` (carte verre + pastille icône violet), `VisionField`, `VisionInput`/`VisionTextarea`/`VisionSelect` (fond `--v-surface-2`, bordure `--v-border`, texte #FFF, focus ring accent), `VisionButton` (primaire = dégradé `--v-grad` #7551FF→#582CFF ; secondaire/ghost charte), `VisionToggle` (dégradé violet quand actif).
- Page + InvitesManager réécrits avec ces primitives + `VisionCard`. Barre d'enregistrement en verre dépoli sticky. Espacements cohérents, hiérarchie claire, responsive (grilles `sm:`).
- InvitesManager : cartes de type cliquables (hover `v-hover`), panneau QR en `--v-surface-2`, table aux tokens Vision (thead `--v-surface-2`, lignes `v-row` hover), pastilles statut/type via `pill(color)` en tokens.

### Animations (framer-motion, sobres, prefers-reduced-motion)
- Entrée de page : `Reveal` (fade + translate ~350ms). Cartes : `Stagger`/`StaggerItem` (~60ms).
- Hover doux sur cartes/boutons/champs (transitions 150-180ms). Focus ring accent violet sur inputs.
- Bouton « Copier » : check animé **« Copié ! »** (`AnimatePresence`, scale/opacity). Toast d'enregistrement animé. Panneau QR généré : entrée fade+slide.
- `Reveal`/`Stagger` désactivent l'animation si `prefers-reduced-motion`.

### Exceptions volontaires (hors UI, conservées)
- `ctx.fillStyle="#ffffff"` (fond du PNG QR téléchargé) et `background:"#fff"` (fond du QR affiché) : **blancs fonctionnels nécessaires au scan**, pas du theming.

## 4. APRÈS — vérifications
- `npx tsc --noEmit` **OK** · `eslint` (3 fichiers) **0** · `npm run build` **OK** (`/admin/entreprise` compilée).
- Grep : **0** vert cyber #22C55E, **0** token neutre résiduel (`fg-*`/`bg-primary`…), **0** hex en dur hors 2 blancs QR fonctionnels.
- Contraste AA : texte #FFFFFF et #A0AEC0 sur surfaces #0B1437/#111C44/#1A2456 → conforme.
- Périmètre respecté : seuls `page.tsx`, `InvitesManager.tsx` (utilisé uniquement ici), `VisionForm.tsx` (nouveau), `globals.css` (règles scopées `.vision-dashboard`). Backend/auth/logique non touchés. Vraies données (nom, TenantId) préservées.
- Bugs fonctionnels : aucun repéré.

## 5. RAPPORT — STOP
Page **Paramètres entreprise** refondue à la charte violet Vision UI : lisible (tokens, AA), cartes verre dépoli, champs & boutons dégradé, animations sobres (entrée/stagger/hover/focus/feedback « Copié »), `prefers-reduced-motion` respecté. `npm run build` passe, zéro couleur en dur (hors blancs QR fonctionnels), zéro vert cyber. **100 % LOCAL**, commit local. STOP.
