using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquaMonitor.API.Data;
using AquaMonitor.API.Models;
using AquaMonitor.API.ViewModels;

namespace AquaMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SensoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public SensoresController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Lista todos os sensores com paginação</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultViewModel<SensorResponseViewModel>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        [FromQuery] string? tipo = null,
        [FromQuery] bool? ativo = null)
    {
        if (pagina < 1) pagina = 1;
        if (tamanhoPagina < 1 || tamanhoPagina > 100) tamanhoPagina = 10;

        var query = _context.Sensores.AsQueryable();

        if (!string.IsNullOrEmpty(tipo))
            query = query.Where(s => s.Tipo == tipo);

        if (ativo.HasValue)
            query = query.Where(s => s.Ativo == ativo.Value);

        var total = await query.CountAsync();

        var sensores = await query
            .OrderBy(s => s.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(s => new SensorResponseViewModel
            {
                Id = s.Id,
                Nome = s.Nome,
                Localizacao = s.Localizacao,
                Tipo = s.Tipo,
                LimiteConsumo = s.LimiteConsumo,
                Ativo = s.Ativo,
                CriadoEm = s.CriadoEm
            })
            .ToListAsync();

        return Ok(new PagedResultViewModel<SensorResponseViewModel>
        {
            Data = sensores,
            TotalRegistros = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        });
    }

    /// <summary>Busca sensor por ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SensorResponseViewModel), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var sensor = await _context.Sensores.FindAsync(id);
        if (sensor == null)
            return NotFound(new { mensagem = $"Sensor {id} não encontrado." });

        return Ok(new SensorResponseViewModel
        {
            Id = sensor.Id,
            Nome = sensor.Nome,
            Localizacao = sensor.Localizacao,
            Tipo = sensor.Tipo,
            LimiteConsumo = sensor.LimiteConsumo,
            Ativo = sensor.Ativo,
            CriadoEm = sensor.CriadoEm
        });
    }

    /// <summary>Cadastra um novo sensor</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SensorResponseViewModel), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] SensorCreateViewModel vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var sensor = new Sensor
        {
            Nome = vm.Nome,
            Localizacao = vm.Localizacao,
            Tipo = vm.Tipo,
            LimiteConsumo = vm.LimiteConsumo
        };

        _context.Sensores.Add(sensor);
        await _context.SaveChangesAsync();

        var response = new SensorResponseViewModel
        {
            Id = sensor.Id,
            Nome = sensor.Nome,
            Localizacao = sensor.Localizacao,
            Tipo = sensor.Tipo,
            LimiteConsumo = sensor.LimiteConsumo,
            Ativo = sensor.Ativo,
            CriadoEm = sensor.CriadoEm
        };

        return CreatedAtAction(nameof(GetById), new { id = sensor.Id }, response);
    }

    /// <summary>Atualiza um sensor existente</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SensorResponseViewModel), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] SensorUpdateViewModel vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var sensor = await _context.Sensores.FindAsync(id);
        if (sensor == null)
            return NotFound(new { mensagem = $"Sensor {id} não encontrado." });

        sensor.Nome = vm.Nome;
        sensor.Localizacao = vm.Localizacao;
        sensor.Tipo = vm.Tipo;
        sensor.LimiteConsumo = vm.LimiteConsumo;
        sensor.Ativo = vm.Ativo;

        await _context.SaveChangesAsync();

        return Ok(new SensorResponseViewModel
        {
            Id = sensor.Id,
            Nome = sensor.Nome,
            Localizacao = sensor.Localizacao,
            Tipo = sensor.Tipo,
            LimiteConsumo = sensor.LimiteConsumo,
            Ativo = sensor.Ativo,
            CriadoEm = sensor.CriadoEm
        });
    }

    /// <summary>Remove um sensor</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var sensor = await _context.Sensores.FindAsync(id);
        if (sensor == null)
            return NotFound(new { mensagem = $"Sensor {id} não encontrado." });

        _context.Sensores.Remove(sensor);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
