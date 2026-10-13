#!/usr/bin/env bash
# =============================================================================
#  Smoke tests pós-deploy — percorre o fluxo principal da API de ponta a ponta
#
#  Uso:   ./scripts/smoke-test.sh <url-base> <pasta-de-evidencias> [ambiente]
#  Ex.:   ./scripts/smoke-test.sh http://localhost:8081 evidencias/staging staging
#
#  Cada resposta é salva em <pasta>/NN-nome.json e um resumo em <pasta>/resumo.md.
#  O script falha (exit 1) se qualquer status HTTP for diferente do esperado.
# =============================================================================
set -uo pipefail

BASE_URL="${1:?Informe a URL base, ex.: http://localhost:8080}"
OUT="${2:-evidencias/local-$(date +%Y%m%d-%H%M%S)}"
AMBIENTE="${3:-local}"
mkdir -p "$OUT"

FALHAS=0
PASSO=0
TOKEN=""
RESUMO="$OUT/resumo.md"

{
  echo "## 🧪 Smoke tests — ambiente \`$AMBIENTE\`"
  echo
  echo "URL: \`$BASE_URL\` — executado em $(date -u '+%Y-%m-%d %H:%M:%S UTC')"
  echo
  echo "| # | Requisição | Esperado | Obtido | Resultado |"
  echo "|---|------------|----------|--------|-----------|"
} > "$RESUMO"

# chamar <nome> <esperado> <método> <rota> [json] [auth:true|false]
chamar() {
  local nome="$1" esperado="$2" metodo="$3" rota="$4" corpo="${5:-}" auth="${6:-false}"
  PASSO=$((PASSO + 1))
  local arquivo; arquivo="$OUT/$(printf '%02d' "$PASSO")-$nome.json"
  local args=(-s -o "$arquivo" -w '%{http_code}' -X "$metodo" "$BASE_URL$rota" -H 'Accept: application/json')
  [[ -n "$corpo" ]] && args+=(-H 'Content-Type: application/json' -d "$corpo")
  [[ "$auth" == "true" ]] && args+=(-H "Authorization: Bearer $TOKEN")

  local status; status=$(curl "${args[@]}")
  local resultado="✅ OK"
  if [[ "$status" != "$esperado" ]]; then resultado="❌ FALHOU"; FALHAS=$((FALHAS + 1)); fi

  printf '%-4s %-7s %-45s esperado=%s obtido=%s  %s\n' "$PASSO" "$metodo" "$rota" "$esperado" "$status" "$resultado"
  echo "| $PASSO | \`$metodo $rota\` | $esperado | $status | $resultado |" >> "$RESUMO"
  ULTIMA_RESPOSTA="$arquivo"
}

EMAIL="smoke.$AMBIENTE.$(date +%s)@aquamonitor.dev"
SENHA="Smoke123456"

chamar health            200 GET  "/health"
chamar swagger           200 GET  "/swagger/v1/swagger.json"
chamar auth-registrar    201 POST "/api/Auth/registrar" "{\"Nome\":\"Smoke Test $AMBIENTE\",\"Email\":\"$EMAIL\",\"Senha\":\"$SENHA\"}"
chamar auth-login        200 POST "/api/Auth/login"     "{\"Email\":\"$EMAIL\",\"Senha\":\"$SENHA\"}"
TOKEN=$(grep -o '"Token":"[^"]*"' "$ULTIMA_RESPOSTA" | head -1 | cut -d'"' -f4)

chamar sensor-criar      201 POST "/api/Sensores" "{\"Nome\":\"Sensor Smoke $AMBIENTE\",\"Localizacao\":\"Bloco A - Pipeline\",\"Tipo\":\"Comercial\",\"LimiteConsumo\":500}"
SENSOR_ID=$(grep -o '"Id":[0-9]*' "$ULTIMA_RESPOSTA" | head -1 | cut -d: -f2)
SENSOR_ID="${SENSOR_ID:-1}"

chamar sensores-listar   200 GET  "/api/Sensores?pagina=1&tamanhoPagina=5"
# Consumo 900 L > limite 500 L e pressão 0.3 bar → gera alertas de ConsumoExcessivo e Vazamento
chamar leitura-registrar 201 POST "/api/Leituras" "{\"SensorId\":$SENSOR_ID,\"ConsumoLitros\":900,\"PressaoBar\":0.3,\"TemperaturaCelsius\":22}"
chamar leituras-listar   200 GET  "/api/Leituras?sensorId=$SENSOR_ID&pagina=1&tamanhoPagina=5"
chamar alertas-sem-token 401 GET  "/api/Alertas"
chamar alertas-com-token 200 GET  "/api/Alertas?sensorId=$SENSOR_ID" "" true
chamar relatorio-sensor  200 GET  "/api/Relatorios/sensor/$SENSOR_ID" "" true
chamar relatorio-geral   200 GET  "/api/Relatorios/geral" "" true

{
  echo
  if (( FALHAS == 0 )); then
    echo "**Resultado: ✅ $PASSO/$PASSO verificações passaram.**"
  else
    echo "**Resultado: ❌ $FALHAS de $PASSO verificações falharam.**"
  fi
  echo
  echo "<details><summary>Resposta de /health</summary>"
  echo
  echo '```json'
  cat "$OUT/01-health.json"; echo
  echo '```'
  echo "</details>"
} >> "$RESUMO"

echo
cat "$RESUMO"
exit $(( FALHAS > 0 ? 1 : 0 ))
