# 🥒 AquaMonitor — Testes Automatizados de API com BDD

Suíte de testes **BDD (Behavior Driven Development)** da AquaMonitor API, escrita em **Gherkin em português** e executada com **Java 17 + Cucumber 7 + RestAssured 5 + JSON Schema**.

| Integrantes | RM |
|-------------|----|
| Bianca Nogueira Sousa | RM561288 |
| Leonardo José Nogueira dos Santos Sousa | RM561637 |

```
 .feature (Gherkin pt-BR)  ──►  Step definitions (Java)  ──►  RestAssured (HTTP)  ──►  AquaMonitor API + Oracle
        "o que"                     "como"                      status · corpo · contrato
```

---

## ✅ O que a atividade pede × o que foi entregue

| Requisito | Entrega |
|-----------|---------|
| **1.** Mínimo de 3 cenários BDD em Gherkin, positivos e negativos | **58 cenários** em 6 funcionalidades (`src/test/resources/features`) — 26 `@positivo` e 32 `@negativo` |
| **2.** Testes de **todas** as APIs: status code, corpo JSON e contrato (JSON Schema) | **19/19 endpoints** cobertos · 12 JSON Schemas em `src/test/resources/schemas` · 45 cenários com validação de contrato |
| **3.** Execução simples e documentada, local ou em CI | 1 comando local (`scripts/run-bdd.sh`), Maven puro, container Docker **e** pipeline no **GitHub Actions** (`.github/workflows/bdd-api-tests.yml`) |
| Artefatos | Este projeto + `Dockerfile` + scripts + README · relatórios HTML/JSON/JUnit gerados a cada execução |

---

## 🧰 Tecnologias e por que cada uma

| Ferramenta | Papel | Por que |
|------------|-------|---------|
| **Gherkin (pt-BR)** | Linguagem dos cenários | Professor, PO e dev leem o mesmo texto; o cenário vira documentação viva |
| **Cucumber 7** | Liga cada frase Gherkin a um método Java | Padrão de mercado para BDD na JVM; gera relatórios HTML/JSON/JUnit |
| **RestAssured 5** | Cliente HTTP de teste | Sintaxe fluente `given/when/then` para status, corpo, cabeçalhos e tempo de resposta |
| **json-schema-validator** | Teste de contrato | Garante que o "formato" do JSON não muda sem ninguém perceber |
| **PicoContainer** | Injeção de dependência entre steps | Compartilha o estado do cenário sem variáveis estáticas (cenários isolados) |
| **JUnit Platform + Surefire** | Executor | `mvn test` roda tudo; qualquer CI entende o resultado |

> **Por que Java, se a API é C#?** O enunciado permite testes em Java com Cucumber. Testar de fora, **pela rede, como um cliente real**, é justamente o objetivo de um teste de API: o teste não sabe (nem precisa saber) em que linguagem a API foi feita. Funciona como um "cliente oculto" que entra na loja e avalia o atendimento sem conhecer a cozinha.

---

## 📁 Estrutura

```
tests/aquamonitor-bdd/
├── pom.xml                         dependências (Cucumber, RestAssured, JSON Schema)
├── Dockerfile                      roda a suíte sem instalar Java/Maven
├── README.md
├── scripts/
│   ├── run-bdd.sh                  ⭐ sobe o ambiente, prepara admin e roda tudo (1 comando)
│   ├── criar-admin.sh              cria/promove o usuário Admin (cenários @admin)
│   └── resumo-cucumber.py          resumo + matriz de cobertura das APIs (usado no CI)
└── src/test/
    ├── java/br/com/fiap/aquamonitor/bdd/
    │   ├── RunCucumberTest.java    ponto de entrada (JUnit Platform Suite)
    │   ├── steps/                  1 classe por recurso da API + ComumSteps (status/corpo/contrato)
    │   └── support/                Config, ApiClient, Preparacao, TestContext, Hooks
    └── resources/
        ├── features/               01_autenticacao … 06_saude (.feature)
        ├── schemas/                12 contratos *.schema.json (draft-04)
        └── junit-platform.properties
```

---

## 🌱 Como os testes se ligam ao ESG

| Pilar | Tag | O que o teste garante | Exemplo de cenário |
|-------|-----|----------------------|--------------------|
| **E** – Água | `@esg-agua` | Vazamento (pressão < 0,5 bar) gera alerta **CRITICO**; consumo > limite gera **MEDIO**; > 150% gera **ALTO**; leitura normal **não** gera alerta falso | *Detecção automática de anomalias gera alerta para a equipe* |
| **E** – Eficiência | `@esg-eficiencia` | Listagens paginadas; pedido de 500 itens é limitado a 10 (menos CPU, banco, rede e energia); tempo de resposta < 3 s | *Pedido de página gigante é limitado pela API para poupar recursos* |
| **S** – Social / inclusão | `@esg-social` | Erros de validação claros, campo a campo, para qualquer perfil de usuário | *Cadastro com dado inválido é recusado com mensagem clara* |
| **G** – Governança | `@esg-governanca` | Sem token → 401; token adulterado → 401; **Operador não exclui alerta (403)**; resolver alerta grava data/hora (rastreabilidade); sensor desativado não aceita dados | *Operador não tem permissão para excluir alertas (segregação de funções)* |

