#!/usr/bin/env bash
# =============================================================================
# V1 (MAJ prudentes) + V2 (check-up cybersécurité) — Sentys prod
# À EXÉCUTER PAR L'UTILISATEUR sur / depuis le serveur de prod.
# Claude Code (machine de dev locale) ne peut pas SSH vers la prod → ce script
# est le livrable des volets V1 & V2 du prompt PROMPT_MAJ_SECU_QR_EMAIL.md.
#
# Serveur : ubuntu@5.196.64.101 (Ubuntu 24.04)  |  Site : https://sentys.fr
#
# USAGE (depuis votre machine, ou directement sur le serveur sans le préfixe ssh) :
#   bash maj_secu_v1_v2.sh            # tout, avec confirmations
#   bash maj_secu_v1_v2.sh v1         # seulement V1 (mises à jour)
#   bash maj_secu_v1_v2.sh v2         # seulement V2 (audit, lecture seule)
#
# ⚠️ PRUDENCE : pas de backup GitHub du code. Les backups locaux sont faits
#    AVANT toute opération risquée. Soyez prêt à rollback (voir fin de V1).
# =============================================================================
set -uo pipefail

SRV="ubuntu@5.196.64.101"
# Si vous lancez CE script DIRECTEMENT sur le serveur, mettez RUN="" (pas de ssh).
RUN="ssh $SRV"
TS="$(date +%Y%m%d_%H%M%S)"

confirm() { read -r -p "👉 $1 [o/N] " a; [[ "$a" == "o" || "$a" == "O" ]]; }
section() { echo; echo "──────────────────────────────────────────────"; echo "▶ $1"; echo "──────────────────────────────────────────────"; }

# =============================================================================
# VOLET 1 — MISES À JOUR (prudentes, backup d'abord)
# =============================================================================
volet1() {
  section "V1.1 — BACKUP COMPLET (BDD + code) AVANT toute modif"
  $RUN "DB_PASSWORD=\$(grep 'Mot de passe BDD' /home/ubuntu/sentys-credentials.txt | cut -d':' -f2 | xargs); \
        PGPASSWORD=\"\$DB_PASSWORD\" pg_dump -h localhost -U sentys_app -d ctf_training -Fc \
        -f /home/ubuntu/backups/ctf_training_before_maj_${TS}.dump && echo 'OK dump BDD'"
  $RUN "cp -r /home/ubuntu/sentys /home/ubuntu/backups/sentys_before_maj_${TS} && echo 'OK copie code'"
  $RUN "ls -lh /home/ubuntu/backups/ | tail -4"
  echo "✅ Backups créés (suffixe _${TS}). NE PAS continuer si ces lignes sont absentes."

  section "V1.2 — Paquets OS : voir ce qui est upgradable"
  $RUN "sudo apt update && echo '--- UPGRADABLE ---' && sudo apt list --upgradable 2>/dev/null"
  if confirm "Appliquer 'apt upgrade -y' (PAS dist-upgrade) ?"; then
    $RUN "sudo apt upgrade -y"
    echo "Si un service a besoin d'un redémarrage :"
    $RUN "cat /var/run/reboot-required 2>/dev/null || echo 'Pas de reboot requis.'"
  else
    echo "⏭  apt upgrade ignoré."
  fi

  section "V1.3 — Dépendances applicatives (frontend npm)"
  $RUN "cd /home/ubuntu/sentys/frontend && npm audit || true"
  if confirm "Appliquer 'npm audit fix' (SANS --force) ?"; then
    $RUN "cd /home/ubuntu/sentys/frontend && npm audit fix || true"
  fi

  section "V1.3 — Dépendances applicatives (backend NuGet)"
  # ⚠️ Connu : Scriban 7.1.0 = vuln HAUTE (GHSA-24c8-4792-22hx) détectée en local.
  $RUN "cd /home/ubuntu/sentys/backend && dotnet list package --vulnerable --include-transitive 2>/dev/null; \
        echo '--- OUTDATED ---'; dotnet list package --outdated 2>/dev/null"
  echo "→ Mettre à jour les paquets vulnérables compatibles .NET 8 (ex : Scriban) puis rebuild."

  section "V1.4 — Rebuild + restart + test"
  if confirm "Rebuild backend+frontend et redémarrer les services ?"; then
    $RUN "cd /home/ubuntu/sentys/backend && dotnet build -c Release 2>&1 | tail -3"
    $RUN "cd /home/ubuntu/sentys/frontend && npm run build 2>&1 | tail -5"
    $RUN "sudo systemctl restart sentys-backend.service sentys-frontend.service && sleep 4 && \
          systemctl is-active sentys-backend.service sentys-frontend.service"
    echo "--- Tests de fumée ---"
    $RUN "curl -sS -o /dev/null -w 'site https://sentys.fr -> %{http_code}\n' https://sentys.fr"
    $RUN "curl -sS -o /dev/null -w 'health -> %{http_code}\n' https://sentys.fr/api/health"
  fi

  echo
  echo "🔁 ROLLBACK (si KO) :"
  echo "   sudo systemctl stop sentys-backend.service sentys-frontend.service"
  echo "   rm -rf /home/ubuntu/sentys && cp -r /home/ubuntu/backups/sentys_before_maj_${TS} /home/ubuntu/sentys"
  echo "   PGPASSWORD=... pg_restore -h localhost -U sentys_app -d ctf_training --clean \\"
  echo "       /home/ubuntu/backups/ctf_training_before_maj_${TS}.dump"
  echo "   sudo systemctl start sentys-backend.service sentys-frontend.service"
}

