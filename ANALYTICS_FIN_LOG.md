# ANALYTICS_FIN_LOG.md — Rapport financier + Erreurs par comportement

> Suivi de PROMPT_ANALYTICS_FINANCIER.md. Autorité : CLAUDE.md (charte §2, qualité §1, sécu §3/6/7, RGPD §4, rapport §5).
> Travail 100% LOCAL. Aucun push, aucun déploiement.

---

## 0. Backup (méthode §1)
- `backups/analytics-fin-20260707_161520/`
  - `front/analytics/page.tsx` (page analytics actuelle)
  - `back/EnterpriseAnalyticsController.cs` + `back/EnterpriseAnalyticsDtos.cs`
  - `ctf_training_20260707_161520.sql` (dump partiel — stoppé par RLS FORCE sur `assignments`)
  - `ctf_training_schema_only.sql` (schéma complet, 4000 lignes — filet propre)
- ⚠️ Dump data complet impossible sans superuser `postgres` (RLS FORCE). Filet réel = **git** (code commité) + schéma. BDD locale = données de dev/seed reproductibles → risque faible.

---

## 1. Inventaire (existant vs manquant)

### Frontend — `ctf-web/src/app/admin/analytics/page.tsx`
- 3 onglets : `entreprise` / `groupe` / `individuel` (tableau ligne ~78, render conditionnel ~88).
- Composant réutilisable `AnalyticsDetail({ basePath, keyPrefix, showExport?, engagementOverride? })`.
- Helpers dispo : `GlassCard`, `EmptyState`, `SkelBlock` (inline) ; `VisionKpiCard`, `VisionAreaChart`, `VisionGauge`, **`VisionBarChart`** (`@/components/vision/*`) ; `Reveal`, `Stagger/StaggerItem`, `CountUp` (animations, respectent `prefers-reduced-motion`).
- ➕ 4e onglet "Rapport financier" = ajouter au tableau d'onglets + render `<FinancialTab />`.

### Backend — `EnterpriseAnalyticsController.cs`
- Route `api/analytics/enterprise`, `[Authorize(Roles="admin,SuperAdmin")]`, `GuardAsync` (tenant JWT + ModeToggle Analytics).
- Endpoints existants : enterprise `{weak-topics,risk,engagement,export}`, groups `[/{teamId}/...]`, users `[/{userId}/{weak-topics,risk,profile}]`.

### Modèle de données (pertinent)
- **`ChallengeCompletion`** : `UserId, TenantId, ChallengeId, ChallengeTitle, PointsEarned, ScorePercent (0-100), IsDemo, DurationSeconds, CompletedAt`. → granularité **pass/fail + score**, une ligne = une complétion.
- **`Submission`** : `TenantId, UserId, ChallengeId, AttemptNo, IsCorrect (bool), ScoreAwarded, SubmittedAt`. → **par tentative**, on sait si correct/faux. Pas de texte de réponse ni de sous-question.
- **`Challenge`** : `Category (string? libre)`, `Type`, `TenantId`, `ModuleId`, `Points`, `Status`… → **`Category`** = seule notion thématique.
- **`RiskScoreHistory`** : `UserId, TenantId, Score (0-100, CRI), Components (json), ComputedAt`. → évolution CRI mensuelle (vraie donnée pour la courbe).
- **`Tenant`** : porte déjà des flags (IsAnalyticsEnabled, etc.). **Aucun** champ financier. Pas d'entité `TenantSettings` dédiée.

### Manquant
- Part A : aucune donnée d'incident cyber réel tracké ; aucune config financière.
- Part B : **aucun champ "comportement à risque"**. `Challenge.Category` existe mais en texte libre (~59 variantes), sans les buckets "USB / verrouillage session" demandés.

---

## 2. NOTIONS À DÉFINIR — en attente de validation (méthode §4)

### 2.A — Modèle financier (Part A) — VALIDÉ par l'utilisateur (calcul par tête, ancré réel)
Le montant "évité" est une **ESTIMATION** (jamais un fait). Modèle actuariel demandé : stats moyennes d'incidents × coût moyen, **rapporté à l'effectif enregistré** et **pondéré par la progression de formation réelle** (parcours/CRI).