---

## ▶️ Como executar

### Pré-requisitos
- **Docker Desktop** (Windows/macOS) ou Docker Engine + Compose v2 (Linux) — sobe a API e o Oracle.
- **Opcional:** Java 17 + Maven 3.9. Se não tiver, o script roda os testes dentro de um container.
- No Windows, use o **Git Bash** ou o **WSL** para os scripts `.sh`.

### Opção 1 — Um comando (recomendado)

Na **raiz do repositório**:

```bash
cp .env.example .env                          # só na 1ª vez (PowerShell: Copy-Item .env.example .env)
bash tests/aquamonitor-bdd/scripts/run-bdd.sh
```

O script: sobe API + Oracle → espera o `/health` → cria o admin → roda os 58 cenários → salva o relatório.
Na primeira vez o Oracle (~1 GB) demora alguns minutos para baixar e iniciar.

Variações úteis:

```bash
bash tests/aquamonitor-bdd/scripts/run-bdd.sh --tags "@sensores"          # só uma funcionalidade
bash tests/aquamonitor-bdd/scripts/run-bdd.sh --tags "@negativo"          # só caminhos de falha
bash tests/aquamonitor-bdd/scripts/run-bdd.sh --tags "@esg-governanca"    # só um pilar ESG
bash tests/aquamonitor-bdd/scripts/run-bdd.sh --docker                    # força rodar no container
bash tests/aquamonitor-bdd/scripts/run-bdd.sh --down                      # derruba tudo no final
```

### Opção 2 — Maven, com a API já no ar

```bash
docker compose up -d --build                                # na raiz do repositório
bash tests/aquamonitor-bdd/scripts/criar-admin.sh              # opcional: habilita os cenários @admin
cd tests/aquamonitor-bdd
mvn test -Dapi.baseUrl=http://localhost:8080 \
         -Dadmin.email=admin.bdd@aquamonitor.dev -Dadmin.senha=AdminBdd2026
```

Sem as propriedades `admin.*`, os 2 cenários `@admin` aparecem como **pulados** (e não como falha). A suíte também roda contra **staging** ou **produção** só trocando a URL: `-Dapi.baseUrl=http://localhost:8081`.

### Opção 3 — Container, sem Java nem Maven

```bash
docker build -t aquamonitor-bdd-tests tests/aquamonitor-bdd
docker run --rm --network aquamonitor_frontend \
  -e API_BASE_URL=http://api:8080 \
  -v "$PWD/tests/aquamonitor-bdd/target:/tests/target" \
  aquamonitor-bdd-tests
```

### Opção 4 — Pipeline (GitHub Actions)

O workflow **"Testes BDD de API"** roda sozinho a cada push/PR que mexa na API ou nos testes. Para rodar manualmente: **Actions → Testes BDD de API → Run workflow** (campo opcional de tags).

O pipeline gera senhas descartáveis, sobe API + Oracle, compila os testes enquanto o banco inicia, cria o admin, executa os cenários, valida também a imagem Docker dos testes, publica o **resumo com a matriz de cobertura** na própria página da execução e anexa os relatórios como artifact `relatorio-bdd-cucumber`.

### Configuração

| Propriedade `-D` | Variável de ambiente | Padrão | Uso |
|------------------|----------------------|--------|-----|
| `api.baseUrl` | `API_BASE_URL` | `http://localhost:8080` | URL da API testada |
| `admin.email` | `ADMIN_EMAIL` | — | Login do Admin (cenários `@admin`) |
| `admin.senha` | `ADMIN_SENHA` | — | Senha do Admin |
| `cucumber.filter.tags` | — | todas | Filtra cenários por tag |

---

## 📊 Relatórios gerados

| Arquivo | Para quê |
|---------|----------|
| `target/cucumber-reports/cucumber.html` | Relatório visual: cada cenário, cada passo e, anexadas, **todas as chamadas HTTP** (método, URL, status, tempo, corpo enviado e recebido) |
| `target/cucumber-reports/cucumber.json` | Base do resumo do CI |
| `target/cucumber-reports/cucumber.xml` | Formato JUnit (Jenkins, Azure DevOps, GitLab…) |
| `target/cucumber-reports/resumo.md` | Placar + matriz "endpoint × status HTTP testados" (gerado no CI) |
| `target/logs-api.txt` | Logs da API durante a execução |

---

## 🔌 Cobertura das APIs

