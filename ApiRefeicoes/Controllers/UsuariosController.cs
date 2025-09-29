using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using BCrypt.Net;
using ApiRefeicoes.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public UsuariosController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin")] // Apenas SuperAdmins podem listar usuários
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new UsuarioResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // Permite a criação do primeiro SuperAdmin sem autenticação.
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<UsuarioResponseDto>> PostUsuario(CreateUsuarioDto usuarioDto)
        {
            // Medida de Segurança: Bloqueia a criação se já existir algum usuário
            if (await _context.Usuarios.AnyAsync())
            {
                // Verifica se o usuário autenticado é um SuperAdmin para permitir a criação de outros usuários
                if (!User.Identity.IsAuthenticated || !User.IsInRole("SuperAdmin"))
                {
                    return Forbid("A criação de novos usuários só é permitida a SuperAdmins.");
                }
            }

            if (await _context.Usuarios.AnyAsync(u => u.Username == usuarioDto.Username))
            {
                return BadRequest("Nome de usuário já existe.");
            }

            var usuario = new Usuario
            {
                Username = usuarioDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Password),
                Role = usuarioDto.Role
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var responseDto = new UsuarioResponseDto
            {
                Id = usuario.Id,
                Username = usuario.Username,
                Role = usuario.Role
            };

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, responseDto);
        }

        [Authorize] // Qualquer usuário autenticado pode ver seu próprio perfil (exemplo)
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioResponseDto>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            var responseDto = new UsuarioResponseDto
            {
                Id = usuario.Id,
                Username = usuario.Username,
                Role = usuario.Role
            };

            return Ok(responseDto);
        }
    }
}