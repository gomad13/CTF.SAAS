# OPTIM_SKILLS_LOG.md

Skills de méta-outillage (économie de tokens + parallélisation) pour rendre les futures sessions plus rapides et moins coûteuses. Pur travail repo local — aucune modif serveur/BDD/déploiement.

---

## [2026-07-03] — 3 skills + section 10 CLAUDE.md

### Skills créées (`.claude/skills/`, lues automatiquement par Claude Code)
| Skill | Emplacement | Couvre |
|---|---|---|
| `economie-tokens` | `.claude/skills/economie-tokens/SKILL.md` | localiser avant de lire, lecture ciblée, éditions chirurgicales, sorties filtrées, réponses concises, délégation sous-agent — + garde-fous (ne pas économiser au détriment des tests/backup) |
| `parallelisation-taches` | `.claude/skills/parallelisation-taches/SKILL.md` | paralléliser l'indépendant (front⟂back, recherches, sous-agents) ; **séquentialiser** migrations EF / déploiement / même fichier / chaîne de dépendance ; « backup et sécurité d'abord » |
| `workflow-efficace-sentys` | `.claude/skills/workflow-efficace-sentys/SKILL.md` | synthèse : boucle localiser→lire ciblé→éditer chirurgical→paralléliser/séquentialiser→tester→logger ; priorité **sécurité > exactitude > vérif > vitesse > tokens** |

Chaque `SKILL.md` a un frontmatter (`name` + `description` = quand l'appliquer) + règles concises + exemples (bon vs mauvais / parallèle vs séquentiel).

### Fichier d'instructions projet
- **CLAUDE.md complété** (existant NON touché) : ajout d'une **section « ## 10. Optimisation du workflow (tokens & parallélisation) »** en fin de fichier.
  - ⚠️ Numérotée **10** et non 8 : la section **8 = « Checklist »** et **9 = « Fichiers liés »** existent déjà. Ajouter un second « ## 8 » aurait créé un doublon / imposé de renuméroter l'existant (interdit). La section 10 est purement additive.
  - CLAUDE.md reste un fichier **local non versionné** (statut inchangé) ; il est lu à chaque session que git le suive ou non, donc la guidance prend effet immédiatement.

### Cohérence avec les règles existantes (vérifiée)
- Les skills **s'ajoutent** aux règles CLAUDE.md, sans en contredire aucune. Elles renvoient explicitement à : M1 (plan), M2 (sous-agents pour le volumineux), M4 (vérif réelle), §5.3 (sécurité/RGPD), TaskForLesson (migration `--no-build`), et [[charte-graphique-sentys]].
- **Sécurité préservée** : `parallelisation-taches` interdit explicitement de paralléliser migrations BDD / déploiement / même fichier, et pose « backup et sécurité d'abord ». `workflow-efficace-sentys` fixe la priorité **sécurité > … > tokens**.

## Résumé des règles clés
- **Tokens** : grep avant de lire → lire ciblé → éditer par diff → filtrer les sorties → concis → réutiliser le contexte → déléguer le volumineux.
- **Parallélisation** : indépendant en parallèle (front/back, recherches, sous-agents) ; dépendant/risqué en séquentiel (migration→rebuild→update ; build→restart→health) ; jamais au détriment d'un backup/test.

## Exemple concret (prochaine tâche multi-fichiers : « champ + endpoint + UI »)
- **Parallèle** : localisation (grep modèle + controller + page front) ; puis lecture ciblée des 3 zones ; puis édition controller + DTO + page (fichiers indépendants) ; puis `dotnet build` **et** `npm run build` en parallèle.
- **Séquentiel obligatoire** : éditer le modèle → `migrations add` → rebuild → `database update` (avant tout usage) ; puis, après chaque build, `restart` du service correspondant + health check ; puis tests ; puis log.

## Livrables
1. ✅ Skill `economie-tokens`
2. ✅ Skill `parallelisation-taches`
3. ✅ Skill `workflow-efficace-sentys`
4. ✅ Section 10 ajoutée à CLAUDE.md (additive, non destructive)
5. ✅ Ce log
