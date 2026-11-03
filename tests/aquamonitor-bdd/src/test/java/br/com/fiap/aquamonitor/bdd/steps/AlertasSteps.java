package br.com.fiap.aquamonitor.bdd.steps;

import br.com.fiap.aquamonitor.bdd.support.ApiClient;
import br.com.fiap.aquamonitor.bdd.support.Preparacao;
import br.com.fiap.aquamonitor.bdd.support.TestContext;
import io.cucumber.java.pt.Dado;
import io.cucumber.java.pt.Entao;
import io.cucumber.java.pt.Quando;
import io.restassured.response.Response;

import java.util.LinkedHashMap;
import java.util.Map;

import static org.hamcrest.Matchers.equalTo;
import static org.hamcrest.Matchers.matchesPattern;
import static org.hamcrest.Matchers.notNullValue;
import static org.junit.jupiter.api.Assertions.assertEquals;

/** Passos de /api/Alertas (consulta, resolução e exclusão com controle de acesso). */
public class AlertasSteps {

    private static final String DATA_HORA = "^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}.*";

    private final TestContext ctx;
    private final ApiClient api;
    private final Preparacao preparacao;

    public AlertasSteps(TestContext ctx, ApiClient api, Preparacao preparacao) {
        this.ctx = ctx;
        this.api = api;
        this.preparacao = preparacao;
    }

    // ─── Dado ─────────────────────────────────────────────────────────────────

    @Dado("que o sensor gerou um alerta de vazamento")
    public void queOSensorGerouUmAlertaDeVazamento() {
        ctx.setAlertaId(preparacao.gerarAlertaDeVazamento(ctx.getSensorId()));
    }

    @Dado("que o alerta já foi resolvido")
    public void queOAlertaJaFoiResolvido() {
        Response r = api.patch("/api/Alertas/" + ctx.getAlertaId() + "/resolver", ctx.getTokenAtual());
        assertEquals(200, r.getStatusCode(), "Pré-condição: a primeira resolução do alerta deveria retornar 200");
    }

    // ─── Quando ───────────────────────────────────────────────────────────────

    @Quando("eu listo os alertas sem informar token")
    public void euListoOsAlertasSemToken() {
        ctx.setResposta(api.get("/api/Alertas", null, null));
    }

    @Quando("eu listo os alertas com o token {string}")
    public void euListoOsAlertasComOToken(String token) {
        ctx.setResposta(api.get("/api/Alertas", token, null));
    }

    @Quando("eu listo os alertas do sensor com nível {string}")
    public void euListoOsAlertasDoSensorComNivel(String nivel) {
        Map<String, Object> query = new LinkedHashMap<>();
        query.put("sensorId", ctx.getSensorId());
        query.put("nivel", nivel);
        ctx.setResposta(api.get("/api/Alertas", ctx.getTokenAtual(), query));
    }

    @Quando("eu consulto o alerta pelo seu identificador")
    public void euConsultoOAlertaPeloSeuIdentificador() {
        ctx.setResposta(api.get("/api/Alertas/" + ctx.getAlertaId(), ctx.getTokenAtual(), null));
    }

    @Quando("eu consulto o alerta de identificador {int}")
    public void euConsultoOAlertaDeIdentificador(int id) {
        ctx.setResposta(api.get("/api/Alertas/" + id, ctx.getTokenAtual(), null));
    }

    @Quando("eu marco o alerta como resolvido")
    public void euMarcoOAlertaComoResolvido() {
        ctx.setResposta(api.patch("/api/Alertas/" + ctx.getAlertaId() + "/resolver", ctx.getTokenAtual()));
    }

    @Quando("eu marco o alerta de identificador {int} como resolvido")
    public void euMarcoOAlertaDeIdentificadorComoResolvido(int id) {
        ctx.setResposta(api.patch("/api/Alertas/" + id + "/resolver", ctx.getTokenAtual()));
    }

    @Quando("eu excluo o alerta")
    public void euExcluoOAlerta() {
        ctx.setResposta(api.delete("/api/Alertas/" + ctx.getAlertaId(), ctx.getTokenAtual()));
    }

    @Quando("eu excluo o alerta de identificador {int}")
    public void euExcluoOAlertaDeIdentificador(int id) {
        ctx.setResposta(api.delete("/api/Alertas/" + id, ctx.getTokenAtual()));
    }

    // ─── Então ────────────────────────────────────────────────────────────────

    /** Rastreabilidade: além do status, a API precisa gravar QUANDO o alerta foi tratado. */
    @Entao("o alerta deve estar resolvido com data de resolução registrada")
    public void oAlertaDeveEstarResolvidoComDataDeResolucao() {
        ctx.getResposta().then()
                .body("Id", equalTo(ctx.getAlertaId()))
                .body("Resolvido", equalTo(true))
                .body("ResolvidoEm", notNullValue())
                .body("ResolvidoEm", matchesPattern(DATA_HORA));
    }

    @Entao("o alerta ainda deve existir")
    public void oAlertaAindaDeveExistir() {
        Response r = api.get("/api/Alertas/" + ctx.getAlertaId(), ctx.getTokenAtual(), null);
        assertEquals(200, r.getStatusCode(), "O alerta não deveria ter sido excluído por um Operador");
    }

    @Entao("o alerta não deve mais ser encontrado")
    public void oAlertaNaoDeveMaisSerEncontrado() {
        Response r = api.get("/api/Alertas/" + ctx.getAlertaId(), ctx.getTokenAtual(), null);
        assertEquals(404, r.getStatusCode(), "Depois de excluído, o alerta não deveria ser encontrado");
    }
}
