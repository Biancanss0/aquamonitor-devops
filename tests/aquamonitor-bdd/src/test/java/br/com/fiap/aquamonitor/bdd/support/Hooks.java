package br.com.fiap.aquamonitor.bdd.support;

import io.cucumber.java.After;
import io.cucumber.java.Before;
import io.cucumber.java.Scenario;
import io.restassured.response.Response;
import org.opentest4j.TestAbortedException;

import java.nio.charset.StandardCharsets;

/**
 * Ganchos executados antes/depois de cada cenário.
 *
 * <ul>
 *   <li><b>@Before("@admin")</b>: cenários de administrador só rodam se as credenciais de admin
 *       estiverem configuradas; caso contrário são marcados como <i>skipped</i> (e não como falha).</li>
 *   <li><b>@After(order = 20)</b>: anexa ao relatório a lista de chamadas HTTP e a última resposta
 *       — é a evidência de execução de cada cenário.</li>
 *   <li><b>@After(order = 10)</b>: remove os sensores criados (a exclusão em cascata leva junto
 *       leituras e alertas). O banco não acumula lixo e um cenário não interfere no outro.</li>
 * </ul>
 * No Cucumber os @After rodam do maior "order" para o menor.
 */
public class Hooks {

    private final TestContext ctx;
    private final ApiClient api;

    public Hooks(TestContext ctx, ApiClient api) {
        this.ctx = ctx;
        this.api = api;
    }

    @Before(value = "@admin", order = 1)
    public void exigirCredenciaisDeAdministrador() {
        if (!Config.adminConfigurado()) {
            throw new TestAbortedException(
                    "Cenário @admin ignorado: defina ADMIN_EMAIL e ADMIN_SENHA (veja scripts/criar-admin.sh).");
        }
    }

    @After(order = 20)
    public void anexarEvidencias(Scenario cenario) {
        if (!ctx.getHistoricoChamadas().isEmpty()) {
            String historico = "API: " + Config.baseUrl() + "\n\n"
                    + String.join("\n\n", ctx.getHistoricoChamadas());
            cenario.attach(historico.getBytes(StandardCharsets.UTF_8), "text/plain", "Chamadas HTTP do cenário");
        }
        Response resposta = ctx.getResposta();
        if (resposta != null) {
            String corpo = resposta.asString();
            String texto = "HTTP " + resposta.getStatusCode() + " (" + resposta.getTime() + " ms)\n"
                    + (corpo == null || corpo.isBlank() ? "(sem corpo)" : corpo);
            cenario.attach(texto.getBytes(StandardCharsets.UTF_8), "text/plain", "Resposta validada no cenário");
        }
    }

    @After(order = 10)
    public void limparDadosCriados() {
        while (!ctx.getSensoresCriados().isEmpty()) {
            Integer id = ctx.getSensoresCriados().pop();
            try {
                api.delete("/api/Sensores/" + id, null); // 204, ou 404 se o cenário já excluiu
            } catch (RuntimeException e) {
                System.err.println("[limpeza] não foi possível remover o sensor " + id + ": " + e.getMessage());
            }
        }
    }
}
