# FIX_CAMPAGNES_LOG.md — Refonte page « Campagnes » (charte violet Vision UI) + graphes d'efficacité

> Périmètre : page `/admin/campaigns` (formulaire Nouvelle campagne + liste/gestion + graphes d'efficacité). Backend touché UNIQUEMENT pour des endpoints d'agrégation **lecture seule** (efficacité). Aucune modif de la logique d'assignation. **Aucune action réelle (email/assignation) déclenchée.** 100 % LOCAL.
> Autorité : `Instructions.md` absent → **CLAUDE.md** (charte §2, sécu §3/§7, RGPD §4).

## 0. Backup (méthode §1)
- `backups/campagnes-20260708_012500/` : `front/campaigns/` (page + [id]), `back/` (CampaignsController, CampaignsService, CampaignDtos, Campaign.cs), `schema.sql` (schéma DB).

## 1. Fichiers
- `ctf-web/src/app/admin/campaigns/page.tsx` (page — refonte)
- Backend : `CTF. API/Controllers/CampaignsController.cs` + `Contracts/CampaignDtos.cs` (endpoint efficacité, lecture seule) — **inventaire backend en cours (sous-agent)**.
- Réutilise `VisionForm`, `VisionCard`, `VisionAreaChart`, `VisionBarChart`, `CountUp`, `Reveal`/`Stagger`, `Toast`.
- **Non touché** : `types/campaigns.ts` `STATUS_STYLES` (partagé avec `[id]`, thème neutre) → map statut **locale** en tokens `--v-*` sur la page vision.
- Page détail `[id]/page.tsx` : hors périmètre principal (reste sur l'ancien thème) — noté comme suite possible.

## 2. CONSTAT AVANT — couleurs peu lisibles / hors charte
Page hors `.vision-dashboard`, tokens neutres → gris sombre sur violet.

| Emplacement | Avant | Problème | Après (token Vision) |
|---|---|---|---|
| Titres, noms, valeurs | `text-fg-heading` | gris sombre | `--v-text` (#FFFFFF) |
| Sous-titre, aides, placeholders, méta | `text-fg-muted`/`text-fg-body` | gris sombre | `--v-text-2` (#A0AEC0), AA |
| Cartes (form, liste) | `bg-surface` + `border-border` plat | pas verre | `VisionCard` (surface #111C44, blur, bordure #2A3568) |
| Champs (nom/desc/dates) | `bg-surface` + `placeholder:text-fg-muted` | peu lisible | `VisionInput` (surface-2, texte #FFF, placeholder #A0AEC0, focus ring accent) |
| Bouton « Créer » | `bg-primary` plat | — | `VisionButton` dégradé `--v-grad` (#7551FF→#582CFF) |
| **Chips scénarios sélectionnées** | `border-success bg-success` | **= vert cyber #22C55E (INTERDIT)** | sélectionné = **accent violet** (comme parcours) |
| Chips parcours sélectionnées | `bg-primary` | ok | `--v-accent` |
| Filtres statut actifs | `bg-primary` | — | `--v-accent` |
| En-tête liste | `bg-table-head` | — | `--v-surface-2` |
| Barre de progression | `bg-primary` | — | `--v-grad` |
| Pills de statut | `STATUS_STYLES` (tokens **globaux** `--success`/`--info`/`--surface-2`) | **faux dans vision (vert cyber/bleu neutres)** | map **locale** : À venir neutre / En cours `--v-success` / Terminée `--v-cyan` |
| Suppression | `text-danger` + `hover:bg-danger/10` | rouge global | `--v-danger` + hover token |

### Bugs fonctionnels repérés
- (aucun — à compléter ; non corrigé par consigne)

## 3. Graphes d'efficacité (VRAIES données tenant)
- Endpoint existant réutilisable : `GET /api/admin/campaigns/{id}/dashboard` → `CampaignDashboard` (totalAssigned, notStarted/inProgress/completed, globalCompletionPercentage, averageSuccessRate, lateEmployeesCount). Donne **participation + complétion** par campagne.
- À AJOUTER (lecture seule, agrégation) — selon inventaire backend :
  - **Synthèse** : comparaison participation/complétion entre campagnes (barres).
  - **Par campagne** : complétions dans le temps (aire violet→cyan), résultats scénarios (ex. phishing : cliqué vs signalé).
- 3 états (chargement/données/vide), code couleur charte (succès/alerte/danger). `[Authorize]` admin + tenant JWT + DTO + pas de N+1 + pas de démo.
- _(design finalisé après retour inventaire backend)_

## 4. PROPOSITIONS de features/plugins — À VALIDER (aucune codée sans OK)

> ⚠️ Rappel : cette page fait de l'**assignation en masse**. Toute proposition touchant emails/relances/planification/logique d'assignation est marquée **NE PAS CODER sans validation explicite**.

### A — Confort d'usage (sans effet de bord, sûres)
| # | Proposition | But | Effet de bord | Coût |
|---|---|---|---|---|
| A1 | **Prévisualisation du nombre d'assignés** avant création (« ~N employés actifs seront assignés ») | éviter les surprises d'assignation en masse | lecture seule (compte les users actifs) | faible (1 endpoint count ou réutilise stats) |
| A2 | **Recherche + tri** de la liste (par nom, date, complétion) | retrouver vite une campagne | aucun (front only) | faible |
| A3 | **Rappel visuel des dates** (badge « se termine dans X j » / « démarre dans X j ») | lisibilité temporelle | aucun (front only) | faible |
| A4 | **Duplication d'une campagne** (pré-remplit le formulaire, sans créer ni assigner) | gagner du temps | **aucun tant que ça ne crée rien** — juste pré-remplissage front | faible |
| A5 | **Export CSV** de l'efficacité (synthèse + par campagne) | reporting | lecture seule | faible |

### B — À effet de bord (NE PAS coder sans validation explicite)
| # | Proposition | Risque |
|---|---|---|
| B1 | Statut **Brouillon** (créer sans assigner tout de suite) | touche la **logique d'assignation** (création différée) → validation requise |
| B2 | **Duplication qui recrée + réassigne** immédiatement | **assignation réelle en masse** → NON sans OK |
| B3 | **Relances / notifications automatiques** aux retardataires | **envoi d'emails réels** → NON sans OK |
| B4 | **Planification d'envois** (scheduling) | **actions réelles différées** → NON sans OK |

→ **En attente de ton choix.** Par défaut je n'implémente **rien** de cette section 4 ; je livre uniquement les 3 objectifs directs (lisibilité, DA, animations, graphes d'efficacité).

## 5. Corrections appliquées / Rapport

### Lisibilité (tokens)
- Page wrappée `.vision-dashboard`. Labels/noms/valeurs → `--v-text` (#FFFFFF) ; sous-titres/aides/placeholders/méta → `--v-text-2` (#A0AEC0, AA). Champs `VisionInput` (texte #FFF, placeholder #A0AEC0 via règle scopée, focus ring accent).
- Tous les `text-fg-*`, `bg-surface`, `bg-primary`, `bg-table-head`, `text-[var(--on-accent)]` supprimés.
- **Chips scénarios** : `bg-success` (vert cyber #22C55E) → sélectionné = **accent violet** (comme parcours). Composant `Chip` animé (hover + état sélectionné).
- **Pills de statut** : map **locale** `statusVision` en tokens `--v-*` (À venir neutre / En cours `--v-success` / Terminée `--v-cyan`) — `STATUS_STYLES` global non touché (partagé avec `[id]`).

### DA / cohérence
- Formulaire + liste + efficacité en **VisionCard** (verre dépoli). En-tête liste `--v-surface-2`. Bouton « Créer » = `VisionButton` dégradé `--v-grad`. Filtres = `Chip` accent. Barre de progression = `--v-grad`. Checkbox `accentColor: --v-accent`. Suppression `--v-danger` + hover `.v-act-danger`.

### Animations (framer-motion, sobres, prefers-reduced-motion)
- Entrée `Reveal`. **Stagger** sur les champs du formulaire ET sur la liste de campagnes ET sur les KPI d'efficacité. Hover doux sur chips/lignes/boutons. Focus ring accent sur les inputs. Feedback création = `toast.ok` (existant). Count-up sur les chiffres d'efficacité. `Reveal`/`Stagger`/`CountUp` respectent `prefers-reduced-motion`.

### Graphes d'efficacité (VRAIES données, lecture seule)
- **Backend** (CampaignsController, endpoints ajoutés, `[Authorize(admin,SuperAdmin)]` + gate `Mode.Campaigns` + tenant JWT, `AsNoTracking`, pas de N+1, pas de démo) :
  - `GET /api/admin/campaigns/efficacy` → synthèse (participation/complétion par campagne).
  - `GET /api/admin/campaigns/{id}/efficacy` → détail : participation, complétion, réussite moyenne, **courbe cumulée des complétions** (jour/semaine), **résultats scénarios phishing** (e-mails d'attaque, taux de clic, taux de signalement) via `ScenarioEmail.IsAttackStep`/`FirstClickAt`/`ReportedAt` scoping campagne→instances→steps→emails.
  - **Aucune écriture** : lecture seule des tables `CampaignAssignment`/`CampaignProgress`/`Scenario*` (pas de recompute/upsert).
- **Frontend** : section « Efficacité des campagnes » — comparaison (VisionBarChart), sélecteur de campagne, 3 KPI (VisionKpiCard), courbe (VisionAreaChart violet→cyan), résultats phishing avec **code couleur charte** (clic ≥30 % danger / ≥10 % alerte / sinon succès ; signalement en succès). **3 états** (chargement/données/vide) partout.

### Vérifs
- Build backend **OK** (0 err/warn) ; frontend `tsc` **OK**, `eslint` **0**, `npm run build` **OK** ; **0 hex en dur**, **0 vert cyber** ; endpoints efficacité **401** sans auth ; health 200 ; aucune erreur de démarrage.
- **Aucune action réelle** (email/assignation) déclenchée : endpoints strictement en lecture. Logique d'assignation existante **non modifiée**. Sécu (JWT/[Authorize]/DTO/pas de N+1/pas de démo), RGPD (tenant), charte OK.
- Page `[id]` détail : **non refondue** (hors périmètre principal, reste sur l'ancien thème) — candidate à une passe ultérieure.

### Rapport final
Les **3 objectifs directs** sont livrés : lisibilité (tokens AA), DA charte violet (verre/champs/boutons dégradé/chips), animations sobres (entrée/stagger/hover/focus/count-up), + **graphes d'efficacité** branchés sur vraies données (état vide sinon). Les **propositions de features (§4)** sont listées et **en attente de validation** — aucune codée. 2 builds OK. **100 % LOCAL**, commit local. **STOP** (attente de ton choix sur les features §4).
