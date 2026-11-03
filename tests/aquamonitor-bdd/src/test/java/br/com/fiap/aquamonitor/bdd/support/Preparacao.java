package br.com.fiap.aquamonitor.bdd.support;

import io.restassured.response.Response;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ThreadLocalRandom;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

/**
 * Operações de negócio reutilizadas pelos passos "Dado" (preparação de dados).
 *
 * <p>Os dados são criados PELA PRÓPRIA API (e não direto no banco): assim cada pré-condição
 * também exercita o sistema real, e os testes funcionam em qualquer ambiente (local, CI,
 * staging) sem depender de scripts SQL de carga.</p>
 *
 * <p>Os nomes e e-mails levam um sufixo único, então a suíte pode rodar várias vezes no mesmo
 * banco sem conflito de "e-mail já cadastrado".</p>
 */
public class Preparacao {

    public static final String SENHA_PADRAO = "AquaBdd2026";

    private final ApiClient api;
    private final TestContext ctx;

    public Preparacao(ApiClient api, TestContext ctx) {
        this.api = api;
        this.ctx = ctx;
    }

    // ─── Dados únicos ─────────────────────────────────────────────────────────

    public static String sufixoUnico() {
        return System.currentTimeMillis() + "" + ThreadLocalRandom.current().nextInt(100, 999);
    }

    public static String emailUnico() {
        return "bdd." + sufixoUnico() + "@aquamonitor.dev";
    }

    // ─── Usuários e autenticação ──────────────────────────────────────────────

    public Map<String, Object> novoUsuario(String senha) {
        Map<String, Object> usuario = new LinkedHashMap<>();
        usuario.put("Nome", "Usuario BDD " + sufixoUnico());
        usuario.put("Email", emailUnico());
        usuario.put("Senha", senha);
        return usuario;
    }

    /** Cadastra um usuário (perfil Operador) e devolve o token JWT retornado no cadastro. */
    public String cadastrarUsuario(String email, String senha) {
        Map<String, Object> usuario = novoUsuario(senha);
        usuario.put("Email", email);
        Response r = api.post("/api/Auth/registrar", usuario, null);
        assertEquals(201, r.getStatusCode(), "Pré-condição: cadastro de usuário deveria retornar 201");
        return r.jsonPath().getString("Token");
    }

    /** Token de um operador novo — usado em passos de apoio que precisam ler dados protegidos. */
    public String tokenApoio() {
        if (ctx.getTokenApoio() == null) {
            ctx.setTokenApoio(cadastrarUsuario(emailUnico(), SENHA_PADRAO));
        }
        return ctx.getTokenApoio();
    }

    public String loginAdministrador() {
        Map<String, Object> login = new LinkedHashMap<>();
        login.put("Email", Config.adminEmail());
        login.put("Senha", Config.adminSenha());
        Response r = api.post("/api/Auth/login", login, null);
        assertEquals(200, r.getStatusCode(),
                "Pré-condição: login do administrador (ADMIN_EMAIL/ADMIN_SENHA) deveria retornar 200");
        assertEquals("Admin", r.jsonPath().getString("Perfil"),
                "O usuário informado em ADMIN_EMAIL precisa ter perfil Admin (rode scripts/criar-admin.sh)");
        return r.jsonPath().getString("Token");
    }

    // ─── Sensores ─────────────────────────────────────────────────────────────

    public Map<String, Object> novoSensor(String tipo, double limiteLitros) {
        String sufixo = sufixoUnico();
        Map<String, Object> sensor = new LinkedHashMap<>();
        sensor.put("Nome", "Sensor BDD " + tipo + " " + sufixo);
        sensor.put("Localizacao", "Bloco BDD - Hidrometro " + sufixo);
        sensor.put("Tipo", tipo);
        sensor.put("LimiteConsumo", limiteLitros);
        return sensor;
    }

