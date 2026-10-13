using Microsoft.Extensions.Diagnostics.HealthChecks;
using AquaMonitor.API.Data;

namespace AquaMonitor.API.Health;

/// <summary>
/// Verifica se a API consegue abrir conexão com o banco de dados.
/// Usado pelo endpoint /health, pelo HEALTHCHECK do Docker e pelos smoke tests do pipeline.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public DatabaseHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var conectado = await _context.Database.CanConnectAsync(cancellationToken);
            return conectado
                ? HealthCheckResult.Healthy("Banco de dados acessível")
                : HealthCheckResult.Unhealthy("Não foi possível conectar ao banco de dados");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao conectar ao banco de dados", ex);
        }
    }
}
