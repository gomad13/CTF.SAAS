# Diagnostic — page `/admin` cassée (DSI bêta readiness)

> Session 2026-04-27. Reproduction et analyse de la cause racine du tableau "Chargement..." infini observé en audit du 2026-04-26.

## Symptôme observé

- Page `/admin` (et `/admin/dashboard` qui partage le même endpoint).
- Tableau "Employés" reste sur "Chargement..." indéfiniment.
- Pie chart "Répartition des statuts" rendu mais grisé.
- 7 erreurs JS console pendant la navigation admin (audit 2026-04-26).

## Capture réseau (Puppeteer + cookie auth admin)

Compte de test temporaire : `dsi-audit-admin@cybermed-test.local` / `Employe@2026` (créé puis supprimé en fin de session).

```
GET /api/admin/tracking/users?pathId=c0000007-0000-0000-0000-000000000000&page=1&pageSize=50
→ HTTP 500
→ body: {"error":"Nullable object must have a value."}
```

L'endpoint était appelé à intervalle régulier (4×) car le composant front retry sur erreur, ce qui faisait monter le compteur à 7 erreurs console.

## Stack trace (capturé via ExceptionMiddleware enrichi en mode dev)

```
System.InvalidOperationException: Nullable object must have a value.
   at lambda_method863(Closure, QueryContext, DbDataReader, ResultContext, SingleQueryResultCoordinator)
   at Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()
   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, ...)
   at CTF.Api.Controllers.AdminController.GetTrackingUsers(Guid pathId, ...) in AdminController.cs:line 565
```

→ Crash dans le **shaper** EF Core (lambda compilé qui matérialise les rows). Il essaie de lire une valeur d'un `DbDataReader` retournée comme NULL (LEFT JOIN non-matché) en la convertissant vers un type C# **non-nullable**.

## Cause racine

`AdminController.GetTrackingUsers` projetait, dans une LINQ avec deux `LEFT JOIN` (`Progresses` via `DefaultIfEmpty()` et un sous-query `scoreQuery`), des champs des entités jointes en types **non-nullables** :

```csharp
select new
{
    ...
    Progress       = p != null ? p.Percent : 0,             // int
    LastActivityAt = p != null ? p.UpdatedAt : (DateTime?)null,
    Score          = sc != null ? sc.Score : 0              // int
};
```

Le translator EF Core 8 / Npgsql génère bien un `LEFT JOIN` SQL, mais lors de la matérialisation, pour les lignes où la jointure échoue, les colonnes `p.Percent`, `p.UpdatedAt`, `sc.Score` sont NULL en SQL. Le shaper compilé essaie de lire ces colonnes en `int` / `DateTime` non-nullable → throw `InvalidOperationException("Nullable object must have a value")` avant même que le ternaire `p != null ? ...` n'ait l'occasion de court-circuiter l'accès.

C'est un piège classique du translator EF : les conditionnels `entity != null ? entity.NonNullableProp : default` ne sont **pas** garantis de protéger la lecture côté shaper si la propriété cible est non-nullable. Le contournement officiel : caster directement la propriété vers son équivalent nullable au niveau de la projection.

## Fix appliqué (`AdminController.cs:497-555` après edit)

1. **Subquery `scoreQuery`** : `Sum(x => x.ScoreAwarded)` → `Sum(x => (int?)x.ScoreAwarded) ?? 0` (pour les groupes hypothétiques sans rows, sans changement fonctionnel pour la majorité).
2. **Projection principale** : remplacement des conditionnels par des casts directs en types nullables :
   - `Progress = (p != null ? p.Percent : 0)` → `PercentNullable = (int?)p.Percent` puis `?? 0` après ToList.
   - `LastActivityAt = ...` → `LastActivityNullable = (DateTime?)p.UpdatedAt`.
   - `Score = ...` → `ScoreNullable = (int?)sc.Score` puis `?? 0`.
3. **`StatusCode` calculé en mémoire** après `ToListAsync()`, à partir des valeurs déjà désencapsulées.
4. **Filter `status` reformulé** côté SQL pour s'exprimer en termes de `PercentNullable` / `ScoreNullable` au lieu du `StatusCode` dérivé (plus traduisible par EF, plus efficace en SQL).

## Vérification post-fix (Puppeteer)

```
GET /api/admin/tracking/users?pathId=c0000007-...&page=1&pageSize=50 → HTTP 200
DOM: tableRows: [11]   (1 header + 10 employés)
Console: 0 errors, 0 5xx
```

Capture : `docs/tests/admin-after-fix.png`.

## Acquis annexes

- **ExceptionMiddleware** enrichi en mode `Development` pour exposer `type` et `stack` dans la réponse JSON (le comportement prod reste un message générique). Utile pour les futurs diagnostics. `CTF. API/Middleware/ExceptionMiddleware.cs`.
- **Charts non conformes DS** (bar chart noir-sur-noir, pie principalement gris) : non bloquant ; la donnée est correcte. À traiter en Phase 2 (couleurs token + empty states textuels quand série vide).

## Compte test à supprimer

`dsi-audit-admin@cybermed-test.local` (tenant CyberMed Innovations). Cleanup prévu dans la phase finale.
