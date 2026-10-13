# Projeto - Cidades ESGInteligentes

## 💧 AquaMonitor API — Tema 7: Acesso à água e preservação de recursos naturais

API REST para **monitoramento inteligente do consumo de água**: cadastro de sensores, registro de leituras, **detecção automática de vazamentos e consumo excessivo** e relatórios de impacto hídrico.

Nesta entrega o projeto ganhou um ciclo de vida DevOps completo:

```
 git push ──► 🔨 Build ──► 🧪 Testes ──► 🐳 Imagem Docker ──► 🚀 Staging ──► 🏭 Produção
                                         (GHCR, tag sha-xxxxxxx)   automático    aprovação manual
```

| Integrantes | RM |
|-------------|----|
| _Nome completo_ | _RM00000_ |
| _Nome completo_ | _RM00000_ |

> 🔗 Repositório: `https://github.com/Biancanss0/aquamonitor-devops`

---

## 🐳 Como executar localmente com Docker

**Pré-requisito:** Docker Desktop (Windows/macOS) ou Docker Engine + Compose v2 (Linux). Não é preciso ter .NET nem Oracle instalados.

**1. Clonar o repositório**

```bash
git clone https://github.com/Biancanss0/aquamonitor-devops.git
cd aquamonitor-devops
```

**2. Criar o arquivo de variáveis de ambiente**

```bash
cp .env.example .env          # PowerShell: Copy-Item .env.example .env
```

Abra o `.env` e troque as senhas e a `JWT_KEY` (mínimo 32 caracteres; use só letras e números).

**3. Subir a aplicação + banco**

```bash
docker compose up -d --build
```

O que acontece:
1. O Compose sobe o **Oracle 23ai Free** (`oracle-db`) e espera ele ficar *healthy*.
2. Faz o build da imagem da API (Dockerfile multi-stage) e sobe o container `api`.
3. A API aplica as **migrations do EF Core automaticamente** (com novas tentativas até o banco aceitar conexões).

> Na primeira vez o download da imagem do Oracle (~1 GB) pode levar alguns minutos.

**4. Acessar**

| O quê | URL |
|-------|-----|
| Swagger (documentação interativa) | http://localhost:8080 |
| Health check (API + banco) | http://localhost:8080/health |

**5. Comandos úteis**

```bash
docker compose ps                 # status dos containers (deve mostrar "healthy")
docker compose logs -f api        # logs da API em tempo real
docker compose down               # para tudo (mantém os dados do banco)
docker compose down -v            # para tudo e APAGA o volume do banco
```

**6. Simular staging e produção na sua máquina** (Git Bash, WSL, Linux ou macOS)

```bash
cp deploy/staging.env.example    deploy/.env.staging       # ajuste os valores
cp deploy/production.env.example deploy/.env.production
BUILD_LOCAL=true ./scripts/deploy.sh staging       # → http://localhost:8081
BUILD_LOCAL=true ./scripts/deploy.sh production    # → http://localhost:8080
./scripts/smoke-test.sh http://localhost:8081 evidencias/local-staging staging
```

Cada ambiente roda com um *project name* próprio (`aquamonitor-staging` / `aquamonitor-production`), então **containers, rede e volume ficam isolados** — os dados de staging nunca se misturam com os de produção.

**Rodar os testes sem .NET instalado:**

```bash
docker build --target test .
```

---

## ⚙️ Pipeline CI/CD

**Ferramenta:** GitHub Actions — arquivos em [`.github/workflows/`](.github/workflows/).
**Registry de imagens:** GitHub Container Registry (GHCR), integrado ao repositório e sem custo.

### Gatilhos

| Evento | O que roda |
|--------|-----------|
| Pull request para `main`/`develop` | Build + testes + build da imagem (sem publicar, sem deploy) |
| Push em `develop` | CI completo + **deploy em staging** |
| Push em `main` | CI completo + **deploy em staging** → **deploy em produção** (com aprovação) |
| Manual (`workflow_dispatch`) | Igual ao push do branch escolhido |

