# Test — Inscription publique fermée (Bêta DSI)

> Vérifié le 2026-04-28 sur stack locale (back 5202, front 3000).

## Contexte

L'inscription publique est volontairement fermée pendant la bêta DSI. Réactivable via la config `Beta:RegistrationOpen=true`. OAuth Google/Microsoft restent actifs (fallback Demo si domaine inconnu — comportement implémenté en session SSO antérieure).

## Tests

### 1. Endpoint utilitaire `GET /api/auth/registration-status` (public)

```
GET http://localhost:5202/api/auth/registration-status

→ HTTP 200
→ Body: {"open":false}
```

✅ Public, anonyme accepté, retour JSON minimal. Le front l'utilise au mount de `/register` pour décider quel écran rendre.

### 2. Endpoint `POST /api/auth/register` avec `Beta:RegistrationOpen=false`

```
POST http://localhost:5202/api/auth/register
Content-Type: application/json
X-Requested-With: XMLHttpRequest
Body: {"firstName":"Test","lastName":"User","email":"test-blq@example.local","password":"Strong@2026!"}

→ HTTP 403 Forbidden
```

✅ Inscription bloquée même si le payload est valide. Le user ne peut pas créer de compte sans passer par un admin.

(Le message FR retourné n'est pas vérifié dans cette session car le test direct PowerShell ne capture pas le body sur 403, mais le code source `AuthController.cs` retourne bien :
```
"L'inscription publique est actuellement fermée. Si votre organisation est partenaire de Viper, contactez votre administrateur. Pour rejoindre la bêta, écrivez à contact@viper.fr."
```)

### 3. Page front `/register` rendue

Capture : `docs/tests/register-closed.png`.

Contenu visible :
- Banner bêta haut "Version bêta privée — vos retours nous aident à améliorer la plateforme. **Envoyer un feedback**"
- Bandeau "BÊTA PRIVÉE"
- H1 "Inscription réservée aux organisations partenaires"
- Paragraphe explicatif : "Viper est actuellement en bêta fermée. Les comptes sont créés directement par l'administrateur de votre organisation, ou par notre équipe si votre entreprise rejoint le programme partenaire."
- Lien "Vous avez déjà reçu vos identifiants ? **Se connecter**" → `/login`
- CTA primaire "Contacter l'équipe Viper" → `mailto:contact@viper.fr?subject=Demande%20d%27acc%C3%A8s%20bêta%20Viper`
- CTA secondaire "← Retour à l'accueil" → `/landing`
- Banner cookies en bas (route publique non-auth, normal)

✅ Aucun formulaire d'inscription n'est rendu. Le user ne peut pas tenter de créer un compte via cette page.

### 4. OAuth Google/Microsoft

Vérifié dans le code (`Program.cs` configure les `AuthBuilder` Google/Microsoft) et dans `SsoFlowService` : ces flows restent actifs. Si un user OAuth a un domaine email inconnu, la session SSO le rattache au tenant Demo (logique implémentée précédemment). **Aucun test live OAuth en bêta DSI** car nécessite des credentials Google/Microsoft Cloud Console pour la prod.

## Réactivation pour la V1 publique

Modifier la valeur `Beta:RegistrationOpen` à `true` :

- Dev : `dotnet user-secrets set "Beta:RegistrationOpen" "true"` dans `CTF. API/`
- Prod (Scaleway via Docker Compose) : env var `Beta__RegistrationOpen=true` dans le `.env` du serveur Web

Le front bascule automatiquement (le `useEffect` au mount lit l'endpoint et rend le formulaire).

## Conclusion

Bloquant 4 corrigé et vérifié ✅ :
- Backend retourne 403 sur tentative d'inscription
- Frontend rend une page friendly informative au lieu du formulaire
- Endpoint `/registration-status` permet de basculer sans rebuild
- Réactivation = 1 changement de config, pas de modification de code
