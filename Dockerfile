# syntax=docker/dockerfile:1
# =============================================================================
#  AquaMonitor API — Dockerfile multi-stage
#  Estágios:  build  →  test (opcional)  →  runtime (imagem final, enxuta)
# =============================================================================
ARG DOTNET_VERSION=8.0

# ─── 1) BUILD: SDK completo (~800 MB), usado só para compilar ────────────────
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copiamos primeiro SÓ o .csproj e fazemos o restore. Como essa camada só muda
# quando as dependências mudam, o Docker reaproveita o cache nos próximos builds.
COPY src/AquaMonitor.API/AquaMonitor.API.csproj src/AquaMonitor.API/
RUN dotnet restore src/AquaMonitor.API/AquaMonitor.API.csproj

# Agora o restante do código-fonte e o publish em Release.
COPY src/AquaMonitor.API/ src/AquaMonitor.API/
RUN dotnet publish src/AquaMonitor.API/AquaMonitor.API.csproj \
      -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ─── 2) TEST (opcional): docker build --target test . ────────────────────────
# Permite rodar os testes xUnit dentro de um container, sem .NET instalado na máquina.
FROM build AS test
COPY src/AquaMonitor.Tests/AquaMonitor.Tests.csproj src/AquaMonitor.Tests/
RUN dotnet restore src/AquaMonitor.Tests/AquaMonitor.Tests.csproj
COPY src/AquaMonitor.Tests/ src/AquaMonitor.Tests/
RUN dotnet test src/AquaMonitor.Tests/AquaMonitor.Tests.csproj --no-restore -c Release

# ─── 3) RUNTIME: só o ASP.NET Runtime (~220 MB) + binários publicados ────────
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime

# Versão (commit SHA) injetada pelo pipeline; aparece no endpoint /health.
ARG APP_VERSION=dev

# curl é usado apenas pelo HEALTHCHECK abaixo.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    APP_VERSION=${APP_VERSION}

EXPOSE 8080

# Segurança: a imagem oficial já traz o usuário "app" (UID 1654).
# Rodar como não-root reduz o impacto caso a aplicação seja comprometida.
USER $APP_UID

# O Docker marca o container como "healthy" só quando /health responde 200
# (API no ar E banco acessível).
HEALTHCHECK --interval=15s --timeout=5s --start-period=90s --retries=5 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "AquaMonitor.API.dll"]
