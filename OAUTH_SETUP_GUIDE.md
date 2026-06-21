# Guide setup OAuth — Google + Microsoft (étape par étape)

> À suivre **toi-même** pour créer les credentials. Claude Code ne peut pas créer de comptes Google Cloud ou Azure à ta place.
> Temps estimé : 15 minutes Google + 10 minutes Microsoft.

---

## Partie 1 — Google OAuth

### 1.1 Créer un projet Google Cloud

1. Va sur https://console.cloud.google.com/
2. Connecte-toi avec ton compte Google (perso ou pro, peu importe)
3. En haut à gauche, clique sur le sélecteur de projet → **"Nouveau projet"**
4. Nom du projet : `Viper CTF` (ou ce que tu veux)
5. Organisation : laisse "Aucune organisation" si tu n'en as pas
6. Clique **Créer** et attends ~20 secondes

### 1.2 Activer l'API OAuth

1. Projet sélectionné, va dans le menu burger → **APIs et services** → **Écran de consentement OAuth**
2. Type d'utilisateur : **Externe** (pour que n'importe qui avec un compte Google puisse se connecter — en mode test limité à 100 users, largement suffisant pour dev/démo)
3. Clique **Créer**

### 1.3 Configurer l'écran de consentement

**Informations sur l'application** :
- Nom de l'application : `Viper`
- Email d'assistance utilisateur : ton email perso
- Logo : optionnel, tu peux sauter
- Domaines d'application : laisse vide pour dev
- Email du développeur : ton email perso

Clique **Enregistrer et continuer**.

**Niveaux d'accès (scopes)** : clique **Ajouter ou supprimer des champs d'application**, coche :
- `.../auth/userinfo.email`
- `.../auth/userinfo.profile`
- `openid`

Clique **Mettre à jour** puis **Enregistrer et continuer**.

**Utilisateurs de test** : ajoute ton email perso + `h.madoumier@orange.fr` + `madoumih@3il.fr`. Clique **Enregistrer et continuer** puis **Retour au tableau de bord**.

### 1.4 Créer les identifiants OAuth Client ID

1. Menu gauche → **Identifiants**
2. En haut → **+ Créer des identifiants** → **ID client OAuth**
3. Type d'application : **Application Web**
4. Nom : `Viper Dev`
5. **URI de redirection autorisés** → clique **+ Ajouter un URI** :
   - `http://localhost:5202/api/auth/google/callback`
6. Laisse les URI JavaScript autorisés vides pour l'instant
7. Clique **Créer**

Une modale s'ouvre avec :
- **ID client** (long, genre `1234567890-abc...apps.googleusercontent.com`)
- **Code secret du client** (genre `GOCSPX-...`)

**Copie les deux dans un endroit sûr tout de suite** (le secret ne se réaffichera plus tel quel, tu pourras seulement le regénérer).

### 1.5 Enregistrer les credentials côté backend

Ouvre PowerShell dans le dossier de ton projet API :

```powershell
cd C:\Users\madoumih\source\repos\Projet.SAAS\<dossier-api>
dotnet user-secrets set "Authentication:Google:ClientId" "TON_CLIENT_ID_ICI.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "GOCSPX-TON_SECRET_ICI"
```

Vérifie :

```powershell
dotnet user-secrets list
```

Tu dois voir les 2 clés affichées.

**✅ Google est prêt.**

---

## Partie 2 — Microsoft (Azure AD)

### 2.1 Créer un compte Azure (gratuit)

