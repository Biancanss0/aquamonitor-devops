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
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

/** Passos de /api/Sensores (CRUD + paginação). */
public class SensoresSteps {

    private final TestContext ctx;
    private final ApiClient api;
    private final Preparacao preparacao;

    public SensoresSteps(TestContext ctx, ApiClient api, Preparacao preparacao) {
        this.ctx = ctx;
        this.api = api;
        this.preparacao = preparacao;
    }

    // ─── Dado ─────────────────────────────────────────────────────────────────

    @Dado("que eu preencho um sensor {string} com limite de consumo de {int} litros")
    public void queEuPreenchoUmSensor(String tipo, int limite) {
        ctx.getPayload().clear();
        ctx.getPayload().putAll(preparacao.novoSensor(tipo, limite));
    }

    @Dado("o campo {string} do sensor recebe o valor {string}")
    public void oCampoDoSensorRecebeOValor(String campo, String valor) {
        ctx.getPayload().put(campo, Preparacao.valorTipado(campo, valor));
    }

    @Dado("que existe um sensor {string} cadastrado com limite de {int} litros")
    public void queExisteUmSensorCadastrado(String tipo, int limite) {
        ctx.setSensorId(preparacao.criarSensor(tipo, limite));
    }

    @Dado("que existem {int} sensores {string} cadastrados")
    public void queExistemSensoresCadastrados(int quantidade, String tipo) {
        for (int i = 0; i < quantidade; i++) {
            ctx.setSensorId(preparacao.criarSensor(tipo, 1000));
        }
    }

    @Dado("que o sensor foi desativado")
    public void queOSensorFoiDesativado() {
        preparacao.desativarSensor(ctx.getSensorId());
    }

    // ─── Quando ───────────────────────────────────────────────────────────────

    @Quando("eu cadastro o sensor")
    public void euCadastroOSensor() {
        Response r = api.post("/api/Sensores", ctx.getPayload(), null);
        ctx.setResposta(r);
        if (r.getStatusCode() == 201) {
            int id = r.jsonPath().getInt("Id");
            ctx.setSensorId(id);
            ctx.getSensoresCriados().push(id); // garante a limpeza no final do cenário
        }
    }

    @Quando("eu consulto o sensor pelo seu identificador")
    public void euConsultoOSensorPeloSeuIdentificador() {
        ctx.setResposta(api.get("/api/Sensores/" + ctx.getSensorId()));
    }

    @Quando("eu consulto o sensor de identificador {int}")
    public void euConsultoOSensorDeIdentificador(int id) {
        ctx.setResposta(api.get("/api/Sensores/" + id));
    }

    @Quando("eu listo os sensores do tipo {string} na página {int} com {int} itens por página")
    public void euListoOsSensoresDoTipo(String tipo, int pagina, int tamanhoPagina) {
        Map<String, Object> query = new LinkedHashMap<>();
        query.put("tipo", tipo);
        query.put("pagina", pagina);
        query.put("tamanhoPagina", tamanhoPagina);
        ctx.setResposta(api.get("/api/Sensores", null, query));
    }

    @Quando("eu listo os sensores na página {int} com {int} itens por página")
    public void euListoOsSensores(int pagina, int tamanhoPagina) {
        Map<String, Object> query = new LinkedHashMap<>();
        query.put("pagina", pagina);
        query.put("tamanhoPagina", tamanhoPagina);
        ctx.setResposta(api.get("/api/Sensores", null, query));
    }

    @Quando("eu reduzo o limite do sensor para {int} litros e o desativo")
    public void euReduzoOLimiteDoSensorEODesativo(int novoLimite) {
        Map<String, Object> corpo = preparacao.novoSensor("Residencial", novoLimite);
        corpo.put("Ativo", false);
        ctx.setResposta(api.put("/api/Sensores/" + ctx.getSensorId(), corpo, null));
    }

    @Quando("eu atualizo o sensor de identificador {int}")
    public void euAtualizoOSensorDeIdentificador(int id) {
        Map<String, Object> corpo = preparacao.novoSensor("Comercial", 500);
        corpo.put("Ativo", true);
        ctx.setResposta(api.put("/api/Sensores/" + id, corpo, null));
    }

    @Quando("eu excluo o sensor")
    public void euExcluoOSensor() {
        ctx.setResposta(api.delete("/api/Sensores/" + ctx.getSensorId(), null));
    }

    @Quando("eu excluo o sensor de identificador {int}")
    public void euExcluoOSensorDeIdentificador(int id) {
        ctx.setResposta(api.delete("/api/Sensores/" + id, null));
    }

    // ─── Então ────────────────────────────────────────────────────────────────

    @Entao("o sensor retornado deve estar ativo")
    public void oSensorRetornadoDeveEstarAtivo() {
        ctx.getResposta().then().body("Ativo", equalTo(true));
    }

    @Entao("o sensor retornado deve estar inativo")
    public void oSensorRetornadoDeveEstarInativo() {
        ctx.getResposta().then().body("Ativo", equalTo(false));
    }

    @Entao("o cabeçalho {string} deve apontar para o sensor criado")
    public void oCabecalhoDeveApontarParaOSensorCriado(String cabecalho) {
        String valor = ctx.getResposta().getHeader(cabecalho);
        assertNotNull(valor, "O cabeçalho '" + cabecalho + "' não veio na resposta");
        assertTrue(valor.toLowerCase().endsWith("/api/sensores/" + ctx.getSensorId()),
                "O cabeçalho '" + cabecalho + "' deveria apontar para /api/Sensores/" + ctx.getSensorId()
                        + ", mas veio: " + valor);
    }

    @Entao("o sensor não deve mais ser encontrado")
    public void oSensorNaoDeveMaisSerEncontrado() {
        Response r = api.get("/api/Sensores/" + ctx.getSensorId());
        assertEquals(404, r.getStatusCode(), "Depois de excluído, o sensor não deveria ser encontrado");
    }
}
