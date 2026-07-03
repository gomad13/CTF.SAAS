---
name: workflow-efficace-sentys
description: Mode de travail optimal sur le projet Sentys (synthèse) — localiser→lire ciblé→éditer chirurgical→tester→logger, paralléliser l'indépendant, rester concis, toujours dans le respect du fichier d'instructions (sécurité/qualité/charte/RGPD) et backup avant modif destructrice.
---

# Workflow efficace Sentys (synthèse)

Combine [[economie-tokens]] et [[parallelisation-taches]] avec les règles projet (CLAUDE.md, TaskForLesson.md).

## Boucle standard
1. **PLAN** court (fichiers touchés, ordre, points de vérif) — cf. CLAUDE.md M1.
2. **LOCALISER** : `grep`/recherche avant de lire (jamais lire un gros fichier en entier).
3. **LIRE CIBLÉ** : seulement les plages utiles ; réutiliser le contexte déjà connu.
4. **ÉDITER CHIRURGICAL** : diff/str_replace sur ancre unique, pas de réécriture massive.
5. **PARALLÉLISER l'indépendant** (recherches, front⟂back, sous-agents) ; **SÉQUENTIALISER le dépendant/risqué** (migration → rebuild → update ; build → restart → health).
6. **TESTER pour de vrai** : build, endpoint (curl/psql), logs runtime lus — cf. CLAUDE.md M4. Filtrer les sorties (`grep`/`tail`).
7. **LOGGER** : journal court + `TaskForLesson.md` si erreur rencontrée.
8. **RAPPORT concis** : cause → correctif → preuve.

## Non négociable (le workflow ne les contourne jamais)
- **Backup avant toute modif destructrice** (BDD + code) ; rollback possible.
- **Sécurité/RGPD** : isolation tenant stricte, contexte depuis JWT/token (jamais body/query), secrets jamais versionnés — cf. CLAUDE.md §5.3.
- **Qualité** : DTOs, `AsNoTracking` en lecture, méthodes courtes, zéro `any` TS — cf. CLAUDE.md §5.
- **Charte** : teal `bg-sentys` go-forward, contraste AA, Lucide filaire — cf. [[charte-graphique-sentys]].
- **Vérif réelle** avant de clore : « normalement ça marche » n'est jamais une conclusion (M4).

## Réflexe « multi-fichiers »
Localiser les 3 (ou N) points en parallèle → éditer l'indépendant en parallèle → séquentialiser la chaîne à dépendance (schéma→migration→usage) et le déploiement → tester → logger. Voir exemple détaillé dans [[parallelisation-taches]].

## Priorité en cas de tension
**Sécurité > exactitude > exhaustivité de la vérif > vitesse > économie de tokens.**
On optimise les tokens et la vitesse *après* avoir garanti sécurité, exactitude et vérification.
