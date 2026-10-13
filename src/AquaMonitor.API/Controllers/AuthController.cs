using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquaMonitor.API.Data;
using AquaMonitor.API.Models;
using AquaMonitor.API.Services;
using AquaMonitor.API.ViewModels;

namespace AquaMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;

    public AuthController(AppDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    /// <summary>Registra um novo usuário</summary>
    [HttpPost("registrar")]
    [ProducesResponseType(typeof(AuthResponseViewModel), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Registrar([FromBody] RegistroViewModel vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == vm.Email);
        if (emailExiste)
            return BadRequest(new { mensagem = "E-mail já cadastrado." });

        var usuario = new Usuario
        {
            Nome = vm.Nome,
            Email = vm.Email,
            SenhaHash = _authService.HashSenha(vm.Senha)
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var token = _authService.GerarToken(usuario);
        var expiracao = DateTime.UtcNow.AddHours(8);

        return CreatedAtAction(nameof(Registrar), new AuthResponseViewModel
        {
            Token = token,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Perfil,
            Expiracao = expiracao
        });
    }

    /// <summary>Realiza login e retorna token JWT</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseViewModel), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginViewModel vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == vm.Email);
        if (usuario == null || !_authService.VerificarSenha(vm.Senha, usuario.SenhaHash))
            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });

        var token = _authService.GerarToken(usuario);
        var expiracao = DateTime.UtcNow.AddHours(8);

        return Ok(new AuthResponseViewModel
        {
            Token = token,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Perfil,
            Expiracao = expiracao
        });
    }
}
