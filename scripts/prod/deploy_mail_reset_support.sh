#!/usr/bin/env bash
# =============================================================================
# DÉPLOIEMENT — Mot de passe oublié (sécurisé) + Mail support (Brevo)
# Sur la PROD existante (sentys.fr). À EXÉCUTER PAR L'UTILISATEUR.
# Claude Code ne peut pas SSH vers la prod → ce script est le livrable.
#
# Code déjà commité + poussé : branche feat/maj-secu-qr-email (commit 951ea79).
# Nouvelle migration attendue : 20260715014205_AddPasswordResetAndSecurityStamp
#   (colonne Users.SecurityStamp + table PasswordResetTokens)
#
# USAGE :
#   bash deploy_mail_reset_support.sh        # via ssh depuis votre PC
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
$RUN "cd /home/ubuntu/sentys && git fetch origin && git checkout ${BRANCH} && git pull --ff-only"

section "2. Backend : publish"
$RUN "cd /home/ubuntu/sentys/backend && dotnet restore && dotnet publish -c Release -o publish 2>&1 | tail -3"

section "2b. Migrations : état AVANT (contrôle du gap)"
# On veut voir ce qui est 'Pending'. Idéalement, SEULE AddPasswordResetAndSecurityStamp doit l'être.
$RUN "cd /home/ubuntu/sentys/backend && DBP=\$(grep 'Mot de passe BDD' /home/ubuntu/sentys-credentials.txt | cut -d':' -f2 | xargs); \
      ASPNETCORE_ENVIRONMENT=Production dotnet ef migrations list \
      --connection \"Host=localhost;Database=ctf_training;Username=sentys_app;Password=\$DBP\" 2>&1 | tail -12"
confirm "Le listing ci-dessus ne montre QUE AddPasswordResetAndSecurityStamp en (Pending) ? Appliquer ?" || {
  echo "⚠️ Gap inattendu. NE PAS forcer 'ef database update' (risque de drift comme en local)."
  echo "   → Appliquer alors UNIQUEMENT la migration de cette feature en SQL idempotent :"
  echo "     psql ... -f scripts/prod/pwreset_migration_only.sql   (généré si besoin)"
  exit 1
}

section "2c. Migrations : application"
$RUN "cd /home/ubuntu/sentys/backend && DBP=\$(grep 'Mot de passe BDD' /home/ubuntu/sentys-credentials.txt | cut -d':' -f2 | xargs); \
      ASPNETCORE_ENVIRONMENT=Production dotnet ef database update \
      --connection \"Host=localhost;Database=ctf_training;Username=sentys_app;Password=\$DBP\" 2>&1 | tail -8"

section "2d. Restart backend"
$RUN "sudo systemctl restart sentys-backend && sleep 4 && systemctl is-active sentys-backend"

section "3. Frontend : build + restart"
$RUN "cd /home/ubuntu/sentys/frontend && npm install && npm run build 2>&1 | tail -5"
$RUN "sudo systemctl restart sentys-frontend && sleep 4 && systemctl is-active sentys-frontend"

section "4. Tests de fumée"
$RUN "curl -sS -o /dev/null -w 'site https://sentys.fr -> %{http_code}\n' https://sentys.fr"
$RUN "curl -sS -o /dev/null -w 'health -> %{http_code}\n' https://sentys.fr/api/health"
# forgot-password doit répondre 200 + message neutre (aucune énumération), SANS envoyer si Brevo non configuré
$RUN "curl -sS -X POST https://sentys.fr/api/auth/forgot-password \
      -H 'Content-Type: application/json' -H 'X-Requested-With: XMLHttpRequest' \
      -d '{\"email\":\"inconnu-smoke@example.com\"}' -w '\nforgot-password -> %{http_code}\n'"
# migration bien enregistrée
$RUN "DBP=\$(grep 'Mot de passe BDD' /home/ubuntu/sentys-credentials.txt | cut -d':' -f2 | xargs); \
      PGPASSWORD=\"\$DBP\" psql -h localhost -U sentys_app -d ctf_training -tAc \
      \"SELECT \\\"MigrationId\\\" FROM \\\"__EFMigrationsHistory\\\" WHERE \\\"MigrationId\\\" LIKE '%AddPasswordResetAndSecurityStamp';\""

cat <<'TXT'

✅ Code déployé. ⚠️ POUR QUE LES EMAILS PARTENT RÉELLEMENT (sinon LogOnly = rien n'est envoyé) :
   Éditer /etc/systemd/system/sentys-backend.service, section [Service], puis
   `sudo systemctl daemon-reload && sudo systemctl restart sentys-backend` :

     Environment=Mail__Provider=Brevo
     Environment=Mail__BrevoApiKey=xkeysib-...        # clé Brevo (JAMAIS commitée)
     Environment=Mail__SenderEmail=noreply@sentys.fr  # expéditeur vérifié chez Brevo
     Environment=Mail__SupportEmail=support@sentys.fr # destinataire du formulaire support
     Environment=FrontendUrl=https://sentys.fr        # pour que le lien de reset pointe en prod

   Prérequis Brevo : domaine/expéditeur vérifié (SPF/DKIM) sinon les mails partent en spam / sont rejetés.

🧪 À VÉRIFIER manuellement (avec Brevo configuré) :
   - /forgot-password → saisir un email EXISTANT → recevoir l'email (lien https://sentys.fr/reset-password?token=…)
   - Ouvrir le lien → définir un nouveau mot de passe → login OK avec le nouveau, KO avec l'ancien
   - Les autres sessions (autre navigateur) sont déconnectées (rotation SecurityStamp)
   - Lien expiré/rejoué → message « lien invalide »
   - /support → envoyer un message → reçu sur support@sentys.fr

🔁 ROLLBACK si KO :
   sudo systemctl stop sentys-backend sentys-frontend
   rm -rf /home/ubuntu/sentys && cp -r /home/ubuntu/backups/sentys_predeploy_TS /home/ubuntu/sentys
   PGPASSWORD=... pg_restore -h localhost -U sentys_app -d ctf_training --clean \
       /home/ubuntu/backups/ctf_training_predeploy_TS.dump
   sudo systemctl start sentys-backend sentys-frontend
TXT