### Etapas

| # | Job | O que faz | Por quê |
|---|-----|-----------|---------|
| 1 | 🔨 **Build & 🧪 Testes** | `dotnet restore` → `dotnet build -c Release` → `dotnet test` (xUnit, 18 testes de integração com banco InMemory) + cobertura | Nenhum código quebrado chega a virar imagem. O resultado vai para o resumo da execução e como artifact `resultados-testes`. |
| 2 | 🐳 **Build & Push da imagem** | Build multi-stage com cache, push para `ghcr.io/<usuário>/<repo>:sha-<commit>` | A tag com o SHA do commit é **imutável**: sabemos exatamente qual código está em cada ambiente. |
| 3 | 🚀 **Staging** | Gera o `.env` a partir dos *secrets* do Environment `staging`, roda `scripts/deploy.sh staging` (Docker Compose: API + Oracle), espera o `/health`, executa **12 smoke tests** ponta a ponta e promove a imagem para a tag `:staging` | Valida a imagem em um ambiente igual ao de produção antes de liberar. |
| 4 | 🏭 **Produção** | Mesmo workflow de deploy (`deploy.yml`), com `ASPNETCORE_ENVIRONMENT=Production`, porta 8080, **a mesma imagem** testada em staging, e promoção para `:production` | Só roda na `main`, depois de staging passar, e pode exigir **aprovação manual** (Required reviewers). |

**Smoke tests** (`scripts/smoke-test.sh`): health check → Swagger → registrar usuário → login (JWT) → criar sensor → listar sensores → registrar leitura de 900 L com pressão 0,3 bar (dispara alertas de *vazamento* e *consumo excessivo*) → listar leituras → alertas **sem token = 401** → alertas com token = 200 → relatório do sensor → relatório geral. Se qualquer status HTTP for diferente do esperado, o deploy falha.

**Evidências automáticas:** cada deploy publica o artifact `evidencias-<ambiente>` (respostas JSON, `docker compose ps`, logs da API e do Oracle, redes e volumes) e escreve um resumo com tabelas na página da execução.

### Como configurar no seu GitHub (uma vez)

1. Crie um repositório e envie o projeto (`git push` para `main` e crie também o branch `develop`).
2. **Settings → Actions → General → Workflow permissions:** marque *Read and write permissions*.
3. **Settings → Environments:** crie `staging` e `production`.
   No `production`, marque **Required reviewers** e adicione você — isso cria o portão de aprovação manual.
4. *(Opcional)* Em cada Environment, crie os secrets `ORACLE_PASSWORD`, `ORACLE_APP_PASSWORD` e `JWT_KEY` (só letras e números). Se não existirem, o pipeline gera valores aleatórios fortes a cada deploy.
5. Faça um push em `main` → aba **Actions** → acompanhe → clique em **Review deployments → Approve** quando o job de produção pedir.

> **Sobre os ambientes:** por padrão, os deploys rodam em runners do GitHub (`ubuntu-latest`), que são máquinas temporárias — o ambiente é criado, validado com os smoke tests, documentado e descartado ao fim do job. Para um servidor permanente, basta registrar um *self-hosted runner* na VM/servidor e criar a variável de repositório `DEPLOY_RUNNER=self-hosted`: o mesmo `deploy.sh` passa a manter staging (porta 8081) e produção (porta 8080) no ar.

---

## 📦 Containerização

### Dockerfile