Si tu n'en as pas déjà un :
1. Va sur https://portal.azure.com/
2. Inscris-toi avec un compte Microsoft (perso, ton @orange.fr si tu n'as pas de compte MS, crée-en un)
3. Pas de carte bancaire demandée pour le tier gratuit Azure AD (ex-AD B2C, maintenant Entra ID) — tu ne paies rien

### 2.2 Enregistrer une application

1. Une fois dans le portail Azure : dans la barre de recherche en haut, tape **Microsoft Entra ID** (ancien nom : Azure Active Directory)
2. Dans le menu gauche → **Inscriptions d'applications**
3. En haut → **+ Nouvelle inscription**
4. Remplis :
   - **Nom** : `Viper`
   - **Types de comptes pris en charge** : **Comptes dans un annuaire d'organisation unique et comptes Microsoft personnels (par ex. Skype, Xbox)** — c'est l'option la plus large, pour qu'un médecin avec un compte Outlook perso puisse se connecter comme un salarié avec un compte Office 365 pro
   - **URI de redirection** :
     - Plateforme : **Web**
     - Valeur : `http://localhost:5202/api/auth/microsoft/callback`
5. Clique **Inscrire**

### 2.3 Récupérer le Client ID

Sur la page de ton application qui vient de s'ouvrir, tu vois en haut :
- **ID d'application (client)** : un GUID style `abc12345-6789-4def-0123-456789abcdef` → **copie-le**, c'est ton `ClientId`
- **ID de l'annuaire (tenant)** : autre GUID → pas utile pour toi (tu utilises le endpoint "common" pour accepter tous les tenants Microsoft)

### 2.4 Créer un Client Secret

1. Dans le menu gauche de l'application → **Certificats et secrets**
2. Onglet **Secrets client** → **+ Nouveau secret client**
3. **Description** : `Viper Dev Secret`
4. **Expire** : `24 mois` (ou moins si tu préfères, tu devras regénérer au renouvellement)
5. Clique **Ajouter**

**IMPORTANT** : dans la liste qui apparaît, la colonne **Valeur** contient le secret complet. **Copie-le immédiatement** — il disparaîtra au prochain refresh de la page (seul l'ID de secret reste visible, la valeur non).

### 2.5 Permissions API

1. Menu gauche → **API autorisées**
2. Par défaut tu devrais voir `User.Read` déjà présent — c'est suffisant pour récupérer email/nom
3. Si tu veux aussi la photo de profil : **+ Ajouter une autorisation** → **Microsoft Graph** → **Autorisations déléguées** → cocher `openid`, `profile`, `email` (probablement déjà là)
4. Clique **Mettre à jour les autorisations**
5. (Optionnel) **Accorder le consentement admin** si tu veux éviter que chaque user ait à consentir au 1er login → clique le bouton "Accorder un consentement administrateur pour [annuaire]"

### 2.6 Enregistrer les credentials côté backend

```powershell
cd C:\Users\madoumih\source\repos\Projet.SAAS\<dossier-api>
dotnet user-secrets set "Authentication:Microsoft:ClientId" "TON_CLIENT_ID_ICI"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "TA_VALEUR_DE_SECRET_ICI"
```

Vérifie :

```powershell
dotnet user-secrets list
```

Les 4 clés totales doivent être listées (2 Google + 2 Microsoft).

**✅ Microsoft est prêt.**

---

## Partie 3 — Test en local

### 3.1 Redémarrer le backend

```powershell
cd C:\Users\madoumih\source\repos\Projet.SAAS\<dossier-api>
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

Attends `Now listening on: http://localhost:5202`.

### 3.2 Redémarrer le front

Dans une autre fenêtre :

```powershell
cd C:\Users\madoumih\source\repos\Projet.SAAS\<dossier-front>
npx cross-env NEXT_DISABLE_TURBOPACK=1 next dev
```

### 3.3 Tester Google

1. Ouvre `http://localhost:3000/login` (ou `/register`)
2. Clique sur **Google**
3. Tu es redirigé vers `accounts.google.com` → sélectionne un compte (celui que tu as mis en "utilisateur de test" plus haut)
4. Consentement → **Continuer**
5. Redirection vers Viper → tu dois atterrir sur le dashboard, connecté
6. Vérifie en base :
   ```sql
   SELECT "Email", "TenantId", "GoogleSubjectId" FROM "Users" ORDER BY "CreatedAt" DESC LIMIT 1;
   ```
   Le user doit exister avec ton email et un `GoogleSubjectId` rempli.

### 3.4 Tester Microsoft

Même chose avec le bouton Microsoft.

### 3.5 Tester le rattachement tenant

Via SuperAdmin :
1. Va sur `/superadmin/catalog/domains` (ou la route équivalente créée par Claude Code)
2. Ajoute un domaine : `gmail.com` → tenant `CyberMed Innovations` (juste pour tester, tu retireras ensuite)
3. Déconnecte-toi
4. Reconnecte-toi via Google avec un compte gmail.com
5. Vérifie en DB que le user est maintenant rattaché à `CyberMed Innovations` au lieu de Demo

⚠️ **N'oublie pas de retirer le domaine `gmail.com` de la whitelist après le test**, sinon tout user Gmail finira automatiquement chez CyberMed.

---

## Troubleshooting courant

### "redirect_uri_mismatch" côté Google
Les URIs de redirection déclarés dans Google Console doivent correspondre **exactement** (protocole, host, port, path) à celui appelé. Vérifie que tu as bien `http://localhost:5202/api/auth/google/callback` et pas une variante.

### "AADSTS50011" côté Microsoft
Même problème d'URI de redirection mismatch. Retourne dans Azure → ton app → Authentification → URI de redirection.

### "unauthorized_client" ou "invalid_client"
Secret mal copié. Regénère-en un nouveau (Google Console ou Azure Portal), recopie, remets via `dotnet user-secrets set`, redémarre le backend.

### "access_denied"
Écran de consentement OAuth Google pas encore publié (mode test) et user qui tente de se connecter n'est pas dans la liste des testeurs. Ajoute son email dans **Écran de consentement OAuth → Utilisateurs de test**.

### "email_verified = false"
Rare sur Google, plus fréquent sur Microsoft avec des comptes @hotmail/@outlook non vérifiés. Demande à l'user de vérifier son email côté Microsoft avant de réessayer.

### Les boutons disent "SSO non configuré"
Les `dotnet user-secrets` n'ont pas été lus. Vérifie :
- Que tu es dans le bon dossier (celui du projet API avec le `.csproj`)
- Que `dotnet user-secrets list` retourne bien les 4 clés
- Que tu as bien redémarré le backend après avoir set les secrets

---

## Passage en production (sentys.fr)

> ⚠️ Les **URIs de redirection** déclarés chez Google / Azure doivent correspondre **exactement** au
> `CallbackPath` configuré dans `Program.cs` — c'est l'URL sur laquelle le provider renvoie. Ce sont :
> - Google    : `https://sentys.fr/api/auth/google/callback`
> - Microsoft : `https://sentys.fr/api/auth/microsoft/callback`
> (PAS de `/oauth/` dans le path — c'est le challenge côté front, pas le callback OAuth.)

1. **Déclarer les redirect URIs de prod** dans Google Console (Identifiants → ton client OAuth → Ajouter un URI)
   et Azure (Inscriptions d'applications → Authentification → Ajouter un URI) :
   - `https://sentys.fr/api/auth/google/callback`
   - `https://sentys.fr/api/auth/microsoft/callback`
   (On peut garder en parallèle les URIs `http://localhost:5202/...` pour le dev.)
2. **Écran de consentement Google** en mode **Production** (formulaire de vérification Google, ~2 semaines).
3. **Secrets en variables d'environnement** sur le serveur (jamais dans le code/git). Dans le unit systemd
   `sentys-backend.service`, section `[Service]` :
   ```ini
   Environment=Authentication__Google__ClientId=xxxxxx.apps.googleusercontent.com
   Environment=Authentication__Google__ClientSecret=GOCSPX-xxxxxx
   Environment=Authentication__Microsoft__ClientId=00000000-0000-0000-0000-000000000000
   Environment=Authentication__Microsoft__ClientSecret=xxxxxx
   Environment=FrontendUrl=https://sentys.fr
   ```
   (Le double underscore `__` mappe la hiérarchie `Authentication:Google:ClientId`.) Puis `systemctl daemon-reload && systemctl restart sentys-backend.service`.
4. Tant que `ClientId` est vide, les boutons SSO sont **masqués** automatiquement (`/api/auth/sso-status`).
5. `Secure = true` est déjà forcé sur les cookies hors dev (cf. `!_env.IsDevelopment()`).

### Comportements à connaître
- **email_verified** : un compte dont le provider renvoie `email_verified=false` est **refusé** (`?error=email_not_verified`).
- **Liaison de compte** : la connexion SSO est rattachée au compte local **de même email** (pas de doublon) ;
  un compte peut cumuler password + Google + Microsoft (`AuthProvider="multi"`).
- **Tenant d'un nouveau compte SSO** : résolu par le domaine email (`TenantEmailDomains`, si auto-provisioning
  activé) ; sinon **fallback tenant Demo**. Le rattachement à une vraie entreprise se fait ensuite via le QR/invitation.

---

**Tu es prêt. Fais Google d'abord, teste, puis Microsoft.**
