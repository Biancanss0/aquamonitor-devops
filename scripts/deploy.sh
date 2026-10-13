#!/usr/bin/env bash
# =============================================================================
#  Deploy do AquaMonitor em um ambiente (staging | production) via Docker Compose
#
#  Uso:   ./scripts/deploy.sh staging
#         ./scripts/deploy.sh production
#
#  Variáveis opcionais:
#    ENV_FILE=caminho/.env       arquivo de ambiente (padrão: deploy/.env.<ambiente>)
#    BUILD_LOCAL=true            faz build da imagem local em vez de baixar do registry
#    HEALTH_TIMEOUT=300          segundos aguardando o /health responder
# =============================================================================
set -euo pipefail

AMBIENTE="${1:-}"
case "$AMBIENTE" in
  staging|production) ;;
  *) echo "Uso: $0 <staging|production>"; exit 1 ;;
esac

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ENV_FILE:-$ROOT/deploy/.env.$AMBIENTE}"
PROJETO="aquamonitor-$AMBIENTE"
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-300}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "❌ Arquivo $ENV_FILE não encontrado."
  echo "   Copie deploy/$AMBIENTE.env.example para deploy/.env.$AMBIENTE e preencha."
  exit 1
fi

# Carrega as variáveis (porta, imagem, versão) para usar neste script
set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a
API_PORT="${API_PORT:-8080}"

COMPOSE=(docker compose -p "$PROJETO" --env-file "$ENV_FILE" -f "$ROOT/docker-compose.yml")

echo "════════════════════════════════════════════════════════════"
echo " 🚀 Deploy AquaMonitor → $AMBIENTE"
echo "    Imagem : ${API_IMAGE:-aquamonitor-api:local}"
echo "    Versão : ${APP_VERSION:-local}"
echo "    Porta  : $API_PORT"
echo "════════════════════════════════════════════════════════════"

if [[ "${BUILD_LOCAL:-false}" == "true" ]]; then
  "${COMPOSE[@]}" up -d --build
else
  "${COMPOSE[@]}" pull                # baixa exatamente a imagem testada no CI
  "${COMPOSE[@]}" up -d --no-build
fi

echo "⏳ Aguardando a API ficar saudável (até ${HEALTH_TIMEOUT}s)..."
INICIO=$(date +%s)
until curl -fsS "http://localhost:$API_PORT/health" >/dev/null 2>&1; do
  if (( $(date +%s) - INICIO > HEALTH_TIMEOUT )); then
    echo "❌ Timeout: a API não ficou saudável. Últimos logs:"
    "${COMPOSE[@]}" ps
    "${COMPOSE[@]}" logs --tail=80
    echo "🔎 Diagnóstico: login do usuário da aplicação direto no Oracle (sqlplus):"
    # shellcheck disable=SC2016
    "${COMPOSE[@]}" exec -T oracle-db bash -c \
      'echo "select user from dual;" | sqlplus -s -L "$APP_USER/$APP_USER_PASSWORD@localhost:1521/FREEPDB1"' || true
    exit 1
  fi
  sleep 5
done

echo "✅ $AMBIENTE no ar em $(( $(date +%s) - INICIO ))s"
curl -fsS "http://localhost:$API_PORT/health"; echo
"${COMPOSE[@]}" ps
