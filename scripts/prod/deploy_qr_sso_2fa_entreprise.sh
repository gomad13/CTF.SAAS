#!/usr/bin/env bash
# =============================================================================
# DÉPLOIEMENT MAJ — QR invitations + SSO + 2FA email + Paramètres entreprise
# Sur la PROD existante (sentys.fr déjà en service). À EXÉCUTER PAR L'UTILISATEUR.
# Claude Code ne peut pas SSH vers la prod (bloqué) → ce script est le livrable.
#
# ⚠️ PRÉREQUIS OBLIGATOIRE : le code doit être COMMITÉ + POUSSÉ sur origin
#    (branche feat/maj-secu-qr-email) AVANT. Aujourd'hui une partie est encore
#    non-commitée en local — voir la note git dans les *_LOG.md.
#
# ⚠️ Adapter les chemins/noms : ce script suppose le layout décrit dans
#    PROMPT_DEPLOYMENT_OVH_SENTYS.md (backend/ , frontend/). Le repo local
#    s'appelle "CTF. API" / "ctf-web" — vérifier la structure réelle côté prod.
#
# USAGE :
#   bash deploy_qr_sso_2fa_entreprise.sh           # via ssh depuis votre PC
#   (ou RUN="" si vous lancez DIRECTEMENT sur le serveur)
# =============================================================================
set -uo pipefail

SRV="ubuntu@5.196.64.101"
RUN="ssh $SRV"            # mettre RUN="" si exécuté sur le serveur
BRANCH="feat/maj-secu-qr-email"
TS="$(date +%Y%m%d_%H%M%S)"
section(){ echo; echo "▶ $1"; }
confirm(){ read -r -p "👉 $1 [o/N] " a; [[ "$a" == "o" || "$a" == "O" ]]; }

section "0. BACKUP (BDD + code) AVANT toute opération"
$RUN "DBP=\$(grep 'Mot de passe BDD' /home/ubuntu/sentys-credentials.txt | cut -d':' -f2 | xargs); \
      PGPASSWORD=\"\$DBP\" pg_dump -h localhost -U sentys_app -d ctf_training -Fc \
      -f /home/ubuntu/backups/ctf_training_predeploy_${TS}.dump && echo 'OK dump BDD'"
$RUN "cp -r /home/ubuntu/sentys /home/ubuntu/backups/sentys_predeploy_${TS} && echo 'OK copie code'"
$RUN "ls -lh /home/ubuntu/backups/ | tail -4"
confirm "Backups présents et corrects ? Continuer le déploiement ?" || { echo "Abandon."; exit 1; }

section "1. Récupérer le nouveau code (git)"
# Si la prod déploie par git pull :
$RUN "cd /home/ubuntu/sentys && git fetch origin && git checkout ${BRANCH} && git pull --ff-only"
# (Alternative rsync si la prod n'est pas un clone git : voir PROMPT_DEPLOYMENT_OVH_SENTYS.md PHASE 5.)

section "2. Backend : publish + MIGRATIONS BDD + restart"
$RUN "cd /home/ubuntu/sentys/backend && dotnet restore && dotnet publish -c Release -o publish 2>&1 | tail -3"
# Migrations nouvelles attendues : AddTenantInvite, Add2FA, AddTenantSettings
$RUN "cd /home/ubuntu/sentys/backend && DBP=\$(grep 'Mot de passe BDD' /home/ubuntu/sentys-credentials.txt | cut -d':' -f2 | xargs); \
      ASPNETCORE_ENVIRONMENT=Production dotnet ef database update \
      --connection \"Host=localhost;Database=ctf_training;Username=sentys_app;Password=\$DBP\" 2>&1 | tail -8"
$RUN "sudo systemctl restart sentys-backend && sleep 4 && systemctl is-active sentys-backend"

section "3. Frontend : build + restart"
$RUN "cd /home/ubuntu/sentys/frontend && npm install && npm run build 2>&1 | tail -5"
$RUN "sudo systemctl restart sentys-frontend && sleep 4 && systemctl is-active sentys-frontend"

section "4. Tests de fumée"
$RUN "curl -sS -o /dev/null -w 'site https://sentys.fr -> %{http_code}\n' https://sentys.fr"
$RUN "curl -sS -o /dev/null -w 'health -> %{http_code}\n' https://sentys.fr/api/health"
$RUN "DBP=\$(grep 'Mot de passe BDD' /home/ubuntu/sentys-credentials.txt | cut -d':' -f2 | xargs); \
      PGPASSWORD=\"\$DBP\" psql -h localhost -U sentys_app -d ctf_training -tAc \
      \"SELECT \\\"MigrationId\\\" FROM \\\"__EFMigrationsHistory\\\" WHERE \\\"MigrationId\\\" LIKE '%AddTenantInvite' OR \\\"MigrationId\\\" LIKE '%Add2FA' OR \\\"MigrationId\\\" LIKE '%AddTenantSettings';\""

cat <<'TXT'

✅ Déployé. À VÉRIFIER manuellement :
  - Login admin → onglet « Paramètres entreprise » (/admin/entreprise) : 5 sections OK.
  - Générer/révoquer un QR ; ouvrir /join?token=… sur mobile.
  - Paramètres perso → activer/désactiver 2FA (en prod sans Brevo : code en logs ; voir ci-dessous).
  - Login d'un compte 2FA activé → écran code.

🔌 CONFIG OPTIONNELLE (sinon : 2FA en mode log, boutons SSO masqués) — variables d'env systemd
   (/etc/systemd/system/sentys-backend.service, section [Service]) puis daemon-reload + restart :
   Environment=Mail__Provider=Brevo
   Environment=Mail__BrevoApiKey=xkeysib-...
   Environment=Authentication__Google__ClientId=...     (+ ClientSecret)
   Environment=Authentication__Microsoft__ClientId=...  (+ ClientSecret)
   Redirect URIs à déclarer : https://sentys.fr/api/auth/google/callback et /api/auth/microsoft/callback
   (cf. OAUTH_SETUP_GUIDE.md, et la doc Brevo dans MAJ_SECU_QR_LOG.md)

🔁 ROLLBACK si KO :
   sudo systemctl stop sentys-backend sentys-frontend
   rm -rf /home/ubuntu/sentys && cp -r /home/ubuntu/backups/sentys_predeploy_TS /home/ubuntu/sentys
   PGPASSWORD=... pg_restore -h localhost -U sentys_app -d ctf_training --clean \
       /home/ubuntu/backups/ctf_training_predeploy_TS.dump
   sudo systemctl start sentys-backend sentys-frontend
TXT
