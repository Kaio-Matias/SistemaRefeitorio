using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiRefeicoes.Services;
using ApiRefeicoes.Models;
using ApiRefeicoes.Data;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApiRefeicoesDbContext _context;

        public AuthController(IConfiguration configuration, ApiRefeicoesDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            var user = _context.Usuarios.SingleOrDefault(u => u.Username == loginRequest.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Utilizador ou senha inválidos." });
            }

            // CORREÇÃO: A chamada ao GenerateToken deve passar o objeto de configuração inteiro,
            // pois o TokenService está desenhado para extrair os valores de dentro dele.
            var token = TokenService.GenerateToken(user, _configuration);

            return Ok(new { token });
        }

        [HttpGet("validate-token")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            return Ok();
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}