| Método | Endpoint | Status validados | Contrato |
|--------|----------|------------------|----------|
| POST | `/api/Auth/registrar` | 201 · 400 (e-mail duplicado) · 400 (validação) | `auth-response`, `erro-mensagem`, `erro-validacao` |
| POST | `/api/Auth/login` | 200 · 401 | `auth-response`, `erro-mensagem` |
| GET | `/api/Sensores` | 200 (paginação, filtro, limite de página) | `sensores-paginado` |
| GET | `/api/Sensores/{id}` | 200 · 404 | `sensor`, `erro-mensagem` |
| POST | `/api/Sensores` | 201 (+ cabeçalho `Location`) · 400 | `sensor`, `erro-validacao` |
| PUT | `/api/Sensores/{id}` | 200 · 404 | `sensor`, `erro-mensagem` |
| DELETE | `/api/Sensores/{id}` | 204 · 404 | — |
| GET | `/api/Leituras` | 200 (filtro por sensor e vazamento) | `leituras-paginado` |
| GET | `/api/Leituras/{id}` | 200 · 404 | `leitura`, `erro-mensagem` |
| POST | `/api/Leituras` | 201 · 400 (validação) · 400 (sensor inativo) · 404 | `leitura`, `erro-validacao`, `erro-mensagem` |
| DELETE | `/api/Leituras/{id}` | 204 · 404 | — |
| GET | `/api/Alertas` 🔒 | 200 · 401 (sem token) · 401 (token adulterado) | `alertas-paginado` |
| GET | `/api/Alertas/{id}` 🔒 | 200 · 404 | `alerta`, `erro-mensagem` |
| PATCH | `/api/Alertas/{id}/resolver` 🔒 | 200 · 400 (já resolvido) · 404 | `alerta`, `erro-mensagem` |
| DELETE | `/api/Alertas/{id}` 🔒 Admin | 204 (Admin) · 403 (Operador) · 404 | `erro-mensagem` |
| GET | `/api/Relatorios/geral` 🔒 | 200 · 400 (período inválido) · 401 | `relatorio-geral`, `erro-mensagem` |
| GET | `/api/Relatorios/sensor/{id}` 🔒 | 200 · 404 | `relatorio-sensor`, `erro-mensagem` |
| GET | `/health` | 200 (API + banco *Healthy*, < 3 s) | `health` |
| GET | `/swagger/v1/swagger.json` | 200 (todos os recursos documentados) | — |

**Respostas sem corpo** (204, 401, 403) são validadas só pelo status — não há JSON para comparar com um contrato.

---

## 🧠 Decisões de projeto (e por quê)

1. **Dados criados pela própria API, não por SQL.** Cada `Dado` cadastra o que precisa via HTTP — a pré-condição também testa o sistema e a suíte roda em qualquer ambiente. Única exceção: promover o Admin, porque a API (corretamente) não permite que alguém se autopromova.
2. **Limpeza automática.** O hook `@After` exclui os sensores criados no cenário (a exclusão em cascata remove leituras e alertas). O banco não acumula lixo e um cenário não "suja" o outro — como lavar a louça logo depois de usar.
3. **Dados únicos por execução.** E-mails e nomes têm sufixo com timestamp: a suíte pode rodar quantas vezes quiser no mesmo banco.
4. **Contratos estritos.** `required` + `additionalProperties: false` + `enum`: se um campo sumir, mudar de tipo ou surgir sem aviso, o teste quebra. É o "contrato de aluguel" entre API e quem a consome.
5. **Steps em linguagem de negócio.** "eu registro uma leitura de 900 litros com pressão de 0.3 bar" em vez de "envio POST com JSON X". O cenário continua válido mesmo se a rota mudar — só o step muda.
6. **Evidência embutida.** Cada cenário anexa ao relatório todas as chamadas HTTP feitas. Se algo falhar, o RestAssured imprime requisição e resposta completas no console.
7. **Segredos fora do código.** URL, e-mail e senha do admin chegam por variável de ambiente; no CI são gerados aleatoriamente a cada execução.

---

## 🩺 Problemas comuns

| Sintoma | Causa / solução |
|---------|-----------------|
| `Connection refused` em todos os cenários | API fora do ar: `docker compose ps` e `curl http://localhost:8080/health` |
| Timeout esperando o `/health` | 1ª execução baixando o Oracle (~1 GB). Aguarde ou aumente `HEALTH_TIMEOUT=900` |
| 2 cenários "skipped" | Faltam `ADMIN_EMAIL`/`ADMIN_SENHA` — rode `scripts/criar-admin.sh` ou use o `run-bdd.sh` |
| `\r: command not found` no Windows | Clone com Git configurado para LF (o `.gitattributes` já força LF nos `.sh`) |
| Porta 8080 ocupada | Mude `API_PORT` no `.env` — o `run-bdd.sh` lê o valor de lá |
