package br.com.fiap.aquamonitor.bdd.steps;

import br.com.fiap.aquamonitor.bdd.support.ApiClient;
import br.com.fiap.aquamonitor.bdd.support.TestContext;
import io.cucumber.java.pt.Entao;
import io.cucumber.java.pt.Quando;

import java.util.List;
import java.util.Map;

import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

/** Passos de /health e da documentação OpenAPI (Swagger). */
public class SaudeSteps {

    private final TestContext ctx;
    private final ApiClient api;

    public SaudeSteps(TestContext ctx, ApiClient api) {
        this.ctx = ctx;
        this.api = api;
    }

    @Quando("eu consulto o endpoint de saúde da API")
    public void euConsultoOEndpointDeSaude() {
        ctx.setResposta(api.get("/health"));
    }

    @Entao("o banco de dados deve estar saudável")
    public void oBancoDeDadosDeveEstarSaudavel() {
        List<Map<String, Object>> checks = ctx.getResposta().jsonPath().getList("checks");
        assertNotNull(checks, "O /health deveria listar os checks");
        boolean bancoOk = checks.stream()
                .anyMatch(c -> "oracle-db".equals(c.get("nome")) && "Healthy".equals(c.get("status")));
        assertTrue(bancoOk, "O check 'oracle-db' deveria estar Healthy: " + checks);
    }

    @Quando("eu consulto a documentação OpenAPI")
    public void euConsultoADocumentacaoOpenApi() {
        ctx.setResposta(api.get("/swagger/v1/swagger.json"));
    }

    @Entao("a documentação deve listar o caminho {string}")
    public void aDocumentacaoDeveListarOCaminho(String caminho) {
        Map<String, Object> caminhos = ctx.getResposta().jsonPath().getMap("paths");
        assertNotNull(caminhos, "O documento OpenAPI deveria ter 'paths'");
        assertTrue(caminhos.containsKey(caminho), "O caminho " + caminho + " não está documentado: " + caminhos.keySet());
    }
}
