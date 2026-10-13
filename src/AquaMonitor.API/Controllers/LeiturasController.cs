using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquaMonitor.API.Data;
using AquaMonitor.API.Models;
using AquaMonitor.API.ViewModels;

namespace AquaMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LeiturasController : ControllerBase
{
    private readonly AppDbContext _context;

    public LeiturasController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Lista leituras com paginação e filtros</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultViewModel<LeituraResponseViewModel>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        [FromQuery] int? sensorId = null,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null,
        [FromQuery] bool? vazamento = null)
    {
        if (pagina < 1) pagina = 1;
        if (tamanhoPagina < 1 || tamanhoPagina > 100) tamanhoPagina = 10;

        var query = _context.Leituras
            .Include(l => l.Sensor)
            .AsQueryable();

        if (sensorId.HasValue)
            query = query.Where(l => l.SensorId == sensorId.Value);

        if (de.HasValue)
            query = query.Where(l => l.RegistradoEm >= de.Value);

        if (ate.HasValue)
            query = query.Where(l => l.RegistradoEm <= ate.Value);

        if (vazamento.HasValue)
            query = query.Where(l => l.VazamentoDetectado == vazamento.Value);

        var total = await query.CountAsync();

        var leituras = await query
            .OrderByDescending(l => l.RegistradoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(l => new LeituraResponseViewModel
            {
                Id = l.Id,
                SensorId = l.SensorId,
                SensorNome = l.Sensor!.Nome,
                ConsumoLitros = l.ConsumoLitros,
                PressaoBar = l.PressaoBar,
                TemperaturaCelsius = l.TemperaturaCelsius,
                VazamentoDetectado = l.VazamentoDetectado,
                RegistradoEm = l.RegistradoEm
            })
            .ToListAsync();

        return Ok(new PagedResultViewModel<LeituraResponseViewModel>
        {
            Data = leituras,
            TotalRegistros = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        });
    }

    /// <summary>Registra nova leitura e detecta anomalias automaticamente</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LeituraResponseViewModel), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create([FromBody] LeituraCreateViewModel vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var sensor = await _context.Sensores.FindAsync(vm.SensorId);
        if (sensor == null)
            return NotFound(new { mensagem = $"Sensor {vm.SensorId} não encontrado." });

        if (!sensor.Ativo)
            return BadRequest(new { mensagem = "Não é possível registrar leitura em sensor inativo." });

        // Detecta vazamento: pressão baixa + consumo acima da média
        bool vazamento = vm.PressaoBar.HasValue && vm.PressaoBar < 0.5 && vm.ConsumoLitros > 0;

        var leitura = new Leitura
        {
            SensorId = vm.SensorId,
            ConsumoLitros = vm.ConsumoLitros,
            PressaoBar = vm.PressaoBar,
            TemperaturaCelsius = vm.TemperaturaCelsius,
            VazamentoDetectado = vazamento
        };

        _context.Leituras.Add(leitura);

        // Gera alertas automáticos
        var alertas = new List<Alerta>();

        if (vazamento)
        {
            alertas.Add(new Alerta
            {
                SensorId = vm.SensorId,
                Tipo = "Vazamento",
                Descricao = $"Possível vazamento detectado no sensor '{sensor.Nome}'. Pressão: {vm.PressaoBar} bar.",
                Nivel = "CRITICO"
            });
        }

        if (vm.ConsumoLitros > sensor.LimiteConsumo)
        {
            var percentual = (vm.ConsumoLitros / sensor.LimiteConsumo * 100).ToString("F1");
            alertas.Add(new Alerta
            {
                SensorId = vm.SensorId,
                Tipo = "ConsumoExcessivo",
                Descricao = $"Consumo de {vm.ConsumoLitros}L excedeu o limite de {sensor.LimiteConsumo}L ({percentual}%).",
                Nivel = vm.ConsumoLitros > sensor.LimiteConsumo * 1.5 ? "ALTO" : "MEDIO"
            });
        }

        if (alertas.Any())
            _context.Alertas.AddRange(alertas);

        await _context.SaveChangesAsync();

        var response = new LeituraResponseViewModel
        {
            Id = leitura.Id,
            SensorId = leitura.SensorId,
            SensorNome = sensor.Nome,
            ConsumoLitros = leitura.ConsumoLitros,
            PressaoBar = leitura.PressaoBar,
            TemperaturaCelsius = leitura.TemperaturaCelsius,
            VazamentoDetectado = leitura.VazamentoDetectado,
            RegistradoEm = leitura.RegistradoEm
        };

        return CreatedAtAction(nameof(GetAll), new { sensorId = leitura.SensorId }, response);
    }

    /// <summary>Busca leitura por ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LeituraResponseViewModel), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var leitura = await _context.Leituras
            .Include(l => l.Sensor)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (leitura == null)
            return NotFound(new { mensagem = $"Leitura {id} não encontrada." });

        return Ok(new LeituraResponseViewModel
        {
            Id = leitura.Id,
            SensorId = leitura.SensorId,
            SensorNome = leitura.Sensor!.Nome,
            ConsumoLitros = leitura.ConsumoLitros,
            PressaoBar = leitura.PressaoBar,
            TemperaturaCelsius = leitura.TemperaturaCelsius,
            VazamentoDetectado = leitura.VazamentoDetectado,
            RegistradoEm = leitura.RegistradoEm
        });
    }

    /// <summary>Remove uma leitura</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var leitura = await _context.Leituras.FindAsync(id);
        if (leitura == null)
            return NotFound(new { mensagem = $"Leitura {id} não encontrada." });

        _context.Leituras.Remove(leitura);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
