using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;

namespace ApiRefeicoes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FuncoesController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public FuncoesController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        // GET: api/Funcoes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Funcao>>> GetFuncoes()
        {
            return await _context.Funcoes.ToListAsync();
        }

        // GET: api/Funcoes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Funcao>> GetFuncao(int id)
        {
            var funcao = await _context.Funcoes.FindAsync(id);

            if (funcao == null)
            {
                return NotFound();
            }

            return funcao;
        }

        // POST: api/Funcoes
        [HttpPost]
        public async Task<ActionResult<Funcao>> PostFuncao(Funcao funcao)
        {
            _context.Funcoes.Add(funcao);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFuncao", new { id = funcao.Id }, funcao);
        }

        // PUT: api/Funcoes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFuncao(int id, Funcao funcao)
        {
            if (id != funcao.Id)
            {
                return BadRequest();
            }

            _context.Entry(funcao).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FuncaoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Funcoes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuncao(int id)
        {
            var funcao = await _context.Funcoes.FindAsync(id);
            if (funcao == null)
            {
                return NotFound();
            }

            _context.Funcoes.Remove(funcao);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FuncaoExists(int id)
        {
            return _context.Funcoes.Any(e => e.Id == id);
        }
    }
}