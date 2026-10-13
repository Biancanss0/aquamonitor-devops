using System.Net;
using System.Text.Json;
using Xunit;

namespace AquaMonitor.Tests;

/// <summary>
/// Garante que o endpoint /health (usado pelo Docker e pelos smoke tests de deploy)
/// responde 200 e informa status, ambiente e versão.
/// </summary>
public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHttpStatusCode200_ComStatusHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
        Assert.True(doc.RootElement.TryGetProperty("ambiente", out _));
        Assert.True(doc.RootElement.TryGetProperty("versao", out _));
    }
}
