# UIUX_PROMAX_LOG.md

Installation du skill **UI/UX Pro Max** (open-source MIT, gratuit) + règle d'articulation avec la charte Sentys. Travail 100 % repo local (Windows), aucune modif serveur.

---

## [2026-07-03] — Installation + test + règle charte

### Pré-requis vérifiés
- **Node** v24.13.1 ✓ · **npm** 11.12.1 ✓
- **Python 3** : **absent** au départ (seul le stub Microsoft Store répondait). → **Installé** via `winget install --id Python.Python.3.12 -e --scope user --silent` → **Python 3.12.10** à `C:\Users\madoumih\AppData\Local\Programs\Python\Python312\python.exe` (scope utilisateur, sans admin). Le moteur de recherche du skill en a besoin.

### Méthode d'installation qui a marché
```bash
# Repo vérifié réel avant install : git ls-remote https://github.com/nextlevelbuilder/ui-ux-pro-max-skill -> OK
npx --yes skills add https://github.com/nextlevelbuilder/ui-ux-pro-max-skill --skill ui-ux-pro-max
```
- **Méthode A (skills CLI)** → succès. (Méthodes B `uipro-cli` non nécessaires.)
- **Emplacement réel** : `.agents/skills/ui-ux-pro-max/` (SKILL.md + `scripts/` + `data/`, ~1.9 Mo), **symlinké** dans `.claude/skills/ui-ux-pro-max` → Claude Code le lit automatiquement.
- **Assessment sécurité** affiché par l'installeur : Socket = 0 alerte, Snyk = Low Risk, un scanner « Gen » = High Risk. Installé sur demande explicite (skill design MIT open-source). Les scripts sont du Python **standard-lib** (aucune dépendance pip, `search.py --help` OK).

### Preuve : la recherche répond
```bash
python .agents/skills/ui-ux-pro-max/scripts/search.py "healthcare SaaS dashboard" --design-system
python .agents/skills/ui-ux-pro-max/scripts/search.py "healthcare SaaS dashboard" --design-system --stack nextjs
```
Retourne un **design system complet** : PATTERN (Real-Time/Operations Landing), STYLE (« Accessible & Ethical », WCAG AAA, 16px+, focus states), COLORS, TYPOGRAPHY (Fira Code/Fira Sans), KEY EFFECTS (focus rings 3-4px, ARIA, cibles 44×44px), AVOID, et une PRE-DELIVERY CHECKLIST a11y. Version `--stack nextjs` identique adaptée au stack. ✅ Skill fonctionnel (50+ styles, 161 palettes, 57 pairings de fonts, 99 guidelines UX, 25 types de charts, 10+ stacks dont nextjs/shadcn/tailwind).

### Règle d'articulation avec la charte Sentys (inscrite)
Nouveau skill complémentaire **`.claude/skills/ui-ux-pro-max-charte-sentys/SKILL.md`** :
> **ui-ux-pro-max guide la STRUCTURE / layout / UX / accessibilité / dataviz. La PALETTE reste la charte Sentys** : `#03b5aa / #037971 / #023436 / #00bfb3` (WCAG AA). Ne jamais adopter les couleurs suggérées par le skill.

Illustration concrète : pour « healthcare SaaS dashboard » le skill propose un primary **cyan `#0891B2`** → on l'**ignore** et on mappe sur la charte (`primary → #03b5aa` = `bg-sentys`, `accent → #00bfb3`, fond sombre → `#023436`). On garde en revanche sa structure de page, ses patterns, sa checklist a11y et ses recommandations de charts. Renvoie vers [[charte-graphique-sentys]], [[responsive-mobile-sentys]], [[animations-sentys]], [[dataviz-sentys]].

### Git / versionnage
- Le skill tiers vendorisé (`.agents/skills/ui-ux-pro-max/`, 1.9 Mo) et le **symlink** `.claude/skills/ui-ux-pro-max` sont **gitignorés** (réinstallables via la commande `npx skills add` ci-dessus ; un symlink Windows en git est fragile). Ce log fait office de manifeste de réinstallation.
- **Commité** : la règle `ui-ux-pro-max-charte-sentys` + ce log.

## Comment l'utiliser sur les prochaines tâches UI
1. Avant de concevoir/refactorer une page ou un composant, lancer une recherche :
   `python .agents/skills/ui-ux-pro-max/scripts/search.py "<type de page/composant>" --design-system --stack nextjs`
2. **Reprendre** : structure, patterns, hiérarchie, UX, accessibilité (focus/44px/ARIA), types de charts.
3. **Remplacer** ses couleurs par les tokens `sentys*` (charte). Priorité : **charte Sentys > recommandations du skill**.
4. Claude Code lit `ui-ux-pro-max` (auto) + la règle `ui-ux-pro-max-charte-sentys` à chaque session.

## Livrables
1. ✅ UI UX Pro Max installé (`.agents/skills/…`, symlink `.claude/skills/…`) et fonctionnel (recherche prouvée).
2. ✅ Pré-requis : Node ✓ + Python 3.12.10 installé (winget, scope user).
3. ✅ Règle d'articulation charte inscrite (`ui-ux-pro-max-charte-sentys`).
4. ✅ Ce log.
