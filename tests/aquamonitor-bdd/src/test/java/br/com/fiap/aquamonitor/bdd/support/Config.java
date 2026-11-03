package br.com.fiap.aquamonitor.bdd.support;

/**
 * Configuração da suíte, lida de propriedades -D (prioridade) ou de variáveis de ambiente.
 *
 * <pre>
 *  Propriedade -D     Variável de ambiente   Padrão
 *  api.baseUrl        API_BASE_URL           http://localhost:8080
 *  admin.email        ADMIN_EMAIL            (vazio → cenários @admin são pulados)
 *  admin.senha        ADMIN_SENHA            (vazio → cenários @admin são pulados)
 * </pre>
 *
 * Nenhuma senha fica no código: em CI elas chegam por variável de ambiente (boa prática de
 * governança/segurança).
 */
public final class Config {

    private Config() {
    }

    public static String baseUrl() {
        String url = valor("api.baseUrl", "API_BASE_URL", "http://localhost:8080");
        return url.endsWith("/") ? url.substring(0, url.length() - 1) : url;
    }

    public static String adminEmail() {
        return valor("admin.email", "ADMIN_EMAIL", null);
    }

    public static String adminSenha() {
        return valor("admin.senha", "ADMIN_SENHA", null);
    }

    public static boolean adminConfigurado() {
        return adminEmail() != null && adminSenha() != null;
    }

    private static String valor(String propriedade, String variavelAmbiente, String padrao) {
        String v = System.getProperty(propriedade);
        if (v == null || v.isBlank()) {
            v = System.getenv(variavelAmbiente);
        }
        return (v == null || v.isBlank()) ? padrao : v.trim();
    }
}
