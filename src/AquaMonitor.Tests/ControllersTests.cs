using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using AquaMonitor.API.ViewModels;

namespace AquaMonitor.Tests;

public class SensoresControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SensoresControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/Sensores";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica status code 200
    }

    [Fact]
    public async Task GetById_SensorExistente_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/Sensores/1";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetById_SensorInexistente_Returns404()
    {
        // Arrange
        var request = "/api/Sensores/99999";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_SensorValido_Returns201()
    {
        // Arrange
        var novo = new SensorCreateViewModel
        {
            Nome = "Sensor Novo",
            Localizacao = "Bloco B",
            Tipo = "Residencial",
            LimiteConsumo = 300
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Sensores", novo);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_SensorSemNome_Returns400()
    {
        // Arrange
        var invalido = new { Localizacao = "Bloco C", Tipo = "Comercial", LimiteConsumo = 100 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Sensores", invalido);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class LeiturasControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LeiturasControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/Leituras";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica status code 200
    }

    [Fact]
    public async Task Get_ComFiltroSensorId_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/Leituras?sensorId=1&pagina=1&tamanhoPagina=5";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Post_LeituraValida_Returns201()
    {
        // Arrange
        var nova = new LeituraCreateViewModel
        {
            SensorId = 1,
            ConsumoLitros = 150,
            PressaoBar = 2.5,
            TemperaturaCelsius = 22
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Leituras", nova);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_LeituraSensorInexistente_Returns404()
    {
        // Arrange
        var nova = new LeituraCreateViewModel { SensorId = 99999, ConsumoLitros = 100 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Leituras", nova);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class AlertasControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private string? _token;

    public AlertasControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AutenticarAsync()
    {
        var login = new LoginViewModel { Email = "admin@teste.com", Senha = "123456" };
        var response = await _client.PostAsJsonAsync("/api/Auth/login", login);
        var content = await response.Content.ReadFromJsonAsync<AuthResponseViewModel>();
        _token = content?.Token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    [Fact]
    public async Task Get_SemToken_Returns401()
    {
        // Arrange
        var request = "/api/Alertas";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ComToken_ReturnsHttpStatusCode200()
    {
        // Arrange
        await AutenticarAsync();
        var request = "/api/Alertas";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica status code 200
    }
}

public class RelatoriosControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RelatoriosControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AutenticarAsync()
    {
        var login = new LoginViewModel { Email = "admin@teste.com", Senha = "123456" };
        var response = await _client.PostAsJsonAsync("/api/Auth/login", login);
        var content = await response.Content.ReadFromJsonAsync<AuthResponseViewModel>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", content?.Token);
    }

    [Fact]
    public async Task GetRelatorioGeral_ComToken_ReturnsHttpStatusCode200()
    {
        // Arrange
        await AutenticarAsync();
        var request = "/api/Relatorios/geral";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica status code 200
    }

    [Fact]
    public async Task GetRelatorioSensor_ComToken_ReturnsHttpStatusCode200()
    {
        // Arrange
        await AutenticarAsync();
        var request = "/api/Relatorios/sensor/1";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica status code 200
    }

    [Fact]
    public async Task GetRelatorioSensor_SemToken_Returns401()
    {
        // Arrange
        var request = "/api/Relatorios/sensor/1";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_CredenciaisValidas_ReturnsHttpStatusCode200()
    {
        // Arrange
        var login = new LoginViewModel { Email = "admin@teste.com", Senha = "123456" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", login);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica status code 200
    }

    [Fact]
    public async Task Login_CredenciaisInvalidas_Returns401()
    {
        // Arrange
        var login = new LoginViewModel { Email = "admin@teste.com", Senha = "senhaerrada" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", login);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Registrar_UsuarioNovo_Returns201()
    {
        // Arrange
        var registro = new RegistroViewModel
        {
            Nome = "Novo Usuario",
            Email = $"novo_{Guid.NewGuid()}@teste.com",
            Senha = "123456"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/registrar", registro);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