    /** Cria o sensor pela API e o registra para limpeza automática no fim do cenário. */
    public int criarSensor(String tipo, double limiteLitros) {
        Response r = api.post("/api/Sensores", novoSensor(tipo, limiteLitros), null);
        assertEquals(201, r.getStatusCode(), "Pré-condição: cadastro de sensor deveria retornar 201");
        int id = r.jsonPath().getInt("Id");
        ctx.getSensoresCriados().push(id);
        return id;
    }

    public void desativarSensor(int sensorId) {
        Response atual = api.get("/api/Sensores/" + sensorId);
        assertEquals(200, atual.getStatusCode(), "Pré-condição: sensor deveria existir");
        Map<String, Object> corpo = new LinkedHashMap<>();
        corpo.put("Nome", atual.jsonPath().getString("Nome"));
        corpo.put("Localizacao", atual.jsonPath().getString("Localizacao"));
        corpo.put("Tipo", atual.jsonPath().getString("Tipo"));
        corpo.put("LimiteConsumo", atual.jsonPath().getDouble("LimiteConsumo"));
        corpo.put("Ativo", false);
        Response r = api.put("/api/Sensores/" + sensorId, corpo, null);
        assertEquals(200, r.getStatusCode(), "Pré-condição: desativação do sensor deveria retornar 200");
    }

    // ─── Leituras ─────────────────────────────────────────────────────────────

    public Map<String, Object> novaLeitura(Integer sensorId, double consumo, Double pressao, Double temperatura) {
        Map<String, Object> leitura = new LinkedHashMap<>();
        leitura.put("SensorId", sensorId);
        leitura.put("ConsumoLitros", consumo);
        leitura.put("PressaoBar", pressao);
        leitura.put("TemperaturaCelsius", temperatura);
        return leitura;
    }

    public int registrarLeitura(int sensorId, double consumo, double pressao) {
        Response r = api.post("/api/Leituras", novaLeitura(sensorId, consumo, pressao, 22.0), null);
        assertEquals(201, r.getStatusCode(), "Pré-condição: registro de leitura deveria retornar 201");
        return r.jsonPath().getInt("Id");
    }

    // ─── Alertas ──────────────────────────────────────────────────────────────

    /** Lista (até 100) alertas do sensor usando um token de apoio. */
    public List<Map<String, Object>> alertasDoSensor(int sensorId) {
        Map<String, Object> query = new LinkedHashMap<>();
        query.put("sensorId", sensorId);
        query.put("tamanhoPagina", 100);
        Response r = api.get("/api/Alertas", tokenApoio(), query);
        assertEquals(200, r.getStatusCode(), "Consulta de apoio aos alertas deveria retornar 200");
        List<Map<String, Object>> alertas = r.jsonPath().getList("Data");
        assertNotNull(alertas);
        return alertas;
    }

    /** Gera um alerta CRÍTICO de vazamento (pressão abaixo de 0,5 bar) e devolve o id dele. */
    public int gerarAlertaDeVazamento(int sensorId) {
        registrarLeitura(sensorId, 100, 0.2);
        return alertasDoSensor(sensorId).stream()
                .filter(a -> "Vazamento".equals(a.get("Tipo")))
                .map(a -> ((Number) a.get("Id")).intValue())
                .findFirst()
                .orElseThrow(() -> new AssertionError("A API deveria ter gerado um alerta de Vazamento"));
    }

    // ─── Conversões usadas pelos passos ───────────────────────────────────────

    /** Lê números escritos tanto com ponto quanto com vírgula ("0.3" ou "0,3"). */
    public static double numero(String texto) {
        return Double.parseDouble(texto.trim().replace(',', '.'));
    }

    /** Converte o valor de um Exemplo do Gherkin para o tipo JSON correto do campo. */
    public static Object valorTipado(String campo, String valor) {
        switch (campo) {
            case "LimiteConsumo":
            case "ConsumoLitros":
            case "PressaoBar":
            case "TemperaturaCelsius":
                return numero(valor);
            case "SensorId":
                return Integer.parseInt(valor.trim());
            case "Ativo":
                return Boolean.parseBoolean(valor.trim());
            default:
                return valor;
        }
    }
}
