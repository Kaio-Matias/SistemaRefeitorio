using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(ApiRefeicoesDbContext context, IConfiguration configuration)
        {
            _context = context;
            _tokenService = new TokenService(configuration);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            // CORREÇÃO: Busca por 'Username' em vez de 'Email'
            var usuario = await _context.Usuarios
                                        .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(model.Password, usuario.PasswordHash))
            {
                return Unauthorized(new { message = "Usuário ou senha inválidos." });
            }

            var token = _tokenService.GenerateToken(usuario);
            return Ok(new { token });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}