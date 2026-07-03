---
name: parallelisation-taches
description: À appliquer pour aller plus vite — paralléliser les tâches INDÉPENDANTES (frontend/backend, recherches multiples, fichiers différents), mais SÉQUENTIALISER les tâches dépendantes ou risquées (migrations BDD, déploiement, même fichier). La vitesse ne compromet jamais la sécurité.
---

# Parallélisation des tâches

Principe : **parallélise l'indépendant, séquentialise le dépendant/risqué.**

## Ce qui PEUT être parallélisé
- **Recherches/inspections multiples** : lancer plusieurs `grep`/`ls`/`cat` en **un seul** message/commande (outils indépendants → appels groupés).
- **Frontend ⟂ Backend** : quand une feature touche `ctf-web/` ET `CTF. API/` sans dépendance directe, préparer/éditer les deux en parallèle.
- **Fichiers différents indépendants** : plusieurs composants/DTO/pages sans lien.
- **Sous-agents en parallèle** (cf. CLAUDE.md M2) : plusieurs explorations/lectures massives simultanées → chacune rend sa conclusion.
- **Commandes bash indépendantes** : les grouper dans un lot ; `cmd1 & cmd2 & wait` si vraiment indépendantes, sinon un seul appel avec séparateurs.

## Ce qui DOIT rester SÉQUENTIEL (jamais paralléliser)
- **Migrations EF Core** : `migrations add` → **rebuild** → `database update` dans cet ordre. Jamais deux migrations en parallèle. (cf. TaskForLesson : `--no-build` après un `migrations add` n'applique rien.)
- **Déploiement** : `dotnet publish` → `systemctl restart sentys-backend` → **health check** ; `npm run build` → `restart sentys-frontend`. Un restart ne se lance pas avant que le build soit fini et vérifié.
- **Modifications du MÊME fichier** : éditions ordonnées (risque de conflit/écrasement).
- **Chaîne de dépendance** : créer l'entité → migration → usage. Respecter l'ordre.
- **Backup → modif → test** : le backup se termine AVANT toute modif destructrice ; le test vient APRÈS le déploiement.

## Règle d'or
> **Backup et sécurité d'abord.** Ne jamais paralléliser au détriment d'un backup, d'un test, ou de l'isolation tenant. En cas de doute sur une dépendance → séquentiel.

## Exemple concret (feature multi-fichiers sur CE projet)
Tâche : « ajouter un champ + endpoint + UI ».
- **Parallèle** : `grep` du modèle + `grep` du controller + `grep` de la page front (localisation) ; puis lecture ciblée des 3 zones.
- **Séquentiel obligatoire** : modifier le modèle → `migrations add X` → rebuild → `database update` → puis (parallèle OK) éditer controller + DTO + page front → puis build backend **et** frontend (peuvent tourner en parallèle) → puis restart backend, restart frontend (chacun après SON build) → puis tests → log.
- **Jamais** : lancer la migration pendant que le modèle est encore en cours d'édition ; redémarrer un service avant la fin de son build.
