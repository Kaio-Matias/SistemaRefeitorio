using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
// using BCrypt.Net; // O 'using' é opcional se você usar o nome completo como abaixo

namespace ApiRefeicoes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public UsuariosController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }

        // POST: api/Usuarios
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario([FromForm] UsuarioDto usuarioDto)
        {
            var usuario = new Usuario
            {
                Nome = usuarioDto.Nome,
                Email = usuarioDto.Email,

                SenhaHash = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Senha),
                Role = usuarioDto.Role
            };

            if (usuarioDto.FotoPerfilFile != null && usuarioDto.FotoPerfilFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await usuarioDto.FotoPerfilFile.CopyToAsync(memoryStream);
            }

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsuario", new { id = usuario.Id }, usuario);
        }

        // PUT: api/Usuarios/5/foto
        [HttpPut("{id}/foto")]
        public async Task<IActionResult> PutUsuarioFoto(int id, IFormFile fotoFile)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            if (fotoFile == null || fotoFile.Length == 0)
            {
                return BadRequest("Nenhuma foto foi enviada.");
            }

            using var memoryStream = new MemoryStream();
            await fotoFile.CopyToAsync(memoryStream);

            _context.Entry(usuario).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Foto do usuário atualizada com sucesso." });
        }


        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }

    // DTO para receber os dados do formulário de usuário
    public class UsuarioDto
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string Role { get; set; }
        public IFormFile? FotoPerfilFile { get; set; }
    }
}