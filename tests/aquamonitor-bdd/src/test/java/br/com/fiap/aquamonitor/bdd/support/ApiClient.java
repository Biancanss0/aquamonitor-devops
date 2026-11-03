package br.com.fiap.aquamonitor.bdd.support;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.restassured.RestAssured;
import io.restassured.config.DecoderConfig;
import io.restassured.config.EncoderConfig;
import io.restassured.config.JsonConfig;
import io.restassured.config.RestAssuredConfig;
import io.restassured.filter.log.LogDetail;
import io.restassured.http.ContentType;
import io.restassured.path.json.config.JsonPathConfig;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;

import java.util.Map;
import java.util.stream.Collectors;

/**
 * Cliente HTTP fino sobre o RestAssured.
 *
 * <p>Todas as requisições da suíte passam por aqui, o que garante três coisas:</p>
 * <ol>
 *   <li>mesma configuração em todo lugar (URL base, JSON, UTF-8);</li>
 *   <li>log completo da requisição/resposta no console quando uma validação falha;</li>
 *   <li>cada chamada é anotada no {@link TestContext} e vira evidência no relatório HTML.</li>
 * </ol>
 */
public class ApiClient {

    private static final ObjectMapper JSON = new ObjectMapper();
    private static final int LIMITE_LOG_RESPOSTA = 600;

    static {
        RestAssured.config = RestAssuredConfig.config()
                .encoderConfig(EncoderConfig.encoderConfig().defaultContentCharset("UTF-8"))
                .decoderConfig(DecoderConfig.decoderConfig().defaultContentCharset("UTF-8"))
                // números decimais do JSON lidos como Double (e não Float) → comparações exatas
                .jsonConfig(JsonConfig.jsonConfig().numberReturnType(JsonPathConfig.NumberReturnType.DOUBLE));
    }

    private final TestContext ctx;

    public ApiClient(TestContext ctx) {
        this.ctx = ctx;
    }

    // ─── Atalhos ──────────────────────────────────────────────────────────────

    public Response get(String caminho) {
        return enviar("GET", caminho, null, null, null);
    }

    public Response get(String caminho, String token, Map<String, ?> query) {
        return enviar("GET", caminho, null, token, query);
    }

    public Response post(String caminho, Object corpo, String token) {
        return enviar("POST", caminho, corpo, token, null);
    }

    public Response put(String caminho, Object corpo, String token) {
        return enviar("PUT", caminho, corpo, token, null);
    }

    public Response patch(String caminho, String token) {
        return enviar("PATCH", caminho, null, token, null);
    }

    public Response delete(String caminho, String token) {
        return enviar("DELETE", caminho, null, token, null);
    }

    // ─── Núcleo ───────────────────────────────────────────────────────────────

    public Response enviar(String metodo, String caminho, Object corpo, String token, Map<String, ?> query) {
        RequestSpecification req = RestAssured.given()
                .baseUri(Config.baseUrl())
                .accept(ContentType.JSON)
                .log().ifValidationFails(LogDetail.ALL);

        if (corpo != null) {
            req.contentType(ContentType.JSON).body(corpo);
        }
        if (token != null) {
            req.header("Authorization", "Bearer " + token);
        }
        if (query != null && !query.isEmpty()) {
            req.queryParams(query);
        }

        Response resposta = req.request(metodo, caminho);
        registrar(metodo, caminho, corpo, token, query, resposta);
        return resposta;
    }

    private void registrar(String metodo, String caminho, Object corpo, String token,
                           Map<String, ?> query, Response resposta) {
        String queryString = (query == null || query.isEmpty()) ? "" : "?" + query.entrySet().stream()
                .map(e -> e.getKey() + "=" + e.getValue())
                .collect(Collectors.joining("&"));

        StringBuilder linha = new StringBuilder()
                .append(metodo).append(' ').append(caminho).append(queryString)
                .append(" → ").append(resposta.getStatusCode())
                .append(" (").append(resposta.getTime()).append(" ms)")
                .append(token != null ? " [autenticado]" : "");

        if (corpo != null) {
            linha.append("\n    requisição: ").append(paraJson(corpo));
        }
        String corpoResposta = resposta.asString();
        if (corpoResposta != null && !corpoResposta.isBlank()) {
            if (corpoResposta.length() > LIMITE_LOG_RESPOSTA) {
                corpoResposta = corpoResposta.substring(0, LIMITE_LOG_RESPOSTA) + "…";
            }
            linha.append("\n    resposta:   ").append(corpoResposta);
        }
        ctx.registrarChamada(linha.toString());
    }

    public static String paraJson(Object objeto) {
        try {
            return JSON.writeValueAsString(objeto);
        } catch (JsonProcessingException e) {
            return String.valueOf(objeto);
        }
    }
}
