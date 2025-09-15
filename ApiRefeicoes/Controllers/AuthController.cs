using Microsoft.AspNetCore.Mvc;
using ApiRefeicoes.Data;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        // Modificamos o construtor para receber o ILogger
        public AuthController(ApiRefeicoesDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = new TokenService(configuration);
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation("Tentativa de login para o email: {Email}", loginDto.Email);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (usuario == null)
            {
                // Se este log aparecer, o email está errado ou não existe no banco
                _logger.LogWarning("FALHA NO LOGIN: Usuário com email '{Email}' NÃO ENCONTRADO no banco de dados.", loginDto.Email);
                return Unauthorized(new { message = "Email ou senha inválidos." });
            }

            // Verificamos a senha
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.SenhaHash);

            if (!isPasswordValid)
            {
                // Se este log aparecer, o email foi encontrado, mas a senha está errada
                _logger.LogWarning("FALHA NO LOGIN: Senha INCORRETA para o usuário '{Email}'.", loginDto.Email);
                return Unauthorized(new { message = "Email ou senha inválidos." });
            }

            _logger.LogInformation("Login bem-sucedido para o usuário '{Email}'. Gerando token.", loginDto.Email);
            var token = _tokenService.GenerateToken(usuario);

            return Ok(new
            {
                user = new { usuario.Email, usuario.Nome, usuario.Role },
                token
            });
        }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}