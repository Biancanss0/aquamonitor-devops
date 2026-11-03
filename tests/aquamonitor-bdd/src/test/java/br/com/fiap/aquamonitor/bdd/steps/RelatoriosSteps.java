package br.com.fiap.aquamonitor.bdd.steps;

import br.com.fiap.aquamonitor.bdd.support.ApiClient;
import br.com.fiap.aquamonitor.bdd.support.Preparacao;
import br.com.fiap.aquamonitor.bdd.support.TestContext;
import io.cucumber.java.pt.Dado;
import io.cucumber.java.pt.Entao;
import io.cucumber.java.pt.Quando;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import static org.hamcrest.Matchers.equalTo;
import static org.hamcrest.Matchers.greaterThan;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

/** Passos de /api/Relatorios (impacto hídrico por sensor e geral). */
public class RelatoriosSteps {

    private final TestContext ctx;
    private final ApiClient api;
    private final Preparacao preparacao;

    public RelatoriosSteps(TestContext ctx, ApiClient api, Preparacao preparacao) {
        this.ctx = ctx;
        this.api = api;
        this.preparacao = preparacao;
    }

    // ─── Dado ─────────────────────────────────────────────────────────────────

    /** Ex.: "300, 400" → registra duas leituras normais (pressão 3 bar) no sensor do cenário. */
    @Dado("o sensor recebe as leituras {string} em litros")
    public void oSensorRecebeAsLeituras(String leituras) {
        for (String valor : leituras.split(",")) {
            preparacao.registrarLeitura(ctx.getSensorId(), Preparacao.numero(valor), 3.0);
        }
    }

    // ─── Quando ───────────────────────────────────────────────────────────────

    @Quando("eu gero o relatório de impacto hídrico do sensor")
    public void euGeroORelatorioDoSensor() {
        ctx.setResposta(api.get("/api/Relatorios/sensor/" + ctx.getSensorId(), ctx.getTokenAtual(), null));
    }

    @Quando("eu gero o relatório de impacto hídrico do sensor de identificador {int}")
    public void euGeroORelatorioDoSensorDeIdentificador(int id) {
        ctx.setResposta(api.get("/api/Relatorios/sensor/" + id, ctx.getTokenAtual(), null));
    }

    @Quando("eu gero o relatório geral de consumo")
    public void euGeroORelatorioGeral() {
        ctx.setResposta(api.get("/api/Relatorios/geral", ctx.getTokenAtual(), null));
    }

    @Quando("eu gero o relatório geral de {string} até {string}")
    public void euGeroORelatorioGeralNoPeriodo(String de, String ate) {
        Map<String, Object> query = new LinkedHashMap<>();
        query.put("de", de);
        query.put("ate", ate);
        ctx.setResposta(api.get("/api/Relatorios/geral", ctx.getTokenAtual(), query));
    }

    @Quando("eu gero o relatório geral sem informar token")
    public void euGeroORelatorioGeralSemToken() {
        ctx.setResposta(api.get("/api/Relatorios/geral", null, null));
    }

    // ─── Então ────────────────────────────────────────────────────────────────

    @Entao("o relatório deve contabilizar {int} leituras")
    public void oRelatorioDeveContabilizarLeituras(int quantidade) {
        ctx.getResposta().then().body("TotalLeituras", equalTo(quantidade));
    }

    @Entao("o consumo total do relatório deve ser {int} litros")
    public void oConsumoTotalDoRelatorioDeveSer(int litros) {
        double consumo = ctx.getResposta().jsonPath().getDouble("ConsumoTotal");
        assertEquals(litros, consumo, 0.0001, "Consumo total do relatório");
    }

    @Entao("o percentual do limite deve ser {word}")
    public void oPercentualDoLimiteDeveSer(String percentual) {
        double obtido = ctx.getResposta().jsonPath().getDouble("PercentualDoLimite");
        assertEquals(Preparacao.numero(percentual), obtido, 0.0001, "Percentual do limite de consumo");
    }

    @Entao("o status de consumo deve ser {string}")
    public void oStatusDeConsumoDeveSer(String status) {
        ctx.getResposta().then().body("StatusConsumo", equalTo(status));
    }

    @Entao("o relatório geral deve incluir o sensor cadastrado")
    public void oRelatorioGeralDeveIncluirOSensor() {
        List<Integer> ids = ctx.getResposta().jsonPath().getList("DetalhesPorSensor.SensorId", Integer.class);
        assertNotNull(ids, "O relatório geral deveria trazer DetalhesPorSensor");
        assertTrue(ids.contains(ctx.getSensorId()),
                "O sensor " + ctx.getSensorId() + " deveria aparecer no relatório geral: " + ids);
    }

    @Entao("o relatório geral deve apontar alertas não resolvidos")
    public void oRelatorioGeralDeveApontarAlertasNaoResolvidos() {
        ctx.getResposta().then().body("AlertasNaoResolvidos", greaterThan(0));
    }
}
