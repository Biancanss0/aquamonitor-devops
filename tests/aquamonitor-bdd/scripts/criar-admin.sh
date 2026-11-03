#!/usr/bin/env bash
# =============================================================================
#  Cria (ou reaproveita) um usuário e o promove a perfil "Admin".
#
#  Por quê? A API não expõe endpoint para virar administrador — de propósito
#  (governança: ninguém se autopromove pela API). Só o perfil Admin pode excluir
#  alertas, então os cenários @admin precisam de um usuário promovido direto no
#  banco, como faria um DBA.
#
#  Uso (na raiz do repositório, com o docker compose no ar):
#     ADMIN_EMAIL=admin.bdd@aquamonitor.dev ADMIN_SENHA=AdminBdd2026 \
#       bash tests/aquamonitor-bdd/scripts/criar-admin.sh
# =============================================================================
set -euo pipefail

API_URL="${API_URL:-http://localhost:${API_PORT:-8080}}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin.bdd@aquamonitor.dev}"
ADMIN_SENHA="${ADMIN_SENHA:-AdminBdd2026}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"

if [[ ! "$ADMIN_EMAIL" =~ ^[A-Za-z0-9._@-]+$ ]]; then
  echo "❌ ADMIN_EMAIL contém caracteres não permitidos: $ADMIN_EMAIL"
  exit 1
fi

echo "👤 Cadastrando $ADMIN_EMAIL (se ainda não existir)..."
status=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$API_URL/api/Auth/registrar" \
  -H 'Content-Type: application/json' \
  -d "{\"Nome\":\"Administrador BDD\",\"Email\":\"$ADMIN_EMAIL\",\"Senha\":\"$ADMIN_SENHA\"}")
case "$status" in
  201) echo "   usuário criado." ;;
  400) echo "   usuário já existia (ok)." ;;
  *)   echo "❌ Cadastro retornou HTTP $status"; exit 1 ;;
esac

echo "🔑 Promovendo para perfil Admin no Oracle..."
# shellcheck disable=SC2016  # $APP_USER e $APP_USER_PASSWORD são expandidos DENTRO do container
docker compose --project-directory "$ROOT" exec -T oracle-db bash -c \
  'sqlplus -s -L "$APP_USER/$APP_USER_PASSWORD@localhost:1521/FREEPDB1"' <<SQL
WHENEVER SQLERROR EXIT SQL.SQLCODE
UPDATE AM_USUARIOS SET PERFIL = 'Admin' WHERE EMAIL = '$ADMIN_EMAIL';
COMMIT;
EXIT
SQL

perfil=$(curl -s -X POST "$API_URL/api/Auth/login" -H 'Content-Type: application/json' \
  -d "{\"Email\":\"$ADMIN_EMAIL\",\"Senha\":\"$ADMIN_SENHA\"}" | grep -o '"Perfil":"[^"]*"' | cut -d'"' -f4)

if [[ "$perfil" == "Admin" ]]; then
  echo "✅ $ADMIN_EMAIL agora faz login com perfil Admin."
else
  echo "❌ Login do administrador não retornou perfil Admin (veio: '${perfil:-nada}')."
  exit 1
fi
