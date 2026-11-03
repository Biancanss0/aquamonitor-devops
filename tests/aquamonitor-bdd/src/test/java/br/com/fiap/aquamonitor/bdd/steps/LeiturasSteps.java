package br.com.fiap.aquamonitor.bdd.steps;

import br.com.fiap.aquamonitor.bdd.support.ApiClient;
import br.com.fiap.aquamonitor.bdd.support.Preparacao;
import br.com.fiap.aquamonitor.bdd.support.TestContext;
import io.cucumber.java.pt.Dado;
import io.cucumber.java.pt.Entao;
import io.cucumber.java.pt.Quando;
import io.restassured.response.Response;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import static org.hamcrest.Matchers.equalTo;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

/** Passos de /api/Leituras e da detecção automática de vazamento/consumo excessivo. */
public class LeiturasSteps {

    private final TestContext ctx;
    private final ApiClient api;
    private final Preparacao preparacao;

    public LeiturasSteps(TestContext ctx, ApiClient api, Preparacao preparacao) {
        this.ctx = ctx;
        this.api = api;
        this.preparacao = preparacao;
    }

    // ─── Dado ─────────────────────────────────────────────────────────────────

    @Dado("que o sensor possui uma leitura registrada de {int} litros")
    public void queOSensorPossuiUmaLeituraRegistrada(int consumo) {
        ctx.setLeituraId(preparacao.registrarLeitura(ctx.getSensorId(), consumo, 3.0));
    }

    @Dado("que o sensor possui uma leitura com vazamento registrada")
    public void queOSensorPossuiUmaLeituraComVazamento() {
        preparacao.registrarLeitura(ctx.getSensorId(), 80, 0.2); // pressão < 0,5 bar ⇒ vazamento
    }

    // ─── Quando ───────────────────────────────────────────────────────────────

    @Quando("eu registro uma leitura de {int} litros com pressão de {word} bar e temperatura de {int} graus")
    public void euRegistroUmaLeitura(int consumo, String pressao, int temperatura) {
        Map<String, Object> leitura = preparacao.novaLeitura(
                ctx.getSensorId(), consumo, Preparacao.numero(pressao), (double) temperatura);
        enviarLeitura(leitura);
    }

    @Quando("eu registro uma leitura de {int} litros para o sensor de identificador {int}")
    public void euRegistroUmaLeituraParaOSensor(int consumo, int sensorId) {
        enviarLeitura(preparacao.novaLeitura(sensorId, consumo, 3.0, 22.0));
    }

    @Quando("eu registro uma leitura com o campo {string} igual a {string}")
    public void euRegistroUmaLeituraComOCampo(String campo, String valor) {
        Map<String, Object> leitura = preparacao.novaLeitura(ctx.getSensorId(), 100, 3.0, 22.0);
        leitura.put(campo, Preparacao.valorTipado(campo, valor));
        enviarLeitura(leitura);
    }

    private void enviarLeitura(Map<String, Object> leitura) {
        Response r = api.post("/api/Leituras", leitura, null);
        ctx.setResposta(r);
        if (r.getStatusCode() == 201) {
            ctx.setLeituraId(r.jsonPath().getInt("Id"));
        }
    }

    @Quando("eu consulto a leitura pelo seu identificador")
    public void euConsultoALeituraPeloSeuIdentificador() {
        ctx.setResposta(api.get("/api/Leituras/" + ctx.getLeituraId()));
    }

    @Quando("eu consulto a leitura de identificador {int}")
    public void euConsultoALeituraDeIdentificador(int id) {
        ctx.setResposta(api.get("/api/Leituras/" + id));
    }

    @Quando("eu listo as leituras do sensor filtrando apenas vazamentos")
    public void euListoAsLeiturasDoSensorFiltrandoVazamentos() {
        Map<String, Object> query = new LinkedHashMap<>();
        query.put("sensorId", ctx.getSensorId());
        query.put("vazamento", true);
        query.put("pagina", 1);
        query.put("tamanhoPagina", 10);
        ctx.setResposta(api.get("/api/Leituras", null, query));
    }

    @Quando("eu excluo a leitura")
    public void euExcluoALeitura() {
        ctx.setResposta(api.delete("/api/Leituras/" + ctx.getLeituraId(), null));
    }

    @Quando("eu excluo a leitura de identificador {int}")
    public void euExcluoALeituraDeIdentificador(int id) {
        ctx.setResposta(api.delete("/api/Leituras/" + id, null));
    }

    // ─── Então ────────────────────────────────────────────────────────────────

    @Entao("a leitura não deve indicar vazamento")
    public void aLeituraNaoDeveIndicarVazamento() {
        ctx.getResposta().then().body("VazamentoDetectado", equalTo(false));
    }

    @Entao("o indicador de vazamento da leitura deve ser {string}")
    public void oIndicadorDeVazamentoDeveSer(String simOuNao) {
        boolean esperado = simOuNao.trim().equalsIgnoreCase("sim");
        ctx.getResposta().then().body("VazamentoDetectado", equalTo(esperado));
    }

    @Entao("a quantidade de alertas automáticos do sensor deve ser {int}")
    public void aQuantidadeDeAlertasDoSensorDeveSer(int quantidade) {
        List<Map<String, Object>> alertas = preparacao.alertasDoSensor(ctx.getSensorId());
        assertEquals(quantidade, alertas.size(), "Quantidade de alertas gerados automaticamente: " + alertas);
    }

    @Entao("deve existir um alerta do tipo {string} com nível {string}")
    public void deveExistirUmAlertaDoTipoComNivel(String tipo, String nivel) {
        List<Map<String, Object>> alertas = preparacao.alertasDoSensor(ctx.getSensorId());
        boolean existe = alertas.stream()
                .anyMatch(a -> tipo.equals(a.get("Tipo")) && nivel.equals(a.get("Nivel")));
        assertTrue(existe, "Esperava um alerta " + tipo + "/" + nivel + ", mas os alertas foram: " + alertas);
    }

    @Entao("a leitura não deve mais ser encontrada")
    public void aLeituraNaoDeveMaisSerEncontrada() {
        Response r = api.get("/api/Leituras/" + ctx.getLeituraId());
        assertEquals(404, r.getStatusCode(), "Depois de excluída, a leitura não deveria ser encontrada");
    }
}
