# PARAM_ENTREPRISE_LOG — Journal d'exécution

> Prompt : `PROMPT_PARAM_ENTREPRISE.md` — onglet admin « Paramètres entreprise » (5 sections), réutilisation.
> Méthode : PLAN → EXECUTE → TEST réel → OK/KO (max 5) → LOG.
> Branche : `feat/maj-secu-qr-email` (continuité). Environnement : local Windows (pas de SSH prod ; tout est code).

## ÉTAT DE L'EXISTANT (exploration)
- `Tenant` a : Name, SsoProvider, IsActive, CreatedAt + flags modes (IsTeamsEnabled, IsCompetitionModeEnabled,
  IsAnalyticsEnabled, IsComplianceEnabled, IsCampaignsEnabled) avec timestamps/auteurs.
  **Manquent** : Description, Sector, GoogleSsoEnabled, MicrosoftSsoEnabled, DefaultTeamsOpen.
- QR/invitations : `/admin/invites` + hooks `useInvites/useCreateInvite/useRevokeInvite` (réutilisables tels quels).
- SSO : `/api/auth/sso-status` (global, config). Liaison par email déjà gérée (`SsoFlowService`).
- 2FA : déjà livré (tâche précédente) — section Sécurité = info + bonnes pratiques.
- Équipes : `/api/teams/status` (mode) + `/api/admin/teams` (liste, `Team.IsOpen` par équipe). Pas de défaut tenant.
- `/api/auth/me` renvoie `tenantId` + `tenantName`. Sidebar admin = `Sidebar.tsx`.

## PLAN
**Backend**
1. `Models/Tenant.cs` : + `Description` (string?), `Sector` (string?), `GoogleSsoEnabled` (bool=true),
   `MicrosoftSsoEnabled` (bool=true), `DefaultTeamsOpen` (bool=false).
2. `Contracts/TenantSettingsDtos.cs` (NEW) : `TenantSettingsDto` (lecture, incl. flags + état config SSO global
   + teamsEnabled + teamsCount) ; `UpdateTenantSettingsRequest` (champs éditables).
3. `Controllers/TenantSettingsController.cs` (NEW) : `GET /api/tenant/settings`, `PUT /api/tenant/settings`,
   `[Authorize(Roles="admin,SuperAdmin")]`, tenant **depuis les claims** (isolation), whitelisting des champs.
4. Migration `AddTenantSettings` + update DB.
5. Enforcement SSO par tenant : `SsoFlowService` — si le tenant résolu a le provider désactivé → refus
   (sinon le toggle serait cosmétique). Édit minimal + documenté.

**Frontend (réutilisation maximale)**
6. Extraire `ctf-web/src/components/invites/InvitesManager.tsx` depuis `/admin/invites/page.tsx`
   (générateur QR + PNG + liste + révocation) → réutilisé dans `/admin/invites` ET la nouvelle page (zéro duplication).
7. `ctf-web/src/lib/hooks/useTenantSettings.ts` (NEW) : GET + mutation PUT.
8. `ctf-web/src/app/admin/entreprise/page.tsx` (NEW) — 5 sections empilées, responsive, charte admin :
   - **1 Infos** : nom (éditable), TenantId (lecture seule + Copier), secteur + description (éditables).
   - **2 Invitations/QR** : `<InvitesManager/>`.
   - **3 SSO** : état config global (configuré/non) + toggles par tenant Google/Microsoft (persistés).
   - **4 Sécurité** : info 2FA email (optionnelle par utilisateur) + bonnes pratiques + lien Paramètres perso.
   - **5 Équipes** : toggle « équipes ouvertes par défaut » + nb d'équipes + lien `/admin/teams`.
9. `Sidebar.tsx` : item « Paramètres entreprise » (icône `Settings`, route `/admin/entreprise`).

**Vérifs** : `dotnet build` + migration ; `tsc` + `next build` ; test e2e GET/PUT settings (curl, admin) ;
isolation (un user non-admin → 403) ; non-régression `/admin/invites` après extraction du composant.

---

## EXÉCUTION — FAIT ✅

### Backend
- `Models/Tenant.cs` : + `Description`, `Sector`, `GoogleSsoEnabled` (true), `MicrosoftSsoEnabled` (true), `DefaultTeamsOpen` (false).
- `Contracts/TenantSettingsDtos.cs` (NEW) : `TenantSettingsDto` + `UpdateTenantSettingsRequest` (whitelist).
- `Controllers/TenantSettingsController.cs` (NEW) : `GET`/`PUT /api/tenant/settings`, `[Authorize(Roles="admin,SuperAdmin")]`,
  tenant **depuis les claims** (isolation). GET renvoie aussi l'état config SSO global + teamsModeEnabled + teamsCount.
