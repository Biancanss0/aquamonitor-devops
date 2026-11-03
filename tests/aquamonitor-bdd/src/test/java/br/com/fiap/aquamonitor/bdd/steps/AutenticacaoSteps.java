package br.com.fiap.aquamonitor.bdd.steps;

import br.com.fiap.aquamonitor.bdd.support.ApiClient;
import br.com.fiap.aquamonitor.bdd.support.Preparacao;
import br.com.fiap.aquamonitor.bdd.support.TestContext;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.cucumber.java.pt.Dado;
import io.cucumber.java.pt.Entao;
import io.cucumber.java.pt.Quando;
import io.restassured.response.Response;

import java.io.IOException;
import java.time.Instant;
import java.util.Base64;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.Map;

import static org.hamcrest.Matchers.equalTo;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

/** Passos de /api/Auth (cadastro e login) e de "estar autenticado". */
public class AutenticacaoSteps {

    private final TestContext ctx;
    private final ApiClient api;
    private final Preparacao preparacao;

    public AutenticacaoSteps(TestContext ctx, ApiClient api, Preparacao preparacao) {
        this.ctx = ctx;
        this.api = api;
        this.preparacao = preparacao;
    }

    // ─── Cadastro ─────────────────────────────────────────────────────────────

    @Dado("que eu tenho os dados de um novo usuário com senha {string}")
    public void queEuTenhoOsDadosDeUmNovoUsuario(String senha) {
        ctx.getPayload().clear();
        ctx.getPayload().putAll(preparacao.novoUsuario(senha));
    }

    @Dado("o campo {string} do cadastro recebe o valor {string}")
    public void oCampoDoCadastroRecebeOValor(String campo, String valor) {
        ctx.getPayload().put(campo, valor);
    }

    @Quando("eu envio a requisição de cadastro de usuário")
    public void euEnvioARequisicaoDeCadastro() {
        ctx.setResposta(api.post("/api/Auth/registrar", ctx.getPayload(), null));
    }

    @Dado("que existe um usuário cadastrado na plataforma")
    public void queExisteUmUsuarioCadastrado() {
        ctx.setEmailUsuario(Preparacao.emailUnico());
        ctx.setSenhaUsuario(Preparacao.SENHA_PADRAO);
        preparacao.cadastrarUsuario(ctx.getEmailUsuario(), ctx.getSenhaUsuario());
    }

    @Quando("eu tento cadastrar outro usuário com o mesmo e-mail")
    public void euTentoCadastrarOutroUsuarioComOMesmoEmail() {
        Map<String, Object> outro = preparacao.novoUsuario("OutraSenha123");
        outro.put("Email", ctx.getEmailUsuario());
        ctx.setResposta(api.post("/api/Auth/registrar", outro, null));
    }

    @Entao("o perfil do usuário retornado deve ser {string}")
    public void oPerfilDoUsuarioRetornadoDeveSer(String perfil) {
        ctx.getResposta().then().body("Perfil", equalTo(perfil));
    }

    /**
     * Não basta "ter um token": decodificamos o payload do JWT e conferimos que ele expira no
     * futuro e que carrega o mesmo e-mail e o mesmo perfil devolvidos no corpo da resposta.
     */
    @Entao("a resposta deve conter um token JWT válido")
    public void aRespostaDeveConterUmTokenJwtValido() throws IOException {
        Response r = ctx.getResposta();
        String token = r.jsonPath().getString("Token");
        assertNotNull(token, "A resposta não trouxe o campo Token");

        String[] partes = token.split("\\.");
        assertEquals(3, partes.length, "Um JWT tem 3 partes (header.payload.assinatura)");

        JsonNode payload = new ObjectMapper().readTree(Base64.getUrlDecoder().decode(partes[1]));
        assertTrue(payload.path("exp").asLong() > Instant.now().getEpochSecond(), "O token já nasceu expirado");
        assertEquals(r.jsonPath().getString("Email"), claim(payload, "email"), "E-mail do token difere do corpo");
        assertEquals(r.jsonPath().getString("Perfil"), claim(payload, "role"), "Perfil do token difere do corpo");
    }

    /** Aceita tanto o nome curto da claim ("role") quanto o URI longo (".../claims/role"). */
    private static String claim(JsonNode payload, String nome) {
        Iterator<Map.Entry<String, JsonNode>> campos = payload.fields();
        while (campos.hasNext()) {
            Map.Entry<String, JsonNode> campo = campos.next();
            String chave = campo.getKey().toLowerCase();
            if (chave.equals(nome) || chave.endsWith("/" + nome) || chave.endsWith("/" + nome + "address")) {
                return campo.getValue().asText();
            }
        }
        return null;
    }

    // ─── Login ────────────────────────────────────────────────────────────────

    @Quando("eu faço login com o e-mail e a senha corretos")
    public void euFacoLoginComOEmailEASenhaCorretos() {
        login(ctx.getEmailUsuario(), ctx.getSenhaUsuario());
    }

    @Quando("eu faço login com a senha {string}")
    public void euFacoLoginComASenha(String senha) {
        login(ctx.getEmailUsuario(), senha);
    }

    @Quando("eu faço login com o e-mail {string} e a senha {string}")
    public void euFacoLoginComOEmailEASenha(String email, String senha) {
        login(email, senha);
    }

    private void login(String email, String senha) {
        Map<String, Object> corpo = new LinkedHashMap<>();
        corpo.put("Email", email);
        corpo.put("Senha", senha);
        ctx.setResposta(api.post("/api/Auth/login", corpo, null));
    }

    // ─── Sessão autenticada ───────────────────────────────────────────────────

    @Dado("que eu estou autenticado como operador")
    public void queEuEstouAutenticadoComoOperador() {
        ctx.setTokenAtual(preparacao.cadastrarUsuario(Preparacao.emailUnico(), Preparacao.SENHA_PADRAO));
    }

    @Dado("que eu estou autenticado como administrador")
    public void queEuEstouAutenticadoComoAdministrador() {
        ctx.setTokenAtual(preparacao.loginAdministrador());
    }
}
