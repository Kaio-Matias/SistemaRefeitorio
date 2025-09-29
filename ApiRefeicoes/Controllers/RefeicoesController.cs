using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;

namespace ApiRefeicoes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefeicoesController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public RefeicoesController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        // GET: api/Refeicoes
        // Retorna os registros incluindo os dados do colaborador
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RegistroRefeicao>>> GetRegistroRefeicoes()
        {
            return await _context.RegistroRefeicoes
                                 .Include(r => r.Colaborador) // Inclui o objeto Colaborador
                                 .ThenInclude(c => c.Funcao) // Opcional: inclui a Função do Colaborador
                                 .Include(r => r.Colaborador) // Inclui o objeto Colaborador novamente
                                 .ThenInclude(c => c.Departamento) // Opcional: inclui o Departamento do Colaborador
                                 .ToListAsync();
        }

        // GET: api/Refeicoes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RegistroRefeicao>> GetRegistroRefeicao(int id)
        {
            var registroRefeicao = await _context.RegistroRefeicoes
                                                 .Include(r => r.Colaborador)
                                                 .ThenInclude(c => c.Funcao)
                                                 .Include(r => r.Colaborador)
                                                 .ThenInclude(c => c.Departamento)
                                                 .FirstOrDefaultAsync(r => r.Id == id);

            if (registroRefeicao == null)
            {
                return NotFound();
            }

            return registroRefeicao;
        }

        // POST: api/Refeicoes
        [HttpPost]
        public async Task<ActionResult<RegistroRefeicao>> PostRegistroRefeicao(RegistroRefeicao registroRefeicao)
        {
            var colaborador = await _context.Colaboradores.FindAsync(registroRefeicao.ColaboradorId);
            if (colaborador == null)
            {
                return BadRequest("Colaborador não encontrado.");
            }

            // Define a data e hora do registro para o momento atual
            registroRefeicao.DataHoraRegistro = DateTime.Now;

            _context.RegistroRefeicoes.Add(registroRefeicao);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRegistroRefeicao", new { id = registroRefeicao.Id }, registroRefeicao);
        }

        // DELETE: api/Refeicoes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegistroRefeicao(int id)
        {
            var registroRefeicao = await _context.RegistroRefeicoes.FindAsync(id);
            if (registroRefeicao == null)
            {
                return NotFound();
            }

            _context.RegistroRefeicoes.Remove(registroRefeicao);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}