```dockerfile
# syntax=docker/dockerfile:1
ARG DOTNET_VERSION=8.0

# ─── 1) BUILD: SDK completo, usado só para compilar ─────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY src/AquaMonitor.API/AquaMonitor.API.csproj src/AquaMonitor.API/
RUN dotnet restore src/AquaMonitor.API/AquaMonitor.API.csproj
COPY src/AquaMonitor.API/ src/AquaMonitor.API/
RUN dotnet publish src/AquaMonitor.API/AquaMonitor.API.csproj \
      -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ─── 2) TEST (opcional): docker build --target test . ───────────────────────
FROM build AS test
COPY src/AquaMonitor.Tests/AquaMonitor.Tests.csproj src/AquaMonitor.Tests/
RUN dotnet restore src/AquaMonitor.Tests/AquaMonitor.Tests.csproj
COPY src/AquaMonitor.Tests/ src/AquaMonitor.Tests/
RUN dotnet test src/AquaMonitor.Tests/AquaMonitor.Tests.csproj --no-restore -c Release

# ─── 3) RUNTIME: só o ASP.NET Runtime + binários publicados ────────────────
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
ARG APP_VERSION=dev
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    APP_VERSION=${APP_VERSION}
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=15s --timeout=5s --start-period=90s --retries=5 \
  CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "AquaMonitor.API.dll"]
```

### Estratégias adotadas

| Estratégia | Como | Benefício |
|-----------|------|-----------|
| **Multi-stage build** | SDK só no estágio `build`; imagem final usa `aspnet` | Imagem final bem menor (runtime ~220 MB × SDK ~850 MB) e sem compilador (menos superfície de ataque) |
| **Cache de camadas** | `COPY *.csproj` + `restore` antes de copiar o código | Mudou só o código? O restore de pacotes é reaproveitado |
| **Usuário não-root** | `USER $APP_UID` (usuário `app` da imagem oficial) | Se a aplicação for comprometida, o invasor não é root no container |
| **HEALTHCHECK** | `curl /health` (API + conexão com Oracle) | Docker/Compose sabem quando a API está realmente pronta |
| **Configuração por ambiente** | Nada sensível no código; tudo via variáveis (`ConnectionStrings__OracleConnection`, `Jwt__Key`) | A mesma imagem roda em local, staging e produção |
| **Versão rastreável** | `APP_VERSION` = SHA do commit, exibida no `/health` | Prova qual versão está rodando em cada ambiente |
| **`.dockerignore`** | Exclui `bin/`, `obj/`, `.git`, `.env` | Build mais rápido e nenhum segredo dentro da imagem |

### Orquestração (`docker-compose.yml`)

| Recurso | Configuração |
|---------|-------------|
| **Serviços** | `api` (ASP.NET Core 8) + `oracle-db` (`gvenzl/oracle-free:23-slim`) |
| **Volume** | `oracle-data` → `/opt/oracle/oradata` (dados persistem entre reinícios) |
| **Redes** | `backend` (*internal*: o banco não tem porta publicada nem acesso à internet) e `frontend` (exposição da API) |
| **Variáveis** | Lidas do `.env` (local) ou `deploy/.env.<ambiente>` (gerado pelo pipeline); obrigatórias validadas com `${VAR:?mensagem}` |
| **Ordem de subida** | `depends_on: condition: service_healthy` — a API só sobe quando o Oracle está pronto |
| **Resiliência** | `restart: unless-stopped` em ambos os serviços |

---

## 📸 Prints do funcionamento

> Salve os prints em `docs/prints/` com os nomes abaixo — eles aparecem aqui automaticamente.

| # | Evidência | Imagem |
|---|-----------|--------|
| 1 | Pipeline completo (build → testes → imagem → staging → produção) | ![Pipeline](docs/prints/01-pipeline-completo.png) |
| 2 | Job de build e testes (18 testes aprovados) | ![Testes](docs/prints/02-build-testes.png) |
| 3 | Imagem publicada no GHCR | ![Imagem](docs/prints/03-imagem-ghcr.png) |
| 4 | Aprovação manual do deploy em produção | ![Aprovação](docs/prints/04-aprovacao-producao.png) |
| 5 | Staging: resumo do deploy + smoke tests | ![Staging](docs/prints/05-staging-smoke-tests.png) |
| 6 | Produção: resumo do deploy + smoke tests | ![Produção](docs/prints/06-producao-smoke-tests.png) |
| 7 | Environments `staging` e `production` com deploys | ![Environments](docs/prints/07-environments.png) |
| 8 | Local: `docker compose ps` com containers *healthy* | ![Compose](docs/prints/08-docker-compose-ps.png) |
| 9 | Local: Swagger + `/health` | ![Swagger](docs/prints/09-swagger-health.png) |

