# SKILLS_INSTALL_LOG.md

Installation de 5 skills/plugins Claude Code **gratuits / local uniquement**. Aucune clé API payante. Vérifié repo/gratuité AVANT installation. (Machine locale Windows ; les plugins vivent dans `~/.claude/plugins`, les skills `npx` dans `.agents/skills` symlinké `.claude/skills`.)

Pré-requis : Node v24.13.1 · npm 11.12.1 · Python 3.12.10 · `claude` CLI 2.1.201 (support `plugin`) · Bun 1.3.14 (installé pour claude-mem).

---

## Résultat par skill

| # | Skill | Statut | Source (vérifiée) | Déclenchement |
|---|---|---|---|---|
| 1 | **Superpowers** (obra) | ✅ installé, `enabled` (v6.1.1) | marketplace `obra/superpowers-marketplace` (MIT) | commandes `/brainstorm`, `/write-plan`, `/execute-plan`, revue/TDD |
| 2 | **Impeccable** (pbakaus) | ✅ installé (skill) | `npx skills add pbakaus/impeccable` → `.agents/skills/impeccable` (symlink Claude Code) | se déclenche sur les tâches design (polish/audit/critique UI) |
| 3 | **Find Skills** (dan323) | ✅ installé, `enabled` | marketplace `dan323/easier-life-skills` → plugin `find-skills` | annuaire/recherche de skills |
| 4 | **ClaudeMem** | ✅ installé + **worker en marche** (PID, port 37777) | npm **`claude-mem@13.10.0`** — Apache-2.0 — repo **`github.com/thedotmack/claude-mem`** (auteur Alex Newman) | mémoire persistante entre sessions, injectée dès la 2e session ; stockage **local** `~/.claude-mem` |
| 5 | **Claude Council** (hex) | ✅ installé, `enabled` (v2026.7.1) | marketplace `hex/claude-marketplace` → plugin `claude-council` | `/claude-council:ask --local "question"` — **mode local (Claude-only)** |

### Détails ClaudeMem (repo exact demandé)
- **Repo confirmé** : `github.com/thedotmack/claude-mem`, npm `claude-mem@13.10.0`, licence **Apache-2.0** (open-source, gratuit). Les candidats `*/claudemem` classiques n'existaient pas → seul le vrai package npm a été retenu (aucun package douteux installé).
- Installé en **provider `claude`** (utilise l'accès Claude existant, **aucune clé payante**), **runtime worker local**, **télémétrie désactivée** (`telemetry disable`), stockage **100 % local** (`~/.claude-mem`). Auto-memory natif de Claude Code conservé (coexistence).
- Worker : `npx claude-mem start` / `status` / `stop`. Nécessite **Bun** (installé + ajouté au PATH utilisateur Windows) — actif dès une session terminal fraîche.

### Claude Council — mode local uniquement (aucune clé)
- La commande `/claude-council:ask` supporte `--local` : « If `--local` is in $ARGUMENTS → local mode … Claude-only ». Le mode multi-IA (OpenAI/Gemini/Grok) **n'est PAS activé**.
- **Aucune clé API configurée** : `OPENAI_API_KEY`, `GEMINI_API_KEY`, `GOOGLE_API_KEY`, `XAI_API_KEY`, `GROK_API_KEY`, `OPENROUTER_API_KEY`, `ANTHROPIC_API_KEY` = toutes vides. **Aucun coût engagé.**
- Usage recommandé : `/claude-council:ask --local "…"` (avis multi-angles Claude-only, gratuit).

## Note d'articulation (priorités — éviter le chaos)
> **En cas de contradiction, le FICHIER D'INSTRUCTIONS PROJET (CLAUDE.md — charte noir + vert cyber #22C55E) fait autorité. Skills < fichier d'instructions.**

- **Superpowers** → méthodo/workflow (plan, TDD, revue, sous-agents).
- **Impeccable + UI UX Pro Max** → aide au design/structure UX — **MAIS la charte finale = CLAUDE.md** (noir + vert cyber, tokens). Ne pas adopter une palette proposée par un skill.
- **ClaudeMem** → mémoire entre sessions (local, gratuit).
- **Find Skills** → découvrir d'autres skills.
- **Claude Council (--local)** → avis multi-angles Claude-only sur décisions difficiles.
Priorité de résolution : **CLAUDE.md > skills maison (charte-graphique-sentys, etc.) > skills tiers (Impeccable, UI UX Pro Max)**.

## Confirmations
- ✅ 5/5 installés et fonctionnels (repos vérifiés, aucun package douteux).
- ✅ Claude Council en mode **--local** (aucune clé payante).
- ✅ **Aucune clé API payante** configurée, **aucun coût**.
- ✅ ClaudeMem = `thedotmack/claude-mem` (Apache-2.0), local, télémétrie off.
- ✅ Aucun conflit bloquant ; rappel : CLAUDE.md prime.
