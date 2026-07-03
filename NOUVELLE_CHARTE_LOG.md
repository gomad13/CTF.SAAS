# NOUVELLE_CHARTE_LOG.md

Refonte charte : **noir / gris / blanc + accent vert cyber**, mode **sombre (défaut) & clair**, en prod.
Frontend Next.js 16 + Tailwind v4, `/home/ubuntu/sentys/frontend/`, service `sentys-frontend`.

---

## D'où venait le « bleu-marine / teal » (diagnostic)
Le token foundation (globals.css) était bien déployé (dark `#0A0A0B` + vert `#22C55E` présents dans le CSS live), MAIS des couleurs **codées en dur** l'écrasaient :
1. **Navy `#0F172A`** (bleu-marine) : **34 occurrences dans 18 fichiers de layout/sidebar** (`app/dashboard/layout.tsx`, `app/admin/layout.tsx`, `components/Sidebar.tsx`, `app/inbox/layout.tsx`, `app/campaigns/layout.tsx`, pages admin, `Footer`, `CookieBanner`, `ChatWidget`…). C'était **la** source du fond/sidebar bleu-marine : ces éléments hardcodaient `#0F172A` en `style`/`className`, donc les tokens CSS ne les touchaient pas.
2. **Teal** `#03b5aa/#037971/#023436/#00bfb3` : restait **uniquement dans `tailwind.config.ts`** (13 occ, clés `sentys*`/`primary`) — mon grep précédent était scopé à `src/` et ratait la config à la racine. (Le teal `#023436` des pages avait déjà été converti.)

## Ce qui a été fait
### Theming (fondation)
- `globals.css` réécrit en **système de tokens** : mode clair = `:root`, mode sombre = `html.dark` / `[data-theme="dark"]` (défaut). Toutes les classes composant (`.btn/.card/.input/.badge/.table/.tabs/.topbar/.stat/.alert/.toast/.modal…`) repointées sur les tokens → thème-aware dans les 2 modes. `@custom-variant dark` (Tailwind v4) + `darkMode:'class'` dans `tailwind.config.ts`.
- Tokens : sombre `--bg #0A0A0B / --surface #161618 / --surface-2 #1F1F22 / --border #242427 / --text #F5F5F7 / --accent #22C55E` ; clair `--bg #FFFFFF / --text #0A0A0B / --accent #16A34A`. États : succès `#22C55E`, danger `#EF4444`, alerte `#F59E0B`, info `#3B82F6`. Contraste WCAG AA (textes `-t` adaptés par mode).
- **Toggle sombre/clair** : `useTheme` (défaut **dark**, persistance `ctf_theme`, applique `data-theme` + classe `.dark`) + composant `ThemeToggle` (soleil/lune, WCAG, cible 40px) placé dans `AppShell` + page Paramètres. **Script no-FOUC** dans `layout.tsx` (thème appliqué avant le paint) + `<html class="dark" data-theme="dark">` par défaut.

### Remplacements « partout »
- **Teal → 0** partout (src + `tailwind.config.ts` + CSS live). Clés `sentys*` de la config → vert/noir.
- **Navy `#0F172A` → `#0A0A0B`** : 34 → 0 (chrome sidebar/header/footer passé en noir, texte clair conservé → lisible dans les 2 modes).
- **Slate `#1E293B` → `#1F1F22`** sur les fichiers de chrome (layouts/sidebar), **bleu `#3B82F6` → `var(--accent)`** (vert) sur le chrome.
- **Pages converties en tokens** (sous-agents + sweeps) : `login`, `register`, `dashboard`, `admin/dashboard` (103 couleurs), `admin/catalog`, `admin/directory`, composants d'exercices (`MailboxChallenge`, `PhishingAiChallenge`, `PasswordQuiz`, `Multichoice`, `FreeText`, `CeoFraud`, `ChoiceOption`), chrome layouts. Logos de marque (Google/Microsoft) préservés.
- **~415 valeurs de couleur** converties en tokens (hardcoded hex `2150 → 1735` ; le reste = « îlots clairs » lisibles via le garde-fou d'auto-contraste + couleurs d'état sémantiques).

### Fichier d'instructions
- `CLAUDE.md` §5.4 : palette remplacée par la nouvelle charte (tableaux sombre + clair, tokens CSS, note modes/toggle, « jamais de bleu/teal »). Checklist §8 mise à jour.

## PREUVE (live https://sentys.fr)
```
grep teal (023436|03b5aa|037971|00bfb3) sur src+tailwind.config : 0   (VIDE ✅)
CSS live servi : teal 0 · navy 0f172a 0 · dark-bg 0a0a0b présent · vert 22c55e présent
<html class="dark" data-theme="dark">  (mode sombre par défaut)
site https://sentys.fr/login : 200
```
Non-régression : `/login` `/register` 200 ; `/dashboard` `/admin` `/superadmin` 307 (redirection login normale) ; `api/health` 200 ; **backend `sentys-backend` intact** (refonte 100 % frontend). Build `✓ Compiled successfully`.

## Backups
- `/home/ubuntu/backups/frontend_src_before_charte_20260703_164214` (avant refonte)
- `/home/ubuntu/backups/frontend_src_charte2_20260703_170351` + `tailwind_charte2_*.ts`

## Reste (validation visuelle)
~70 fichiers gardent encore des cartes/panneaux en dur (admin profond + quelques écrans) → rendus en **îlots clairs lisibles** (fond clair + texte foncé, via le garde-fou d'auto-contraste) même en mode sombre. Non bloquant, mais à convertir en tokens pour une cohérence 100 % — à valider visuellement (web + mobile, 2 modes).
