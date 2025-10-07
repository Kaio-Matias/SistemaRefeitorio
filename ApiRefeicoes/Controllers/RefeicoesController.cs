using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiRefeicoes.Dtos; // Importar a pasta dos DTOs
using Microsoft.AspNetCore.Authorization; // Importar para segurança
using System;

namespace ApiRefeicoes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Protege todos os endpoints deste controlador
    public class RefeicoesController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public RefeicoesController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        // GET: api/Refeicoes
        // Retorna uma lista de refeições usando um DTO para evitar referências circulares
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RefeicaoResponseDto>>> GetRegistroRefeicoes()
        {
            var refeicoes = await _context.RegistroRefeicoes
                                 .Include(r => r.Colaborador.Funcao)
                                 .Include(r => r.Colaborador.Departamento)
                                 .OrderByDescending(r => r.DataHoraRegistro)
                                 .Select(r => new RefeicaoResponseDto
                                 {
                                     Id = r.Id,
                                     DataHoraRegistro = r.DataHoraRegistro,
                                     Colaborador = new ColaboradorRefeicaoDto
                                     {
                                         Id = r.Colaborador.Id,
                                         Nome = r.Colaborador.Nome,
                                         FuncaoNome = r.Colaborador.Funcao.Nome,
                                         DepartamentoNome = r.Colaborador.Departamento.Nome
                                     }
                                 })
                                 .ToListAsync();
            return Ok(refeicoes);
        }

        // GET: api/Refeicoes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RefeicaoResponseDto>> GetRegistroRefeicao(int id)
        {
            var refeicao = await _context.RegistroRefeicoes
                                     .Include(r => r.Colaborador.Funcao)
                                     .Include(r => r.Colaborador.Departamento)
                                     .Where(r => r.Id == id)
                                     .Select(r => new RefeicaoResponseDto
                                     {
                                         Id = r.Id,
                                         DataHoraRegistro = r.DataHoraRegistro,
                                         Colaborador = new ColaboradorRefeicaoDto
                                         {
                                             Id = r.Colaborador.Id,
                                             Nome = r.Colaborador.Nome,
                                             FuncaoNome = r.Colaborador.Funcao.Nome,
                                             DepartamentoNome = r.Colaborador.Departamento.Nome
                                         }
                                     })
                                     .FirstOrDefaultAsync();

            if (refeicao == null)
            {
                return NotFound();
            }

            return Ok(refeicao);
        }

        // POST: api/Refeicoes
        [HttpPost]
        public async Task<ActionResult<RegistroRefeicao>> PostRegistroRefeicao([FromBody] RegistroRefeicao registroRefeicao)
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

            // Retorna o objeto criado, mas sem causar referência circular na resposta
            var response = new { id = registroRefeicao.Id };
            return CreatedAtAction(nameof(GetRegistroRefeicao), response, response);
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