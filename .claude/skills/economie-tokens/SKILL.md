---
name: economie-tokens
description: À appliquer sur CHAQUE tâche pour minimiser la consommation de tokens — localiser avant de lire, lire ciblé (plages de lignes), éditer chirurgical (diff), filtrer les sorties, rester concis. Ne change aucune règle sécurité/qualité/charte (les complète).
---

# Économie de tokens

Objectif : sessions plus rapides et moins coûteuses, sans jamais sacrifier la sécurité/qualité/charte du projet.

## Règles
1. **Localiser AVANT de lire** : `grep`/recherche pour trouver le bon fichier + la bonne ligne, puis lire **seulement la plage utile** (ex. lignes 40–90), pas le fichier entier.
2. **Ne pas relire / re-dumper** ce qui est déjà connu dans la session (structure, conventions, contenu déjà lu). S'appuyer sur le contexte établi.
3. **Éditions chirurgicales** : modifier par remplacement ciblé (str_replace / patch d'ancre unique), jamais réécrire un gros fichier pour changer 3 lignes.
4. **Filtrer les sorties de commandes** : `head`/`tail`/`grep`/`wc`, `--json | python -c`, `| tail -5`. Jamais dumper un `npm run build` complet, une migration entière, un `git diff` géant → cibler les lignes qui portent l'info (erreurs, résumé).
5. **Batcher** recherches et inspections : regrouper plusieurs `grep`/`ls`/`cat` dans **une** commande plutôt que multiplier les allers-retours.
6. **Aller droit au but** grâce à la connaissance de la structure (`CTF. API/` backend, `ctf-web/` frontend, serveur `/home/ubuntu/sentys/…`) : pas d'exploration inutile.
7. **Réponses concises** : rapport court et actionnable (cause → correctif → preuve), pas de reformulation ni de narration du process.
8. **Déléguer le volumineux** à un sous-agent (cf. CLAUDE.md M2) : lecture massive / exploration → le contexte principal ne reçoit que la conclusion, pas les dumps.

## Bon vs mauvais

| ❌ Mauvais (coûteux) | ✅ Bon (économe) |
|---|---|
| `cat Controller.cs` (800 lignes) pour voir une méthode | `grep -n "MethodName"` puis lire lignes 120–150 |
| Réécrire tout le fichier pour changer un token | `str_replace` de l'ancre exacte |
| `npm run build` (sortie complète) | `npm run build 2>&1 \| grep -iE "error\|Compiled"` |
| Relire un fichier déjà lu ce tour | réutiliser ce qu'on sait déjà |
| 6 commandes `grep` séparées | 1 commande groupée avec `echo` séparateurs |
| Long préambule + reformulation | 3 lignes : ce qui a changé + preuve |

## Garde-fous (ne PAS économiser au détriment de)
- La **vérification réelle** (build, endpoint, logs) — cf. CLAUDE.md M4. Filtrer la sortie ≠ ne pas tester.
- La **lecture d'un fichier avant de le supprimer/écraser**.
- Le **backup avant modif destructrice**.
