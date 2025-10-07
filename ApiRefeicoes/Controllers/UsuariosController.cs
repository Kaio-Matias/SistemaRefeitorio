using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace ApiRefeicoes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class UsuariosController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public UsuariosController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios()
        {
            return await _context.Usuarios
                .Select(u => new UsuarioResponseDto { Id = u.Id, Username = u.Username, Role = u.Role })
                .ToListAsync();
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioResponseDto>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Select(u => new UsuarioResponseDto { Id = u.Id, Username = u.Username, Role = u.Role })
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }

        // --- MÉTODO NOVO ADICIONADO ---
        // PUT: api/Usuarios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, UpdateUsuarioDto updateUsuarioDto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            // Verifica se o novo nome de utilizador já está em uso por outro utilizador
            if (await _context.Usuarios.AnyAsync(u => u.Username == updateUsuarioDto.Username && u.Id != id))
            {
                return BadRequest("O nome de usuário já está em uso.");
            }

            usuario.Username = updateUsuarioDto.Username;
            usuario.Role = updateUsuarioDto.Role;

            // Altera a senha apenas se uma nova for fornecida
            if (!string.IsNullOrEmpty(updateUsuarioDto.Password))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUsuarioDto.Password);
            }

            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Usuarios.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Retorna 204 No Content em caso de sucesso
        }

        // POST: api/Usuarios
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(CreateUsuarioDto createUsuarioDto)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Username == createUsuarioDto.Username))
            {
                return BadRequest("O nome de usuário já está em uso.");
            }

            var usuario = new Usuario
            {
                Username = createUsuarioDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUsuarioDto.Password),
                Role = createUsuarioDto.Role,
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
    }
}