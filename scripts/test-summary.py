"""Gera um resumo em Markdown dos resultados .trx do dotnet test (para o GITHUB_STEP_SUMMARY).

Uso: python3 scripts/test-summary.py <pasta-com-trx>
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
pasta = sys.argv[1] if len(sys.argv) > 1 else "TestResults"
arquivos = glob.glob(os.path.join(pasta, "**", "*.trx"), recursive=True)

print("## 🧪 Testes automatizados (xUnit)\n")
if not arquivos:
    print("> ⚠️ Nenhum arquivo .trx encontrado — os testes não chegaram a executar.")
    sys.exit(0)

total = passou = falhou = 0
linhas = []
for arquivo in arquivos:
    raiz = ET.parse(arquivo).getroot()
    contadores = raiz.find("t:ResultSummary/t:Counters", NS)
    if contadores is not None:
        total += int(contadores.get("total", 0))
        passou += int(contadores.get("passed", 0))
        falhou += int(contadores.get("failed", 0))
    for r in raiz.iterfind("t:Results/t:UnitTestResult", NS):
        nome = r.get("testName", "").replace("AquaMonitor.Tests.", "")
        resultado = r.get("outcome", "")
        icone = "✅" if resultado == "Passed" else ("❌" if resultado == "Failed" else "➖")
        duracao = (r.get("duration") or "")[:11]
        linhas.append(f"| {icone} | `{nome}` | {duracao} |")

status = "✅ Todos os testes passaram" if falhou == 0 else f"❌ {falhou} teste(s) falharam"
print(f"**{status}** — {passou}/{total} aprovados.\n")
print("| | Teste | Duração |")
print("|---|-------|---------|")
print("\n".join(sorted(linhas)))
