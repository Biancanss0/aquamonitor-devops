#!/usr/bin/env bash
# =============================================================================
#  Executa a suíte BDD de ponta a ponta com UM comando:
#    1. sobe API + Oracle com docker compose (se ainda não estiverem no ar)
#    2. espera o /health responder 200
#    3. prepara o usuário administrador (cenários @admin)
#    4. roda os testes — com Maven local, ou dentro de um container se não houver Maven
#    5. salva os logs da API junto com os relatórios
#
#  Uso (na raiz do repositório):
#     bash tests/aquamonitor-bdd/scripts/run-bdd.sh                      # tudo
#     bash tests/aquamonitor-bdd/scripts/run-bdd.sh --tags "@sensores"   # só um grupo
#     bash tests/aquamonitor-bdd/scripts/run-bdd.sh --docker             # força rodar no container
#     bash tests/aquamonitor-bdd/scripts/run-bdd.sh --down               # derruba o ambiente no final
#
#  Relatório: tests/aquamonitor-bdd/target/cucumber-reports/cucumber.html
# =============================================================================
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
BDD_DIR="$ROOT/tests/aquamonitor-bdd"
TAGS=""
USAR_DOCKER=false
DERRUBAR=false
SUBIR=true

while [[ $# -gt 0 ]]; do
  case "$1" in
    --tags)    TAGS="${2:-}"; shift 2 ;;
    --docker)  USAR_DOCKER=true; shift ;;
    --down)    DERRUBAR=true; shift ;;
    --no-up)   SUBIR=false; shift ;;
    -h|--help) sed -n '2,19p' "$0"; exit 0 ;;
    *) echo "Opção desconhecida: $1"; exit 2 ;;
  esac
done

cd "$ROOT" || exit 1

# Lê a porta configurada no .env (padrão 8080)
if [[ -f .env ]]; then
  API_PORT="${API_PORT:-$(grep -E '^API_PORT=' .env | cut -d= -f2 | tr -d '[:space:]')}"
fi
API_PORT="${API_PORT:-8080}"
API_URL="${API_URL:-http://localhost:$API_PORT}"
export API_URL API_PORT
export ADMIN_EMAIL="${ADMIN_EMAIL:-admin.bdd@aquamonitor.dev}"
export ADMIN_SENHA="${ADMIN_SENHA:-AdminBdd2026}"
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-420}"

echo "════════════════════════════════════════════════════════════"
echo " 🥒 AquaMonitor — testes BDD de API"
echo "    API   : $API_URL"
echo "    Tags  : ${TAGS:-(todas)}"
echo "════════════════════════════════════════════════════════════"

# ─── 1. Ambiente ─────────────────────────────────────────────────────────────
if [[ "$SUBIR" == true ]]; then
  if [[ ! -f .env ]]; then
    echo "⚠️  .env não encontrado — copiando .env.example (troque as senhas depois!)"
    cp .env.example .env
  fi
  echo "🐳 Subindo API + Oracle (docker compose up -d --build)..."
  docker compose up -d --build || exit 1
fi

# ─── 2. Espera a API ficar saudável ──────────────────────────────────────────
echo "⏳ Aguardando $API_URL/health (até ${HEALTH_TIMEOUT}s; o Oracle demora na 1ª vez)..."
INICIO=$(date +%s)
until curl -fsS "$API_URL/health" >/dev/null 2>&1; do
  if (( $(date +%s) - INICIO > HEALTH_TIMEOUT )); then
    echo "❌ A API não ficou saudável a tempo. Últimos logs:"
    docker compose logs --tail=80 api
    exit 1
  fi
  sleep 5
done
echo "✅ API saudável: $(curl -s "$API_URL/health")"

# ─── 3. Administrador para os cenários @admin ────────────────────────────────
if ! bash "$BDD_DIR/scripts/criar-admin.sh"; then
  echo "⚠️  Não foi possível preparar o admin — os cenários @admin serão pulados."
  unset ADMIN_EMAIL ADMIN_SENHA
fi

# ─── 4. Testes ───────────────────────────────────────────────────────────────
mkdir -p "$BDD_DIR/target"
ARGS=()
[[ -n "$TAGS" ]] && ARGS+=("-Dcucumber.filter.tags=$TAGS")

if [[ "$USAR_DOCKER" == false ]] && command -v mvn >/dev/null 2>&1; then
  echo "☕ Rodando com o Maven local..."
  mvn -B -f "$BDD_DIR/pom.xml" test -Dapi.baseUrl="$API_URL" ${ARGS[@]+"${ARGS[@]}"}
  RESULTADO=$?
else
  echo "🐳 Maven não encontrado (ou --docker): rodando os testes dentro de um container..."
  docker build -t aquamonitor-bdd-tests "$BDD_DIR" || exit 1
  # shellcheck disable=SC2016  # template Go do docker inspect, não é variável do shell
  REDE="$(docker compose ps -q api | xargs docker inspect -f '{{range $k, $v := .NetworkSettings.Networks}}{{println $k}}{{end}}' | grep frontend | head -1)"
  docker run --rm --network "${REDE:-aquamonitor_frontend}" \
    -e API_BASE_URL=http://api:8080 \
    -e ADMIN_EMAIL="${ADMIN_EMAIL:-}" -e ADMIN_SENHA="${ADMIN_SENHA:-}" \
    -v "$BDD_DIR/target:/tests/target" \
    aquamonitor-bdd-tests ${ARGS[@]+"${ARGS[@]}"}
  RESULTADO=$?
fi

# ─── 5. Evidências ───────────────────────────────────────────────────────────
docker compose logs --no-color api > "$BDD_DIR/target/logs-api.txt" 2>&1 || true
echo
echo "📄 Relatório HTML : $BDD_DIR/target/cucumber-reports/cucumber.html"
echo "📄 Logs da API    : $BDD_DIR/target/logs-api.txt"

if [[ "$DERRUBAR" == true ]]; then
  echo "🧹 Derrubando o ambiente (docker compose down -v)..."
  docker compose down -v
fi

if (( RESULTADO == 0 )); then
  echo "✅ Todos os cenários passaram."
else
  echo "❌ Há cenários com falha (código $RESULTADO). Veja o relatório HTML."
fi
exit "$RESULTADO"
