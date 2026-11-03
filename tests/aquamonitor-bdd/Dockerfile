# =============================================================================
#  Imagem que executa a suíte BDD (Cucumber + RestAssured) contra a API
#  — permite rodar os testes SEM instalar Java nem Maven na máquina.
#
#  Build:  docker build -t aquamonitor-bdd-tests tests/aquamonitor-bdd
#  Rodar (com a API do docker compose no ar):
#     docker run --rm --network aquamonitor_frontend \
#       -e API_BASE_URL=http://api:8080 \
#       -v "$PWD/tests/aquamonitor-bdd/target:/tests/target" \
#       aquamonitor-bdd-tests
#
#  Os relatórios ficam em tests/aquamonitor-bdd/target/cucumber-reports.
# =============================================================================
FROM maven:3.9-eclipse-temurin-17

WORKDIR /tests

# 1) Só o pom primeiro: a camada de dependências fica em cache e só é refeita
#    quando o pom.xml muda (build bem mais rápido nas próximas vezes).
COPY pom.xml .
RUN mvn -B -q dependency:go-offline

# 2) Código dos testes. O "test" com um filtro que não casa com nenhum teste
#    compila tudo e baixa os plugins do Surefire ainda durante o build da imagem.
COPY src ./src
RUN mvn -B -q test -Dtest=NenhumTeste -Dsurefire.failIfNoSpecifiedTests=false

# Usuário sem privilégios de root (boa prática de segurança em containers)
RUN useradd --create-home --uid 10001 tester \
 && mkdir -p /tests/target \
 && chown -R tester:tester /tests \
 && cp -r /root/.m2 /home/tester/.m2 \
 && chown -R tester:tester /home/tester/.m2
USER tester

ENV API_BASE_URL=http://api:8080

# Argumentos extras podem ser passados no docker run, ex.: -Dcucumber.filter.tags=@sensores
ENTRYPOINT ["mvn", "-B", "test"]