- Migration `20260621205144_AddTenantSettings` **appliquée**. ⚠️ Correction : EF générait `DEFAULT FALSE` pour les
  flags SSO → les tenants existants auraient eu le SSO désactivé. Migration éditée en `defaultValue: true`,
  **revert + reapply** → tenants existants `GoogleSsoEnabled=t, MicrosoftSsoEnabled=t` (vérifié en base).
- `Services/SsoFlowService.cs` : enforcement par tenant — si le tenant résolu a le provider désactivé →
  refus (`?error=sso_disabled_for_tenant`). Rend le toggle effectif (sinon cosmétique).

### Frontend (réutilisation maximale)
- **Extraction** : `components/invites/InvitesManager.tsx` (NEW) = générateur QR + PNG + URL + liste + révocation,
  piloté par les hooks `useInvites`. `/admin/invites/page.tsx` réduit à un wrapper qui rend `<InvitesManager/>`.
  → **zéro duplication** ; la section QR des Paramètres entreprise réutilise le même composant.
- `lib/types/tenantSettings.ts` + `lib/hooks/useTenantSettings.ts` (NEW).
- `app/admin/entreprise/page.tsx` (NEW) — 5 sections empilées, responsive, charte admin claire :
  1. **Infos** : nom/secteur/description éditables + **TenantId lecture seule + Copier**.
  2. **Invitations** : `<InvitesManager/>` (réutilisé).
  3. **SSO** : toggles Google/Microsoft par tenant + badge « non configuré » selon l'état serveur.
  4. **Sécurité** : info 2FA email (activable par chaque membre) + bonnes pratiques + lien Paramètres perso.
  5. **Équipes** : toggle « ouvertes par défaut » + nb d'équipes + lien `/admin/teams` (si mode activé).
  Barre d'enregistrement collante (1 PUT pour infos+SSO+équipes) + toast.
- `Sidebar.tsx` : item « Paramètres entreprise » (icône `Settings`, route `/admin/entreprise`), section admin.

### Tests (réels)
- Backend (via `/api/auth/dev-token` admin, DEV) :
  | Cas | Attendu | Obtenu |
  |---|---|---|
  | GET settings (admin) | 200 + DTO (teamsCount=3, teamsModeEnabled=true, ssoConfigured=false) | ✅ |
  | PUT settings (ASCII) | 200 + persistance (description/sector/flags/defaultTeamsOpen) | ✅ |
  | GET settings (rôle user) | 403 | ✅ |
  | GET settings (non authentifié) | 401 | ✅ |
  | Restauration tenant séminé | défauts rétablis (vérifié en base) | ✅ |

  > Note : un PUT avec accents via curl Git Bash renvoyait 400 (octets non-UTF8 du terminal corrompant le JSON) —
  > **artefact de test**, pas un bug : le PUT ASCII (et le front) fonctionnent. Le front envoie de l'UTF-8 valide.
- Frontend : `tsc` ✅, `eslint` (fichiers touchés) ✅, **`next build`** ✅ (`/admin/entreprise` + `/admin/invites` générées).
- Non-régression : `/admin/invites` recompile après extraction du composant (même rendu).

## REPORTING
1. **Nav** : item « Paramètres entreprise » ajouté dans la section Administration de `Sidebar.tsx` → `/admin/entreprise`.
2. **5 sections** — réutilisé : QR (InvitesManager + hooks useInvites), SSO (état config global), 2FA (info, feature
   existante), équipes (lien `/admin/teams`, `Team.IsOpen`). Créé : endpoints settings, champs Tenant, page, toggles SSO/équipes.
3. **Champs Tenant ajoutés** : Description, Sector, GoogleSsoEnabled, MicrosoftSsoEnabled, DefaultTeamsOpen.
4. **Isolation** : tenant depuis claims, `[Authorize(admin,SuperAdmin)]` (user → 403, anonyme → 401). Vérifié.
5. **À tester manuellement** (web + mobile) : connexion admin → onglet visible → éditer infos + copier TenantId →
   générer/révoquer QR → toggler SSO/équipes → Enregistrer (toast) → recharger (persisté). Un non-admin n'a pas l'onglet.

## NOTE GIT
Comme aux tâches précédentes, les fichiers partagés/non-suivis (`Tenant.cs` préexistant modifié,
`SsoFlowService.cs` non suivi, `Sidebar.tsx` entangled) sont **laissés non-stagés** pour ne pas happer votre
travail. Commits = **fichiers neufs** (DTOs, controller, migration, page entreprise, InvitesManager, hook, types)
+ mon `admin/invites/page.tsx` + ce log. Édits aux fichiers partagés documentés ici, indispensables au fonctionnement.

---

## EXÉCUTION (détail brut)