🔗 Execução do pipeline: https://github.com/Biancanss0/aquamonitor-devops/actions

---

## 🧰 Tecnologias utilizadas

| Camada | Tecnologia |
|--------|-----------|
| Linguagem / Framework | C# 12, **ASP.NET Core 8** (Web API, arquitetura MVVM) |
| Persistência | **Entity Framework Core 8** + Oracle (`Oracle.EntityFrameworkCore`), migrations |
| Banco de dados | **Oracle Database 23ai Free** (container `gvenzl/oracle-free`) |
| Segurança | Autenticação **JWT Bearer**, senhas com **BCrypt** |
| Documentação da API | Swagger / OpenAPI (Swashbuckle) |
| Testes | **xUnit**, `WebApplicationFactory` (testes de integração), EF Core InMemory, Coverlet |
| Observabilidade | ASP.NET Core **Health Checks** (`/health`) |
| Containers | **Docker** (multi-stage), **Docker Compose** |
| CI/CD | **GitHub Actions** (workflow reutilizável, Environments, aprovação manual) |
| Registry | **GitHub Container Registry (GHCR)** |
| Scripts | Bash (`deploy.sh`, `smoke-test.sh`), Python (resumo dos testes) |

---

## 📚 Referência da API

### 🔓 Públicos

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/health` | Status da API e do banco, ambiente e versão |
| POST | `/api/Auth/registrar` | Registra novo usuário |
| POST | `/api/Auth/login` | Login → retorna token JWT |
| GET/POST | `/api/Sensores` | Lista (paginado) / cadastra sensores |
| GET/PUT/DELETE | `/api/Sensores/{id}` | Busca / atualiza / remove sensor |
| GET/POST | `/api/Leituras` | Lista (paginado) / registra leitura com detecção automática |
| GET/DELETE | `/api/Leituras/{id}` | Busca / remove leitura |

### 🔐 Protegidos (Bearer Token)

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/Alertas` | Lista alertas (paginado, filtros) |
| GET | `/api/Alertas/{id}` | Busca alerta |
| PATCH | `/api/Alertas/{id}/resolver` | Marca alerta como resolvido |
| DELETE | `/api/Alertas/{id}` | Remove alerta (perfil Admin) |
| GET | `/api/Relatorios/geral` | Relatório geral por período |
| GET | `/api/Relatorios/sensor/{id}` | Relatório por sensor |

Paginação: `?pagina=1&tamanhoPagina=10`.

### Estrutura do projeto

```
AquaMonitor/
├── .github/workflows/
│   ├── ci-cd.yml            → pipeline principal (build, testes, imagem, deploys)
│   └── deploy.yml           → workflow reutilizável de deploy (staging/produção)
├── deploy/                  → modelos de .env por ambiente
├── scripts/
│   ├── deploy.sh            → deploy via Docker Compose + espera do /health
│   ├── smoke-test.sh        → 12 verificações ponta a ponta pós-deploy
│   └── test-summary.py      → resumo dos testes para o GitHub
├── src/
│   ├── AquaMonitor.API/     → Controllers, ViewModels, Models, Services, Data, Health, Migrations
│   └── AquaMonitor.Tests/   → testes xUnit
├── docs/prints/             → evidências (prints)
├── Dockerfile
├── docker-compose.yml
├── .env.example
└── README.md
```
