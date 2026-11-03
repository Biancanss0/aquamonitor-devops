package br.com.fiap.aquamonitor.bdd;

import org.junit.platform.suite.api.ConfigurationParameter;
import org.junit.platform.suite.api.IncludeEngines;
import org.junit.platform.suite.api.SelectClasspathResource;
import org.junit.platform.suite.api.Suite;

import static io.cucumber.junit.platform.engine.Constants.GLUE_PROPERTY_NAME;
import static io.cucumber.junit.platform.engine.Constants.PLUGIN_PROPERTY_NAME;

/**
 * Ponto de entrada da suíte BDD.
 *
 * <p>O Maven Surefire executa esta classe; a JUnit Platform entrega o controle ao motor do
 * Cucumber, que lê todos os arquivos {@code .feature} de {@code src/test/resources/features}
 * e procura os passos (steps) no pacote {@code br.com.fiap.aquamonitor.bdd}.</p>
 *
 * <p>Relatórios gerados em {@code target/cucumber-reports}:</p>
 * <ul>
 *   <li>{@code cucumber.html} – relatório visual (evidência de execução)</li>
 *   <li>{@code cucumber.json} – usado pelo pipeline para montar o resumo</li>
 *   <li>{@code cucumber.xml}  – formato JUnit, entendido por qualquer ferramenta de CI</li>
 * </ul>
 *
 * <p>O filtro de tags NÃO fica fixo aqui de propósito: assim ele pode ser passado na linha de
 * comando, ex.: {@code mvn test -Dcucumber.filter.tags="@sensores and not @admin"}.</p>
 */
@Suite
@IncludeEngines("cucumber")
@SelectClasspathResource("features")
@ConfigurationParameter(key = GLUE_PROPERTY_NAME, value = "br.com.fiap.aquamonitor.bdd")
@ConfigurationParameter(key = PLUGIN_PROPERTY_NAME, value =
        "pretty,"
        + " summary,"
        + " html:target/cucumber-reports/cucumber.html,"
        + " json:target/cucumber-reports/cucumber.json,"
        + " junit:target/cucumber-reports/cucumber.xml")
public class RunCucumberTest {
}
