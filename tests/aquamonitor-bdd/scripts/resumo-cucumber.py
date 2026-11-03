#!/usr/bin/env python3
"""
Gera um resumo em Markdown a partir do relatório JSON do Cucumber.

Uso:  python3 resumo-cucumber.py target/cucumber-reports/cucumber.json > resumo.md

O resumo traz:
  1. placar por funcionalidade (passou / falhou / pulado);
  2. a lista de cenários com o status de cada um;
  3. a MATRIZ DE COBERTURA DA API: para cada endpoint, quais status HTTP foram
     exercitados pelos testes — calculada a partir das chamadas reais anexadas
     a cada cenário (evidência de que todas as APIs foram testadas).
"""
import base64
import collections
import json
import re
import sys

ENDPOINTS = [
    ("POST", "/api/Auth/registrar"), ("POST", "/api/Auth/login"),
    ("GET", "/api/Sensores"), ("GET", "/api/Sensores/{id}"), ("POST", "/api/Sensores"),
    ("PUT", "/api/Sensores/{id}"), ("DELETE", "/api/Sensores/{id}"),
    ("GET", "/api/Leituras"), ("GET", "/api/Leituras/{id}"), ("POST", "/api/Leituras"),
    ("DELETE", "/api/Leituras/{id}"),
    ("GET", "/api/Alertas"), ("GET", "/api/Alertas/{id}"), ("PATCH", "/api/Alertas/{id}/resolver"),
    ("DELETE", "/api/Alertas/{id}"),
    ("GET", "/api/Relatorios/geral"), ("GET", "/api/Relatorios/sensor/{id}"),
    ("GET", "/health"), ("GET", "/swagger/v1/swagger.json"),
]
CHAMADA = re.compile(r"^(GET|POST|PUT|PATCH|DELETE) (/[^\s?]*)\S* → (\d{3})", re.M)
ICONE = {"passed": "✅", "failed": "❌", "skipped": "⏭️"}


def status_dos_passos(elemento):
    estados = []
    for grupo in ("before", "steps", "after"):
        for item in elemento.get(grupo, []):
            estados.append(item.get("result", {}).get("status", "skipped"))
    return estados


def consolidar(estados):
    if any(s in ("failed", "ambiguous") for s in estados):
        return "failed"
    if any(s in ("skipped", "pending", "undefined") for s in estados):
        return "skipped"
    return "passed"


def anexos(elemento):
    for grupo in ("before", "steps", "after"):
        for item in elemento.get(grupo, []):
            for emb in item.get("embeddings", []):
                yield emb


def normalizar(caminho):
    return re.sub(r"/\d+(?=/|$)", "/{id}", caminho)


def main(caminho_json):
    features = json.load(open(caminho_json, encoding="utf-8"))
    placar = collections.OrderedDict()
    cenarios = []
    cobertura = collections.defaultdict(set)
    contratos = 0

    for feature in features:
        nome_feature = feature.get("name", "?")
        placar.setdefault(nome_feature, collections.Counter())
        estados_background = []
        for elemento in feature.get("elements", []):
            if elemento.get("type") == "background":
                estados_background = status_dos_passos(elemento)
                continue
            estado = consolidar(estados_background + status_dos_passos(elemento))
            estados_background = []
            placar[nome_feature][estado] += 1
            tags = " ".join(t["name"] for t in elemento.get("tags", []) if t["name"].startswith(("@positivo", "@negativo")))
            cenarios.append((nome_feature, elemento.get("name", "?"), tags, estado))
            contratos += sum(1 for s in elemento.get("steps", [])
                             if "respeitar o contrato" in s.get("name", "")
                             and s.get("result", {}).get("status") == "passed")
            for emb in anexos(elemento):
                if emb.get("mime_type") != "text/plain":
                    continue
                texto = base64.b64decode(emb.get("data", "")).decode("utf-8", "replace")
                for metodo, caminho, status in CHAMADA.findall(texto):
                    cobertura[(metodo, normalizar(caminho))].add(status)

    total = collections.Counter()
    for c in placar.values():
        total.update(c)

    out = ["## 🥒 Testes BDD de API — AquaMonitor", ""]
    out.append(f"**{total['passed']} passaram · {total['failed']} falharam · {total['skipped']} pulados** "
               f"— {sum(total.values())} cenários · {contratos} validações de contrato (JSON Schema) aprovadas")
    out += ["", "| Funcionalidade | Cenários | ✅ | ❌ | ⏭️ |", "|---|---:|---:|---:|---:|"]
    for nome, c in placar.items():
        out.append(f"| {nome} | {sum(c.values())} | {c['passed']} | {c['failed']} | {c['skipped']} |")

    out += ["", "### 🔌 Cobertura das APIs (status HTTP exercitados)", "",
            "| Método | Endpoint | Status testados | Coberto |", "|---|---|---|:-:|"]
    cobertos = 0
    for metodo, caminho in ENDPOINTS:
        status = sorted(cobertura.get((metodo, caminho), []))
        cobertos += bool(status)
        out.append(f"| `{metodo}` | `{caminho}` | {', '.join(status) or '—'} | {'✅' if status else '❌'} |")
    out.append("")
    out.append(f"**{cobertos}/{len(ENDPOINTS)} endpoints cobertos.**")

    out += ["", "<details><summary>📋 Todos os cenários</summary>", "",
            "| | Funcionalidade | Cenário | Tipo |", "|---|---|---|---|"]
    for feature, nome, tags, estado in cenarios:
        out.append(f"| {ICONE[estado]} | {feature} | {nome} | {tags} |")
    out += ["", "</details>", ""]
    print("\n".join(out))
    return 1 if total["failed"] else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "target/cucumber-reports/cucumber.json"))
