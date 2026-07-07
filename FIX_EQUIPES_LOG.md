# FIX_EQUIPES_LOG.md — Refonte page « Gestion des équipes » (charte violet Vision UI)

> Périmètre STRICT : page `/admin/teams` (titre, recherche, tableau équipes : pastille couleur / membres / parcours / compliance / actions Voir-Éditer-Supprimer, bouton Nouvelle équipe, formulaire de création, section « Membres sans équipe »). Travail 100 % LOCAL.
> Autorité charte : `Instructions.md` **n'existe pas** → autorité = **CLAUDE.md §2** (charte violet Vision UI), comme les refontes précédentes.
> `TeamEditModal` **non touché** (partagé avec la page détail `/admin/teams/[id]`, hors périmètre).

## 0. Backup (méthode §1)
- `backups/equipes-20260707_201950/page.tsx` (état avant).

## 1. Fichiers touchés
- `ctf-web/src/app/admin/teams/page.tsx` (page)
- `ctf-web/src/app/globals.css` : ajout token `--v-warning:#FFB547` (alerte, absent du thème) + classes scopées `.v-act` / `.v-act-danger` (hover boutons d'action).
- Réutilise `components/vision/VisionForm.tsx` + `VisionCard` (créés à la refonte Paramètres entreprise) + `CountUp`.

## 2. CONSTAT AVANT — couleurs peu lisibles / hors charte
Page **hors** `.vision-dashboard`, sur tokens neutres historiques → gris trop sombre sur fond violet.

| Emplacement | Avant | Problème | Après (token Vision) |
|---|---|---|---|
| Titres, noms d'équipe, chiffres | `text-fg-heading` | gris trop sombre | `--v-text` (#FFFFFF) |
| Sous-titre, placeholder, méta | `text-fg-muted` / `text-fg-body` | gris trop sombre | `--v-text-2` (#A0AEC0), AA |
| Carte/tableau | `bg-surface` + `border-border` plat | pas verre | `VisionCard` / conteneur verre (surface #111C44, bordure #2A3568) |
| En-têtes colonnes | `bg-table-head` | — | `--v-surface-2` (#1A2456), texte `--v-text-2` |
| Recherche + champs création | `bg-surface` clair | peu lisible | `VisionInput` (surface-2, texte #FFF, focus ring accent) |
| Bouton « Nouvelle équipe » / « Créer » | `bg-primary` plat | — | `VisionButton` dégradé `--v-grad` (#7551FF→#582CFF) |
| **Compliance « bon »** | `complianceColor` → `var(--success)` | **= vert cyber #22C55E (INTERDIT)** | `--v-success` (#01B574) |
| Compliance « moyen » | `var(--warning)` | ok mais global | `--v-warning` (#FFB547) — token ajouté |
| Compliance « faible » | `var(--danger)` (global #EF4444) | rouge global | `--v-danger` (#E53E3E) |
| Actions Voir/Éditer | `text-fg-body` + `bg-surface` | sombre | base tokens + hover `.v-act` (accent) |
| Action Supprimer | `text-danger` + `border-danger/40` | ok mais rouge global | `--v-danger` + hover `.v-act-danger` |
| Fallback pastille | `var(--text-2)` | token neutre | `--v-text-2` |

### Exceptions volontaires (conservées — vraies données / fonctionnel)
- **Pastilles d'équipe** : `background: t.color` (Bleu/Jaune/Rouge/Vert/Violet — **identifiants visuels, vraies données**). Conservées telles quelles (consigne).
- `useState("#7551FF")` : valeur par défaut du **color-picker** d'une nouvelle équipe (donnée, pas theming). `<input type="color">` exige un hex.

### Bugs fonctionnels repérés
- (aucun — à compléter si détecté ; non corrigé par consigne)

## 3. Corrections appliquées

### Lisibilité (tokens)
- Page wrappée dans `.vision-dashboard` → tokens `--v-*` actifs.
- Titres/noms d'équipe/chiffres → `--v-text` (#FFFFFF). Sous-titre/placeholder/méta/emails → `--v-text-2` (#A0AEC0, AA). En-têtes colonnes → `--v-text-2` sur `--v-surface-2`.
- Tous les `text-fg-*`, `bg-surface`, `bg-primary`, `bg-table-head`, `text-[var(--on-accent)]`… remplacés par tokens Vision.
- **Compliance** : `complianceColor` passe de `var(--success)` (**vert cyber #22C55E, supprimé**) à `--v-success` (#01B574) / `--v-warning` (#FFB547, token ajouté) / `--v-danger` (#E53E3E).

### DA / cohérence
- Tableau + section « Membres sans équipe » en **verre dépoli** (surface #111C44/82 %, blur, bordure #2A3568), en-têtes sur `--v-surface-2`.
- Recherche + champs de création = `VisionInput` (focus ring accent). Sélecteur d'affectation = `VisionSelect`. Boutons « Nouvelle équipe » / « Créer » = `VisionButton` dégradé `--v-grad` (#7551FF→#582CFF) ; « Annuler » secondaire.
- Sélecteur d'icône restylé (actif = bordure+fond accent). Actions Voir/Éditer (hover accent via `.v-act`), Supprimer clairement **danger** (`--v-danger`, hover `.v-act-danger`).
- **Pastilles d'équipe conservées** (`t.color`, vraies données) — `TeamBadge`.

### Animations (framer-motion, sobres, prefers-reduced-motion)
- Entrée de page : `Reveal` (fade + translate). Lignes du tableau : **stagger** (`motion.tbody`/`motion.tr` + `staggerContainer/Item`, ~50 ms) ; cartes mobiles : `Stagger`.
- Hover doux sur lignes (`.v-row`, surface s'éclaircit) et boutons d'action. Focus ring accent violet sur recherche/champs (VisionInput) et boutons.
- **Count-up** sur membres, parcours et % compliance (`CountUp`).
- `Reveal`/`Stagger`/`CountUp` + `initial={false}` sur `motion.tbody` si `prefers-reduced-motion` → rendu statique.

### Exceptions volontaires (conservées)
- Pastilles d'équipe `t.color` (identité, vraie donnée). `#7551FF` = valeur par défaut du color-picker `<input type="color">` (donnée équipe, pas theming).

## 4. APRÈS — vérifications
- `tsc --noEmit` **OK** · `eslint` (page) **0** · `npm run build` **OK** (`/admin/teams` compilée).
- Grep : **0** vert cyber #22C55E, **0** token neutre résiduel, **0** `var(--success/danger/warning)` global, hex restants = **uniquement** `#7551FF` (color-picker, documenté).
- Contraste AA : #FFFFFF / #A0AEC0 sur #0B1437/#111C44/#1A2456 → conforme.
- Périmètre : seuls `page.tsx` + `globals.css` (token `--v-warning` + classes `.v-act`). `TeamEditModal` **non touché** (partagé, hors périmètre). Backend/logique non modifiés. Vraies données préservées.
- Bugs fonctionnels : aucun repéré.

## 5. RAPPORT — STOP
Page **Gestion des équipes** refondue charte violet Vision UI : lisible (tokens, AA), tableau/section en verre, boutons dégradé, recherche au style charte, **pastilles d'équipe conservées**, compliance en code couleur cohérent (succès/alerte/danger tokens). Animations sobres (entrée, stagger lignes, hover, focus, count-up), `prefers-reduced-motion` respecté. `npm run build` passe, zéro couleur en dur (hors pastilles + défaut color-picker), zéro vert cyber. **100 % LOCAL**, commit local. STOP.