# =============================================================================
# VOLET 2 — CHECK-UP CYBERSÉCURITÉ (lecture seule ; corriger CRITIQUE/ÉLEVÉ ensuite)
# =============================================================================
volet2() {
  section "V2.1 — Serveur / réseau"
  $RUN "sudo ufw status verbose"                 # attendu : 22/80/443 only
  $RUN "sudo fail2ban-client status"             # attendu : actif
  echo "--- Ports en écoute (PostgreSQL 5432 et Ollama 11434 NE DOIVENT PAS être en 0.0.0.0) ---"
  $RUN "sudo ss -tulpn | grep -E ':5432|:11434|:3000|:5202|LISTEN' "
  $RUN "grep -iE 'PermitRootLogin|PasswordAuthentication' /etc/ssh/sshd_config"  # attendu : no / no

  section "V2.2 — HTTP / TLS"
  $RUN "curl -sI https://sentys.fr | grep -iE 'strict-transport|x-frame|content-security|x-content-type|referrer-policy' || echo 'AUCUN header sécu trouvé !'"
  echo "--- Redirection HTTP -> HTTPS ---"
  $RUN "curl -sI http://sentys.fr | grep -iE 'HTTP/|location'"
  echo "--- Certificat / renouvellement ---"
  $RUN "sudo certbot certificates 2>/dev/null | grep -iE 'Domains|Expiry' || echo 'certbot ?'; \
        systemctl list-timers 2>/dev/null | grep -i certbot || echo 'timer certbot ?'"

  section "V2.3 — Sécurité applicative (audit code)"
  $RUN "grep -rn -i 'AllowAnyOrigin' /home/ubuntu/sentys/backend --include='*.cs' | head || echo 'CORS * : aucun (bon)'"
  $RUN "grep -rn -iE 'ValidateIssuer|ValidateAudience|ValidateLifetime|SigningKey' /home/ubuntu/sentys/backend/Program.cs | head"
  $RUN "grep -rn -i 'Authorize' /home/ubuntu/sentys/backend/Controllers --include='*.cs' | wc -l"

  section "V2.4 — Secrets en dur (doit ne rien remonter de sensible)"
  $RUN "grep -rn -iE 'password\s*=|apikey|secret|connectionstring' /home/ubuntu/sentys/backend \
        --include='*.cs' --include='*.json' | grep -ivE 'IConfiguration|getenv|Environment|appsettings.Development|user-secrets' | head -20 || echo 'RAS'"

  section "V2.5 — RAPPORT"
  cat <<'TXT'
Classer les constats par niveau et corriger CRITIQUE + ÉLEVÉ en priorité :
  [CRITIQUE]  PostgreSQL (5432) ou Ollama (11434) exposés en 0.0.0.0
  [CRITIQUE]  root login SSH ou PasswordAuthentication = yes
  [ÉLEVÉ]     Scriban 7.1.0 (GHSA-24c8-4792-22hx) → upgrade NuGet
  [ÉLEVÉ]     Headers HSTS/CSP/X-Frame-Options absents en prod
  [MOYEN]     npm/NuGet outdated non vulnérables
  [FAIBLE]    Divers durcissements
TXT
}

case "${1:-all}" in
  v1) volet1 ;;
  v2) volet2 ;;
  all) volet1; volet2 ;;
  *) echo "Usage: bash $0 [v1|v2|all]"; exit 1 ;;
esac
