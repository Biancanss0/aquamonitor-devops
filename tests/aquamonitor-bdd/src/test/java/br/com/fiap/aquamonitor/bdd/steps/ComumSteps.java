package br.com.fiap.aquamonitor.bdd.steps;

import br.com.fiap.aquamonitor.bdd.support.TestContext;
import io.cucumber.java.pt.Entao;
import io.restassured.response.Response;

import java.util.List;
import java.util.Map;

import static io.restassured.module.jsv.JsonSchemaValidator.matchesJsonSchemaInClasspath;
import static org.hamcrest.Matchers.equalTo;
import static org.hamcrest.Matchers.lessThan;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

/**
 * Passos "Então" reutilizados por todas as funcionalidades.
 *
 * <p>Aqui ficam as três validações exigidas para cada API:</p>
 * <ol>
 *   <li><b>status code</b> → {@code o status da resposta deve ser ...}</li>
 *   <li><b>corpo JSON</b>   → {@code o campo ... da resposta deve ser ...}, {@code a mensagem de erro ...}</li>
 *   <li><b>contrato</b>     → {@code a resposta deve respeitar o contrato "..."} (JSON Schema)</li>
 * </ol>
 */
public class ComumSteps {

    private final TestContext ctx;

    public ComumSteps(TestContext ctx) {
        this.ctx = ctx;
    }

    private Response resposta() {
        Response r = ctx.getResposta();
        assertNotNull(r, "Nenhuma requisição foi executada no passo 'Quando' deste cenário");
        return r;
    }

    // ─── 1. Status code ───────────────────────────────────────────────────────

    @Entao("o status da resposta deve ser {int}")
    public void oStatusDaRespostaDeveSer(int statusEsperado) {
        resposta().then().statusCode(statusEsperado);
    }

    // ─── 2. Contrato (JSON Schema) ────────────────────────────────────────────

    /**
     * Valida a resposta inteira contra {@code src/test/resources/schemas/<contrato>.schema.json}.
     * Os schemas usam {@code required} + {@code additionalProperties: false}: se a API remover,
     * renomear ou acrescentar um campo, ou mudar um tipo, o teste de contrato quebra.
     */
    @Entao("a resposta deve respeitar o contrato {string}")
    public void aRespostaDeveRespeitarOContrato(String contrato) {
        resposta().then().body(matchesJsonSchemaInClasspath("schemas/" + contrato + ".schema.json"));
    }

    // ─── 3. Corpo da resposta ─────────────────────────────────────────────────

    @Entao("a mensagem de erro deve ser {string}")
    public void aMensagemDeErroDeveSer(String mensagem) {
        resposta().then().body("mensagem", equalTo(mensagem));
    }

    @Entao("a resposta deve apontar erro de validação no campo {string}")
    public void aRespostaDeveApontarErroDeValidacaoNoCampo(String campo) {
        Map<String, Object> erros = resposta().jsonPath().getMap("errors");
        assertNotNull(erros, "A resposta de validação deveria trazer o objeto 'errors'");
        boolean apontaCampo = erros.keySet().stream().anyMatch(chave ->
                chave.equalsIgnoreCase(campo) || chave.toLowerCase().endsWith("." + campo.toLowerCase()));
        assertTrue(apontaCampo, "Esperava erro de validação no campo '" + campo + "', mas veio: " + erros);
    }

    @Entao("o campo {string} da resposta deve ser {string}")
    public void oCampoDaRespostaDeveSerTexto(String campo, String esperado) {
        Object valor = resposta().jsonPath().get(campo);
        assertEquals(esperado, String.valueOf(valor), "Valor inesperado no campo '" + campo + "'");
    }

    @Entao("o campo {string} da resposta deve ser {int}")
    public void oCampoDaRespostaDeveSerNumero(String campo, int esperado) {
        Object valor = resposta().jsonPath().get(campo);
        assertTrue(valor instanceof Number, "O campo '" + campo + "' deveria ser numérico, veio: " + valor);
        assertEquals(esperado, ((Number) valor).doubleValue(), 0.0001, "Valor inesperado no campo '" + campo + "'");
    }

    @Entao("o cabeçalho {string} deve estar presente")
    public void oCabecalhoDeveEstarPresente(String cabecalho) {
        String valor = resposta().getHeader(cabecalho);
        assertNotNull(valor, "O cabeçalho '" + cabecalho + "' não veio na resposta");
        assertFalse(valor.isBlank(), "O cabeçalho '" + cabecalho + "' veio vazio");
    }

    // ─── Respostas paginadas ──────────────────────────────────────────────────

    @Entao("todos os registros devem ter o campo {string} igual a {string}")
    public void todosOsRegistrosDevemTerOCampoIgualA(String campo, String esperado) {
        List<Object> valores = resposta().jsonPath().getList("Data." + campo);
        assertNotNull(valores, "A resposta deveria ter a lista 'Data'");
        assertFalse(valores.isEmpty(), "A lista 'Data' veio vazia — nada para conferir");
        for (Object valor : valores) {
            assertEquals(esperado, String.valueOf(valor), "Registro com '" + campo + "' fora do filtro");
        }
    }

    @Entao("a página deve conter no máximo {int} registros")
    public void aPaginaDeveConterNoMaximoRegistros(int maximo) {
        List<Object> registros = resposta().jsonPath().getList("Data");
        assertNotNull(registros, "A resposta deveria ter a lista 'Data'");
        assertTrue(registros.size() <= maximo,
                "A página trouxe " + registros.size() + " registros, acima do máximo de " + maximo);
    }

    @Entao("o total de registros deve ser {int}")
    public void oTotalDeRegistrosDeveSer(int total) {
        resposta().then().body("TotalRegistros", equalTo(total));
    }

    @Entao("a resposta deve indicar que existe uma próxima página")
    public void aRespostaDeveIndicarQueExisteUmaProximaPagina() {
        resposta().then().body("TemProxima", equalTo(true));
    }

    // ─── Desempenho (eficiência de recursos) ──────────────────────────────────

    @Entao("a resposta deve ser recebida em menos de {int} ms")
    public void aRespostaDeveSerRecebidaEmMenosDe(int milissegundos) {
        resposta().then().time(lessThan((long) milissegundos));
    }
}
