// ApiRefeicoes/Controllers/AuthController.cs

using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    // NOVO DTO (Data Transfer Object) para o request de login
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Senha { get; set; }
        public string DeviceIdentifier { get; set; }
        public string NomeDispositivo { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(ApiRefeicoesDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // MÉTODO DE LOGIN ATUALIZADO
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var user = await _context.Usuarios.SingleOrDefaultAsync(u => u.Email == loginRequest.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Senha, user.SenhaHash))
            {
                return Unauthorized("Email ou senha inválidos.");
            }

            // Lógica para registrar o dispositivo
            var dispositivo = await _context.Dispositivos
                .FirstOrDefaultAsync(d => d.UsuarioId == user.Id && d.DeviceIdentifier == loginRequest.DeviceIdentifier);

            if (dispositivo == null)
            {
                // Se o dispositivo não existe, cria um novo registro
                dispositivo = new Dispositivo
                {
                    UsuarioId = user.Id,
                    DeviceIdentifier = loginRequest.DeviceIdentifier,
                    Nome = loginRequest.NomeDispositivo,
                    UltimoLogin = DateTime.UtcNow,
                    IsAtivo = true
                };
                _context.Dispositivos.Add(dispositivo);
            }
            else
            {
                // Se já existe, apenas atualiza a data do último login e reativa
                dispositivo.UltimoLogin = DateTime.UtcNow;
                dispositivo.IsAtivo = true;
                _context.Dispositivos.Update(dispositivo);
            }

            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user);
            return Ok(new { token });
        }
    }
}