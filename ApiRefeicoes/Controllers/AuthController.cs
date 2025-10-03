using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Adicionar este using
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;
        private readonly IConfiguration _configuration; // Adicionar IConfiguration

        // Injetar IConfiguration no construtor
        public AuthController(ApiRefeicoesDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null)
            {
                return BadRequest("Requisição inválida.");
            }

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == loginRequest.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            {
                return Unauthorized("Usuário ou senha inválidos.");
            }

            // Passar a _configuration para o GenerateToken
            var token = TokenService.GenerateToken(user, _configuration);

            return Ok(new { token });
        }
    }
}