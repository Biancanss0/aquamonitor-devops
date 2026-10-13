using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquaMonitor.API.Data;
using AquaMonitor.API.ViewModels;

namespace AquaMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class RelatoriosController : ControllerBase
{
    private readonly AppDbContext _context;

    public RelatoriosController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Gera relatório geral de consumo hídrico por período (requer autenticação)</summary>
    [HttpGet("geral")]
    [ProducesResponseType(typeof(RelatorioGeralViewModel), 200)]
    public async Task<IActionResult> GetRelatorioGeral(
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null)
    {
        var inicio = de ?? DateTime.UtcNow.AddDays(-30);
        var fim = ate ?? DateTime.UtcNow;

        if (inicio > fim)
            return BadRequest(new { mensagem = "Data início não pode ser maior que data fim." });

        var sensores = await _context.Sensores
            .Where(s => s.Ativo)
            .ToListAsync();

        var leituras = await _context.Leituras
            .Where(l => l.RegistradoEm >= inicio && l.RegistradoEm <= fim)
            .ToListAsync();

        var alertas = await _context.Alertas
            .Where(a => a.CriadoEm >= inicio && a.CriadoEm <= fim)
            .ToListAsync();

        var detalhesPorSensor = sensores.Select(sensor =>
        {
            var leiturasDoSensor = leituras.Where(l => l.SensorId == sensor.Id).ToList();
            var alertasDoSensor = alertas.Where(a => a.SensorId == sensor.Id).ToList();
            var consumoTotal = leiturasDoSensor.Sum(l => l.ConsumoLitros);
            var percentual = sensor.LimiteConsumo > 0
                ? consumoTotal / sensor.LimiteConsumo * 100
                : 0;

            return new RelatorioSensorViewModel
            {
                SensorId = sensor.Id,
                SensorNome = sensor.Nome,
                Localizacao = sensor.Localizacao,
                Tipo = sensor.Tipo,
                LimiteConsumo = sensor.LimiteConsumo,
                ConsumoTotal = consumoTotal,
                ConsumoMedio = leiturasDoSensor.Any() ? leiturasDoSensor.Average(l => l.ConsumoLitros) : 0,
                ConsumoMaximo = leiturasDoSensor.Any() ? leiturasDoSensor.Max(l => l.ConsumoLitros) : 0,
                TotalLeituras = leiturasDoSensor.Count,
                TotalAlertas = alertasDoSensor.Count,
                AlertasNaoResolvidos = alertasDoSensor.Count(a => !a.Resolvido),
                PercentualDoLimite = Math.Round(percentual, 1),
                StatusConsumo = percentual <= 80 ? "Normal" : percentual <= 100 ? "Atenção" : "Critico"
            };
        }).ToList();

        var relatorio = new RelatorioGeralViewModel
        {
            PeriodoInicio = inicio,
            PeriodoFim = fim,
            TotalSensores = sensores.Count,
            ConsumoTotalLitros = leituras.Sum(l => l.ConsumoLitros),
            TotalAlertas = alertas.Count,
            AlertasNaoResolvidos = alertas.Count(a => !a.Resolvido),
            VazamentosDetectados = leituras.Count(l => l.VazamentoDetectado),
            DetalhesPorSensor = detalhesPorSensor
        };

        return Ok(relatorio);
    }

    /// <summary>Relatório de impacto hídrico por sensor (requer autenticação)</summary>
    [HttpGet("sensor/{sensorId}")]
    [ProducesResponseType(typeof(RelatorioSensorViewModel), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRelatorioSensor(
        int sensorId,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null)
    {
        var sensor = await _context.Sensores.FindAsync(sensorId);
        if (sensor == null)
            return NotFound(new { mensagem = $"Sensor {sensorId} não encontrado." });

        var inicio = de ?? DateTime.UtcNow.AddDays(-30);
        var fim = ate ?? DateTime.UtcNow;

        var leituras = await _context.Leituras
            .Where(l => l.SensorId == sensorId && l.RegistradoEm >= inicio && l.RegistradoEm <= fim)
            .ToListAsync();

        var alertas = await _context.Alertas
            .Where(a => a.SensorId == sensorId && a.CriadoEm >= inicio && a.CriadoEm <= fim)
            .ToListAsync();

        var consumoTotal = leituras.Sum(l => l.ConsumoLitros);
        var percentual = sensor.LimiteConsumo > 0 ? consumoTotal / sensor.LimiteConsumo * 100 : 0;

        var relatorio = new RelatorioSensorViewModel
        {
            SensorId = sensor.Id,
            SensorNome = sensor.Nome,
            Localizacao = sensor.Localizacao,
            Tipo = sensor.Tipo,
            LimiteConsumo = sensor.LimiteConsumo,
            ConsumoTotal = consumoTotal,
            ConsumoMedio = leituras.Any() ? leituras.Average(l => l.ConsumoLitros) : 0,
            ConsumoMaximo = leituras.Any() ? leituras.Max(l => l.ConsumoLitros) : 0,
            TotalLeituras = leituras.Count,
            TotalAlertas = alertas.Count,
            AlertasNaoResolvidos = alertas.Count(a => !a.Resolvido),
            PercentualDoLimite = Math.Round(percentual, 1),
            StatusConsumo = percentual <= 80 ? "Normal" : percentual <= 100 ? "Atenção" : "Critico"
        };

        return Ok(relatorio);
    }
}
