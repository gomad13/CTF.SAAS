# REFONTE_GLOBALE_LOG.md — Vision UI violet sur TOUT le site (LOCAL)

> Charte AUTORITÉ : thème unique bleu nuit / violet Vision UI (section 2). `#0B1437` / `#111C44` / `#1A2456` / `#2A3568` / `#FFFFFF` / `#A0AEC0` / accent `#7551FF`→`#582CFF` / dégradés violet→cyan `#582CFF`→`#2CD9FF`.
> Référence de qualité : **dashboard + sidebar** déjà refondus. 100% LOCAL, pas de push. UNIQUEMENT l'affichage.
> Backup : `backups/refonte-globale-20260706/frontend.tgz`.

## Stratégie (pourquoi ce n'est pas un « big-bang » risqué)
Tout le front est **déjà tokenisé** (0 hex en dur hors exceptions documentées) et **déjà animé** (Reveal/Stagger/CountUp/hover/focus sur les 45 pages) par les passes précédentes. Les **alias historiques** (`--pr, --primary, --bg-card, --c-*`…) pointent tous vers les tokens cœur.
→ **Base globale** : overrider les ~30 tokens cœur en violet dans `globals.css` (approche `:root` de la charte) + passer la palette dataviz (`chart-colors.ts`) en violet → **tout le site bascule en violet**, charts inclus, animations conservées. Changement de **définition de thème**, réversible, sans toucher au JSX des pages.
→ **Polish page par page** ensuite : glassmorphism / dégradés premium / KPI mis en valeur là où une page reste en-deçà de la référence ; build + commit par lot.

## ÉTAPE 0 — Inventaire des pages (définition de « fini »)
Statut : `[ ]` à vérifier/polir · `[x]` conforme (violet + animations + build OK).

| #  | Route | Statut |
|----|-------|--------|
| 1  | `/` (root/redirect) | [ ] |
| 2  | `/[id]` | [ ] |
| 3  | `/account/consents` | [ ] |
| 4  | `/admin` | [ ] |
| 5  | `/admin/analytics` | [ ] |
| 6  | `/admin/campaigns` | [ ] |
| 7  | `/admin/campaigns/[id]` | [ ] |
| 8  | `/admin/catalog` | [ ] |
| 9  | `/admin/compliance` | [ ] |
| 10 | `/admin/dashboard` | [ ] |
| 11 | `/admin/directory` | [ ] |
| 12 | `/admin/entreprise` | [ ] |
| 13 | `/admin/scenarios` | [ ] |
| 14 | `/admin/scenarios/instances` | [ ] |
| 15 | `/admin/scenarios/instances/[id]` | [ ] |
| 16 | `/admin/scenarios/launch/[templateId]` | [ ] |
| 17 | `/admin/teams` | [ ] |
| 18 | `/admin/teams/[id]` | [ ] |
| 19 | `/admin/users` | [ ] |
| 20 | `/campaigns/me` | [ ] |
| 21 | `/cgu` | [ ] |
| 22 | `/coaching/history` | [ ] |
| 23 | `/dashboard` (TÉMOIN) | [x] |
| 24 | `/dashboard/admin` | [ ] |
| 25 | `/dashboard/challenge/[id]` | [ ] |
| 26 | `/dashboard/competition` | [ ] |
| 27 | `/dashboard/equipes` | [ ] |
| 28 | `/dashboard/parametres` | [ ] |
| 29 | `/dashboard/parcours` | [ ] |
| 30 | `/dashboard/parcours/[id]` | [ ] |
| 31 | `/demo` | [ ] |
| 32 | `/feedback` | [ ] |
| 33 | `/forgot-password` | [ ] |
| 34 | `/inbox` | [ ] |
| 35 | `/inbox/[emailId]` | [ ] |
| 36 | `/join` | [ ] |
| 37 | `/landing` | [ ] |
| 38 | `/legal/[slug]` | [ ] |
| 39 | `/login` | [ ] |
| 40 | `/mentions-legales` | [ ] |
| 41 | `/paths/[pathId]/module/[modulesId]` | [ ] |
| 42 | `/privacy` | [ ] |
| 43 | `/register` | [ ] |
| 44 | `/reset-password` | [ ] |
| 45 | `/scenarios/landing/[token]` | [ ] |
| 46 | `/superadmin` | [ ] |
| +  | sidebar (chrome commun) | [x] |

## Journal
(en cours)

## Bugs fonctionnels repérés ailleurs (notés, non corrigés)
(aucun pour l'instant)
