using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquaMonitor.API.Data;
using AquaMonitor.API.ViewModels;

namespace AquaMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Requer autenticação JWT
public class AlertasController : ControllerBase
{
    private readonly AppDbContext _context;

    public AlertasController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Lista alertas com paginação (requer autenticação)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultViewModel<AlertaResponseViewModel>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        [FromQuery] int? sensorId = null,
        [FromQuery] string? nivel = null,
        [FromQuery] string? tipo = null,
        [FromQuery] bool? resolvido = null)
    {
        if (pagina < 1) pagina = 1;
        if (tamanhoPagina < 1 || tamanhoPagina > 100) tamanhoPagina = 10;

        var query = _context.Alertas
            .Include(a => a.Sensor)
            .AsQueryable();

        if (sensorId.HasValue)
            query = query.Where(a => a.SensorId == sensorId.Value);

        if (!string.IsNullOrEmpty(nivel))
            query = query.Where(a => a.Nivel == nivel.ToUpper());

        if (!string.IsNullOrEmpty(tipo))
            query = query.Where(a => a.Tipo == tipo);

        if (resolvido.HasValue)
            query = query.Where(a => a.Resolvido == resolvido.Value);

        var total = await query.CountAsync();

        var alertas = await query
            .OrderByDescending(a => a.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(a => new AlertaResponseViewModel
            {
                Id = a.Id,
                SensorId = a.SensorId,
                SensorNome = a.Sensor!.Nome,
                Tipo = a.Tipo,
                Descricao = a.Descricao,
                Nivel = a.Nivel,
                Resolvido = a.Resolvido,
                CriadoEm = a.CriadoEm,
                ResolvidoEm = a.ResolvidoEm
            })
            .ToListAsync();

        return Ok(new PagedResultViewModel<AlertaResponseViewModel>
        {
            Data = alertas,
            TotalRegistros = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        });
    }

    /// <summary>Busca alerta por ID (requer autenticação)</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AlertaResponseViewModel), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var alerta = await _context.Alertas
            .Include(a => a.Sensor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (alerta == null)
            return NotFound(new { mensagem = $"Alerta {id} não encontrado." });

        return Ok(new AlertaResponseViewModel
        {
            Id = alerta.Id,
            SensorId = alerta.SensorId,
            SensorNome = alerta.Sensor!.Nome,
            Tipo = alerta.Tipo,
            Descricao = alerta.Descricao,
            Nivel = alerta.Nivel,
            Resolvido = alerta.Resolvido,
            CriadoEm = alerta.CriadoEm,
            ResolvidoEm = alerta.ResolvidoEm
        });
    }

    /// <summary>Marca um alerta como resolvido (requer autenticação)</summary>
    [HttpPatch("{id}/resolver")]
    [ProducesResponseType(typeof(AlertaResponseViewModel), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Resolver(int id)
    {
        var alerta = await _context.Alertas
            .Include(a => a.Sensor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (alerta == null)
            return NotFound(new { mensagem = $"Alerta {id} não encontrado." });

        if (alerta.Resolvido)
            return BadRequest(new { mensagem = "Alerta já está resolvido." });

        alerta.Resolvido = true;
        alerta.ResolvidoEm = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new AlertaResponseViewModel
        {
            Id = alerta.Id,
            SensorId = alerta.SensorId,
            SensorNome = alerta.Sensor!.Nome,
            Tipo = alerta.Tipo,
            Descricao = alerta.Descricao,
            Nivel = alerta.Nivel,
            Resolvido = alerta.Resolvido,
            CriadoEm = alerta.CriadoEm,
            ResolvidoEm = alerta.ResolvidoEm
        });
    }

    /// <summary>Remove um alerta (somente Admin)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var alerta = await _context.Alertas.FindAsync(id);
        if (alerta == null)
            return NotFound(new { mensagem = $"Alerta {id} não encontrado." });

        _context.Alertas.Remove(alerta);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
