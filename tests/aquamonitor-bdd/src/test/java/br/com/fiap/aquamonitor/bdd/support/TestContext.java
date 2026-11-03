package br.com.fiap.aquamonitor.bdd.support;

import io.restassured.response.Response;

import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Deque;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * Estado compartilhado entre as classes de steps durante UM cenário.
 *
 * <p>O PicoContainer cria uma instância nova a cada cenário e a injeta no construtor de cada
 * classe de steps. Assim o "Dado" (em SensoresSteps) e o "Então" (em ComumSteps) enxergam a
 * mesma resposta, sem variáveis estáticas — e um cenário nunca contamina o outro.</p>
 */
public class TestContext {

    /** Resposta da ação principal do cenário (o "Quando"), validada pelos passos "Então". */
    private Response resposta;

    /** Corpo que está sendo montado para a próxima requisição. */
    private final Map<String, Object> payload = new LinkedHashMap<>();

    /** Histórico de todas as chamadas HTTP do cenário (anexado ao relatório como evidência). */
    private final List<String> historicoChamadas = new ArrayList<>();

    /** Sensores criados pelo cenário, removidos no final (o banco fica limpo para o próximo). */
    private final Deque<Integer> sensoresCriados = new ArrayDeque<>();

    // Token usado nas requisições autenticadas do cenário ("estou autenticado como ...")
    private String tokenAtual;
    // Token de apoio para os passos de preparação/verificação que precisam ler alertas
    private String tokenApoio;

    // Usuário criado no cenário (cenários de autenticação)
    private String emailUsuario;
    private String senhaUsuario;

    // Recursos "em foco" no cenário
    private Integer sensorId;
    private Integer leituraId;
    private Integer alertaId;

    public Response getResposta() {
        return resposta;
    }

    public void setResposta(Response resposta) {
        this.resposta = resposta;
    }

    public Map<String, Object> getPayload() {
        return payload;
    }

    public List<String> getHistoricoChamadas() {
        return historicoChamadas;
    }

    public void registrarChamada(String linha) {
        historicoChamadas.add(linha);
    }

    public Deque<Integer> getSensoresCriados() {
        return sensoresCriados;
    }

    public String getTokenAtual() {
        return tokenAtual;
    }

    public void setTokenAtual(String tokenAtual) {
        this.tokenAtual = tokenAtual;
    }

    public String getTokenApoio() {
        return tokenApoio;
    }

    public void setTokenApoio(String tokenApoio) {
        this.tokenApoio = tokenApoio;
    }

    public String getEmailUsuario() {
        return emailUsuario;
    }

    public void setEmailUsuario(String emailUsuario) {
        this.emailUsuario = emailUsuario;
    }

    public String getSenhaUsuario() {
        return senhaUsuario;
    }

    public void setSenhaUsuario(String senhaUsuario) {
        this.senhaUsuario = senhaUsuario;
    }

    public Integer getSensorId() {
        return sensorId;
    }

    public void setSensorId(Integer sensorId) {
        this.sensorId = sensorId;
    }

    public Integer getLeituraId() {
        return leituraId;
    }

    public void setLeituraId(Integer leituraId) {
        this.leituraId = leituraId;
    }

    public Integer getAlertaId() {
        return alertaId;
    }

    public void setAlertaId(Integer alertaId) {
        this.alertaId = alertaId;
    }
}
