# FIX_CATALOGUE_LOG.md — Refonte page « Catalogue » (charte violet Vision UI) + fiche riche immersive

> Périmètre STRICT : page `/admin/catalog` (grille des parcours) + **fiche vitrine STATIQUE** par parcours. On NE touche PAS au moteur de parcours, ni à l'accès/inscription, ni au scoring. 100 % LOCAL.
> Autorité : `Instructions.md` absent → **CLAUDE.md** (charte §2, qualité §1, sécu §3/§7).

## 0. Backup (méthode §1)
- `backups/catalogue-20260708_214103/` : `front/admin-catalog-page.tsx`, `back/LearningPathsController.cs`, `schema.sql`.

## 1. Fichiers
- `ctf-web/src/app/admin/catalog/page.tsx` (page — refonte)
- Backend : endpoint **détail lecture seule** pour la fiche (modules + exemple) — _à créer selon inventaire (sous-agent en cours)_.
- Réutilise `VisionCard`, `VisionForm` (Input/Select/Button), `Reveal`/`Stagger`, `CountUp`.

## 2. CONSTAT AVANT — couleurs peu lisibles / hors charte
Page hors `.vision-dashboard`, tokens **globaux** (thème neutre) → gris sombre / peu lisible sur le reste violet.

| Emplacement | Avant | Problème | Après (token Vision) |
|---|---|---|---|
| Titres, noms de parcours | `var(--text)` (global) | non aligné vision | `--v-text` (#FFFFFF) |
| Descriptions, méta, sous-titres | `var(--text-2)` (global) | gris sombre | `--v-text-2` (#A0AEC0), AA |
| Cartes | `var(--surface)` + `var(--border)` plat | pas verre | `VisionCard` (surface #111C44, blur, bordure #2A3568) |
| Recherche + selects filtres | `var(--border)` clair | peu lisible | `VisionInput`/`VisionSelect` (surface-2, focus ring accent) |
| Boutons Activer/actions | `var(--accent)` + `var(--on-accent)` | plat/global | `VisionButton` dégradé `--v-grad` |
| **Secteur « finance »** | `sectorColor` → `var(--success)` | **= vert cyber #22C55E (INTERDIT)** | `--v-success` (#01B574) |
| Secteurs santé/cyber/compta | `var(--danger)`/`var(--accent)`/`var(--warning)` (globaux) | à repointer | `--v-danger`/`--v-accent`/`--v-warning` |
| Pills statut | `var(--surface-2)`/`--accent-subtle`/`--success-subtle`/`--success-t` (globaux) | tokens neutres | map locale `--v-*` (color-mix) |
| Modale (Aperçu/Activer) | `var(--surface)` + `rgba` overlay | plat | verre `--v-surface`, upgrade en **fiche riche** |
| Fond page | `var(--bg)` global | — | `.vision-dashboard` (fond #0B1437) |

### Bugs fonctionnels repérés
- (aucun — à compléter ; non corrigé par consigne)

## 3. Fiche riche immersive — sources de vraies données (inventaire confirmé)
- **Path** expose : `Title, Description?, Level?, Sector?, EstimatedMinutes?, Tags?`. **PAS** de champ Objectifs/Audience/Icon/Color.
- **Module** : `Title, SortOrder` (PAS de description).
- **Challenge** : `Title, Instructions, InstructionTitle?, InstructionBody?, InstructionShortReminder?, Category`. Réponses (`CorrectAnswer`, `ContentJson`, `VariantsJson`) = `[JsonIgnore]`, **jamais exposées**.
- **Mapping honnête (zéro invention)** :
  - En-tête : Title, durée, niveau, secteur (icône lucide + dégradé).
  - « Ce que vous allez apprendre » = **thèmes réels** = catégories distinctes des challenges (`Challenge.Category`). Vide → « Thèmes à venir ».
  - « Au programme » = **modules réels** (Title ordonné + nb d'exercices). Vide → « Programme à venir ».
  - « Exemple » = 1er challenge (ordre module/challenge) → `InstructionTitle ?? Title` + `InstructionBody ?? InstructionShortReminder ?? Instructions` (tronqué, SANS réponse). Vide → « Aperçu à venir ».
  - « Pour qui / pourquoi » = `Description` + secteur (public) + niveau + tags. Vide → « Description à venir ».
- **Endpoint créé** (lecture seule) : `GET /api/admin/catalog/{pathId}/fiche` → `CatalogFicheDto` (modules, themes, example). `[Authorize(admin,SuperAdmin)]`, path `IsCatalog` non archivé, `AsNoTracking`, requêtes groupées (**pas de N+1**), aucune écriture, aucune réponse exposée. (Le moteur de parcours n'est pas touché ; `/api/paths/{id}` existant non modifié.)

## 5. Corrections / Rapport

### Lisibilité (tokens)
- Page wrappée `.vision-dashboard`. Titres/noms → `--v-text` (#FFFFFF) ; descriptions/méta/sous-titres → `--v-text-2` (#A0AEC0, AA). Fin des `var(--text)`/`var(--surface)` globaux.
- **Secteur finance** : `var(--success)` (vert cyber) → `--v-success` (#01B574). Autres secteurs repointés sur `--v-danger`/`--v-accent`/`--v-warning`.

### DA / cohérence
- Grille de cartes en **verre dépoli** (`.v-catcard`), filtres `VisionInput`/`VisionSelect`, boutons `VisionButton` dégradé. Pills statut/secteur en tokens Vision. Modales (activation + fiche) en verre.

### Animations (framer-motion, sobres, prefers-reduced-motion)
- Entrée `Reveal`, **stagger** des cartes, **hover** carte (scale 1.02 + surface éclaircie + ombre via `.v-catcard`), **ouverture fiche animée** (`AnimatePresence` + scale/fade), focus ring accent (VisionInput/Select). Reveal/Stagger/CountUp respectent `prefers-reduced-motion`.

### Fiche riche immersive
- En-tête immersif (dégradé secteur→violet + icône lucide, titre, chips durée/niveau/secteur/exercices), « Pour qui/pourquoi », « Ce que vous allez apprendre » (thèmes réels), « Au programme » (modules réels), « Aperçu — un exemple » (vrai contenu, sans réponse). **3 états** (chargement/données/vide) ; **états vides propres** (« … à venir »), **zéro contenu inventé**.
- **CTA = flux EXISTANT** : Activer (ouvre la modale d'activation existante) / Désactiver / Contacter commercial — **aucune modification** de la logique d'accès/activation.

### Vérifs
- Build backend **OK** (0/0) ; frontend `tsc` **OK**, `eslint` **0**, `npm run build` **OK** ; **0 hex en dur**, **0 vert cyber**, **0 token global résiduel** ; endpoints `catalog`/`fiche` **401** sans auth ; health 200 ; aucune erreur.
- Sécu : `[Authorize]` admin, DTO, pas de N+1, pas de fallback démo, **aucune réponse de challenge exposée**, lecture seule. Moteur de parcours/scoring **non touché**.
- Bugs fonctionnels : aucun repéré.

### Rapport final
Catalogue **lisible + animé + charte violet**, avec une **fiche riche immersive par parcours** bâtie sur le **vrai contenu** (en-tête immersif, ce qu'on apprend = thèmes réels, au programme = modules réels, exemple = instruction réelle sans réponse, pour qui/pourquoi). États vides propres, zéro invention. Builds OK, sécu+charte OK. **100 % LOCAL**, commit local. STOP.

## 4. Plan
1. Lisibilité (tokens `--v-*`, wrap `.vision-dashboard`).
2. Grille catalogue (DA verre + anim stagger/hover).
3. Fiche riche immersive (en-tête dégradé+icône, ce qu'on apprend, au programme, exemple réel, pour qui/pourquoi) sur vrai contenu.
4. États vides. CTA = flux EXISTANT (activation), non modifié.

## 5. Corrections / Rapport
_(à compléter)_
