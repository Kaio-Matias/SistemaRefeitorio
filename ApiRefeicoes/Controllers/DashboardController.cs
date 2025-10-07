using ApiRefeicoes.Data;
using ApiRefeicoes.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Protege todo o controlador
    public class DashboardController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public DashboardController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);

            var totalColaboradores = await _context.Colaboradores
                .CountAsync(c => c.Ativo);

            var refeicoesHoje = await _context.RegistroRefeicoes
                .CountAsync(r => r.DataHoraRegistro >= hoje && r.DataHoraRegistro < amanha);

            var stats = new DashboardStatsDto
            {
                TotalColaboradoresAtivos = totalColaboradores,
                RefeicoesHoje = refeicoesHoje
            };

            return Ok(stats);
        }

        [HttpGet("registrosrecentes")]
        public async Task<ActionResult<RegistroRecenteDto>> GetRegistrosRecentes()
        {
            var registros = await _context.RegistroRefeicoes
                .Include(r => r.Colaborador)
                .ThenInclude(c => c.Departamento)
                .OrderByDescending(r => r.DataHoraRegistro)
                .Take(5)
                .Select(r => new RegistroRecenteDto
                {
                    ColaboradorNome = r.Colaborador.Nome,
                    DepartamentoNome = r.Colaborador.Departamento.Nome,
                    DataHoraRegistro = r.DataHoraRegistro
                })
                .ToListAsync();

            return Ok(registros);
        }
    }
}