**Formule (affichée dans l'encart méthodo) :**
```
Perte potentielle évitée (estimation) /an = N × p × C × h × r × t
```
| Symbole | Signification | Nature |
|---|---|---|
| **N** | Nb de salariés enregistrés du tenant | ✅ VRAIE donnée |
| **p** | Proba annuelle qu'un salarié soit impliqué dans un incident | 🔧 hypothèse éditable |
| **C** | Coût moyen d'un incident (€) | 🔧 hypothèse éditable |
| **h** | Part des incidents liés au facteur humain | 🔧 hypothèse éditable |
| **r** | Réduction du risque humain via sensibilisation | 🔧 hypothèse éditable |
| **t** | Couverture réelle de la formation = participation × (CRI moyen/100) | ✅ VRAIE donnée (intègre parcours faits) |

**Valeurs par défaut proposées** (ajustables en UI, sources citées dans l'encart) :
| Param | Défaut | Source / justification |
|---|---|---|
| p | **0,10** (10 %/an) | Proxy prudent par salarié (rapports FR : ~½ des entreprises touchées/an ; facteur humain dominant). |
| C | **466 000 €** _(maj 2026-07-07)_ | Coût moyen d'une cyberattaque pour une PME française ≈ 466 000 € (études sectorielles France, tous coûts confondus). Remplace l'ancien défaut prudent 50 000 €. Reste éditable en UI. |
| h | **95 %** _(maj 2026-07-07)_ | World Economic Forum : ~95 % des violations de données ont une origine humaine. Remplace l'ancien défaut 68 % (Verizon DBIR). Reste éditable en UI. |
| r | **25 %** | Fourchette basse des études sur la baisse de susceptibilité au phishing post-formation. Disclaimer : indicatif, non garanti. |
| t | *calculé* | participation (% users ≥1 complétion) × (CRI moyen/100). Vraie donnée du tenant. |

→ Pondération par couverture réelle **t** = OUI (décision utilisateur : « on prend en compte le nombre de parcours »).
→ **Reste à valider** : les 4 défauts p/C/h/r (ou ajustés), la persistance (2.B), Part B (2.C).

### 2.B — Persistance des hypothèses
| Option | Détail | Migration ? | Charte |
|---|---|---|---|
| **In-memory (défauts + saisie live)** | State React, recalcul direct, export CSV inclut les valeurs. Pas de persistance entre sessions. | Non | ✅ (pas de localStorage) |
| **Serveur (colonnes Tenant)** | Persisté par tenant, rechargé à l'ouverture. | Oui (migration EF) | ✅ |

→ **À valider** : in-memory (rapide, sans migration) **recommandé** pour cette passe, vs serveur (persistant).

### 2.C — Mapping "comportement à risque" (Part B) — proposition
Pas de champ dédié. Proposition : **regrouper `Challenge.Category` en buckets de comportement**, via une table de correspondance **côté code** (pas de migration, pas d'invention de donnée — on agrège seulement du réel existant). Taux d'erreur par bucket = échecs / tentatives (via `Submission.IsCorrect` et/ou `ChallengeCompletion.ScorePercent` sous seuil).

| Bucket comportement | Categories rattachées (exemples réels) |
|---|---|
| **Phishing / e-mails piégés** | Analyse Email, Phishing, Phishing d'identifiants, Business Email Compromise, Pièces jointes, Macros Office, Conversation piratée, Arnaque Web, Analyse de domaine |
| **Mots de passe & authentification** | Authentification, Robustesse mot de passe, Attaque MFA fatigue, Credential stuffing, 2FA & passkeys |
| **Ingénierie sociale & fraude** | Ingénierie Sociale, Usurpation expert-comptable, Arnaque au président, FOVI, Fausse facture, Deepfake vocal, SWIFT / Wire fraud, Wire fraud, Fraude interne |
| **Sécurité physique / poste de travail** | Sécurité Physique, Sécurité physique / ATM, Hygiène numérique |
| **Données sensibles & conformité** | RGPD — Violation, RGPD — Base légale, HDS — Sous-traitance, Téléconsultation, AML / LCB-FT, KYC bancaire, Politique interne, Procédures internes |
| **Autres / non classé** | (toute Category non mappée) |

⚠️ **Limite honnête (zéro invention)** : les comportements "brancher une clé USB inconnue" et "verrouiller sa session" **ne sont pas tracés** comme catégories distinctes en base. Ils sont ici absorbés dans *Sécurité physique / poste de travail*. Pour les isoler, il faudrait un **nouveau champ** `RiskBehavior` sur `Challenge` (migration + backfill) — à décider.

→ **À valider** : (1) l'approche mapping-en-code (recommandée) vs nouveau champ en base ; (2) les buckets ci-dessus ; (3) emplacement du bloc (onglet Entreprise et/ou Groupe).

---

## 3. Plan d'exécution
1. **Part A** (après validation 2.A/2.B) : endpoint `GET /api/analytics/enterprise/financial?months=N` (vraie donnée d'ancrage : incidents proxy = activité, couverture, CRI mensuel) → DTO → onglet `FinancialTab` (KPI estimé + 3 graphes animés + panneau hypothèses éditable + encart méthodo/disclaimer + export CSV) → 3 états → build → commit.
2. **Part B** (après validation 2.C) : endpoint `GET /api/analytics/enterprise/behaviors` (taux d'erreur par bucket) → DTO → bloc "Erreurs par comportement à risque" (VisionBarChart, code couleur charte) → build → commit.

## 4. Décisions validées (utilisateur, 2026-07-07)
- **2.A défauts** : p=10 %/an, C=50 000 €, h=68 % (Verizon DBIR 2024), r=25 %. Pondération par couverture réelle **t** = OUI.
- **2.B persistance** : **in-memory** (simulateur live + export CSV), aucune migration.
- **2.C Part B** : **mapping en code** (buckets §2.C), onglet **Entreprise**, aucune migration. USB/verrouillage session absorbés dans « Sécurité physique » (limite documentée).

## 5. Journal
- 2026-07-07 : backup + inventaire + propositions. Validation reçue. Début Part A (endpoint financial + onglet).

### PASSE A — Onglet « Rapport financier » (TERMINÉ)
**Backend** :
- DTOs `FinancialTrendPointDto` + `FinancialAnalyticsDto` (base RÉELLE : N, participation, CRI mensuel, couverture t ; **aucune hypothèse côté serveur**).
- Endpoint `GET /api/analytics/enterprise/financial?months=N` + `ComputeFinancialAsync` (3 requêtes, agrégation en mémoire, **pas de N+1**, `AsNoTracking`, `!IsDemo`, tenant JWT via GuardAsync, `[Authorize(admin,SuperAdmin)]`).

**Frontend** (`admin/analytics/page.tsx`, 4e onglet) :
- KPI phare **« Perte potentielle évitée (estimation) · 1 an »** (compteur € animé, formaté fr-FR) + 3 KPI (salariés, couverture %, coût/incident, incidents attendus/an — étiquetés estimation/hypothèse).
- 3 graphes animés : évolution des pertes évitées estimées (area), couverture réelle (area), activité de formation (barres).
- **Panneau hypothèses éditable** (p/C/h/r) → recalcul **live** + bouton réinitialiser.
- **Encart méthodologie + disclaimer** (formule, sources Verizon DBIR/IBM/ANSSI, avertissement « estimation, pas une garantie »).
- **Export CSV client-side** incluant les hypothèses (traçabilité).
- 3 états (chargement/données/vide), animations sobres (Reveal/Stagger/compteurs, `prefers-reduced-motion` respecté).

**Formule** : `Perte évitée (est.) /an = N × p × C × h × r × t` — N & t réels, p/C/h/r hypothèses éditables (défauts 10 %/50 000 €/68 %/25 %). Vocabulaire prudent partout (« estimation », jamais « vous avez économisé »).

**Vérifs** : build backend **OK** (0 err/warn) ; frontend `tsc --noEmit` **OK**, `npm run build` **OK**, `eslint` **0** ; **0 hex en dur** ; runtime : `/api/analytics/enterprise/financial` → **401** sans auth, health 200, aucune erreur de démarrage. Sécurité/charte/RGPD OK. → **commit local**.

### PASSE B — Bloc « Erreurs par comportement à risque » (TERMINÉ)
**Backend** :
- DTOs `BehaviorRowDto` + `BehaviorErrorsDto`.
- Endpoint `GET /api/analytics/enterprise/behaviors` + `ComputeBehaviorsAsync` (join complétions→challenges, tenant JWT, `!IsDemo`, `AsNoTracking`, pas de N+1).
- **Mapping en code** `BehaviorOf(category)` par **mots-clés** (robuste aux ~59 variantes) → buckets : Phishing / e-mails piégés · Mots de passe & authentification · Ingénierie sociale & fraude · Sécurité physique / poste de travail · Données sensibles & conformité · Autres.
- **Taux d'échec** = complétions `ScorePercent < 50` / total, par comportement. Tri décroissant (pire d'abord).
- ⚠️ Limite documentée : USB / verrouillage session non tracés séparément → mots-clés `usb`/`verrouill`/`session` rattachés au bucket « Sécurité physique / poste de travail » (pas de bucket distinct sans nouveau champ en base).

**Frontend** (onglet **Entreprise**, sous le détail standard) :
- Bloc « Erreurs par comportement à risque » : lignes triées pire→meilleur, badge taux d'échec + barre **code couleur charte** (danger ≥60 / alerte ≥40 / accent <40), libellé Critique/À surveiller/Maîtrisé, score moyen + n échecs. Lisible en 3 s. 3 états, animé (Reveal).

**Vérifs** : build backend **OK** ; `tsc`/`build`/`eslint` **OK** ; 0 hex ; route `/behaviors` → **401**. Sécurité/charte/RGPD OK.

---

## 6. RAPPORT FINAL (§5)
Les **2 ajouts** demandés sont livrés en local, séparément et testés :
- **A. Onglet « Rapport financier »** : KPI phare *perte potentielle évitée (estimation)* (compteur € animé), 3 graphes animés, panneau hypothèses **p/C/h/r éditable** (recalcul live), **encart méthodo + disclaimer** (sources citées), **export CSV** avec hypothèses. Modèle `N×p×C×h×r×t` — **N & t = vraies données**, p/C/h/r = **hypothèses clairement étiquetées** (jamais présentées comme des faits). Vocabulaire prudent partout.
- **B. Erreurs par comportement à risque** : taux d'échec réel par comportement (mapping `Category`→bucket en code, validé), code couleur charte, décisionnel, onglet Entreprise.

**Conformité** : `[Authorize(admin,SuperAdmin)]` + tenant JWT + DTO + `AsNoTracking` + pas de N+1 + `!IsDemo` (pas de fallback démo) ; charte violet stricte, **0 hex en dur**, **0 vert cyber**, animations sobres (`prefers-reduced-motion`), contraste AA, **3 états** partout ; RGPD (isolation tenant, aucune donnée perso hors périmètre). 2 builds OK à chaque passe. **100 % LOCAL — aucun push, aucun déploiement.**

**Notes / réserves** :
- Validation visuelle sur vraies données à faire **connecté** (CyberMed a des complétions + CRI). Le calcul financier bouge avec l'effectif et la couverture réelle.
- Hypothèses financières **in-memory** (réinit au reload) — persistance serveur = évolution possible (migration).
- USB / verrouillage session non isolables sans champ `RiskBehavior` en base (migration + backfill) — à décider ultérieurement.
- **PDF** = TODO (CSV livré).

## 7. Journal (suite)
- 2026-07-07 : Part A commitée (`8ccceab`). Part B codée + testée → commit. **STOP** (critère d'arrêt atteint).
- 2026-07-07 : maj défauts financiers déployée (C=466 000 €, h=95 %).
- 2026-07-07 : **pastille de risque** dans l'onglet Individuel. Backend : `AnalyticsUserDto` expose `Risk` (dernier CRI). Front : `RiskPill` selon le CRI (haut=mieux) — 🟢 Faible (≥60) · 🟡 Modéré (40-59) · 🟠 À risque (25-39) · 🔴 Gros risque à traiter (<25) · gris « Non évalué ». Affichée dans la liste des collaborateurs + en-tête du détail. Tokens charte (success/warning/danger + color-mix pour l'orange), 0 hex. Builds OK, route 401.
