using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Senha { get; set; }
        public string? DeviceIdentifier { get; set; } 
        public string? NomeDispositivo { get; set; } 
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var user = await _context.Usuarios.SingleOrDefaultAsync(u => u.Email == loginRequest.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Senha, user.SenhaHash))
            {
                return Unauthorized("Email ou senha inválidos.");
            }

            if (!string.IsNullOrEmpty(loginRequest.DeviceIdentifier))
            {
                var dispositivo = await _context.Dispositivos
                    .FirstOrDefaultAsync(d => d.UsuarioId == user.Id && d.DeviceIdentifier == loginRequest.DeviceIdentifier);

                if (dispositivo == null)
                {
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
                    dispositivo.UltimoLogin = DateTime.UtcNow;
                    dispositivo.IsAtivo = true;
                    _context.Dispositivos.Update(dispositivo);
                }
            }

            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user);

            return Ok(new
            {
                User = new { user.Email, user.Nome, user.Role },
                Token = token
            });
        }
    }
}