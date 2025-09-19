// ApiRefeicoes/Controllers/DispositivosController.cs

using ApiRefeicoes.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    [Authorize] // Protege o controller, exigindo um token válido
    [Route("api/[controller]")]
    [ApiController]
    public class DispositivosController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public DispositivosController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        // GET: api/dispositivos
        // Retorna a lista de dispositivos ativos para o usuário logado
        [HttpGet]
        public async Task<IActionResult> GetMeusDispositivos()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var dispositivos = await _context.Dispositivos
                .Where(d => d.UsuarioId == userId && d.IsAtivo)
                .Select(d => new { d.Id, d.Nome, d.UltimoLogin }) // Retorna apenas dados seguros
                .ToListAsync();

            return Ok(dispositivos);
        }

        // POST: api/dispositivos/logout/5
        // Desconecta um dispositivo específico (logout remoto)
        [HttpPost("logout/{dispositivoId}")]
        public async Task<IActionResult> LogoutDispositivo(int dispositivoId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var dispositivo = await _context.Dispositivos
                .FirstOrDefaultAsync(d => d.Id == dispositivoId && d.UsuarioId == userId);

            if (dispositivo == null)
            {
                return NotFound("Dispositivo não encontrado ou não pertence a este usuário.");
            }

            dispositivo.IsAtivo = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Dispositivo '{dispositivo.Nome}' desconectado com sucesso." });
        }
    }
}