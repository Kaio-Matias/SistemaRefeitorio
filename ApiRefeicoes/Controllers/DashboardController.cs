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
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Descomente em produção para proteger o acesso
    public class DashboardController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public DashboardController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retorna as estatísticas principais para o Dashboard, incluindo alertas.
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            // Define o intervalo do dia atual (ajustado para UTC-3 Brasil se necessário)
            var hoje = DateTime.UtcNow.AddHours(-3).Date;
            var amanha = hoje.AddDays(1);

            // 1. Total de Colaboradores Ativos
            var totalColaboradores = await _context.Colaboradores
                .CountAsync(c => c.Ativo);

            // 2. Total de Refeições Hoje
            var refeicoesHoje = await _context.RegistroRefeicoes
                .CountAsync(r => r.DataHoraRegistro >= hoje && r.DataHoraRegistro < amanha);

            // 3. Alertas Pendentes
            // Conta registros onde RefeicaoExcedente é true E que NÃO possuem justificativa associada
            var alertasPendentes = await _context.RegistroRefeicoes
                .CountAsync(r => r.RefeicaoExcedente == true &&
                                 !_context.Justificativas.Any(j => j.RegistroRefeicaoId == r.Id));

            var stats = new DashboardStatsDto
            {
                TotalColaboradoresAtivos = totalColaboradores,
                RefeicoesHoje = refeicoesHoje,
                AlertasPendentes = alertasPendentes
            };

            return Ok(stats);
        }

        /// <summary>
        /// Retorna os últimos 5 registros de refeição para a tabela do Dashboard.
        /// </summary>
        [HttpGet("registrosrecentes")] // Atenção ao nome da rota, deve bater com o frontend
        public async Task<ActionResult<RegistroRecenteDto>> GetRegistrosRecentes()
        {
            var registros = await _context.RegistroRefeicoes
                .Include(r => r.Colaborador)
                    .ThenInclude(c => c.Departamento)
                .OrderByDescending(r => r.DataHoraRegistro)
                .Take(5)
                .Select(r => new RegistroRecenteDto
                {
                    ColaboradorNome = r.NomeColaborador, // Usa o snapshot salvo no registro
                    DepartamentoNome = r.NomeDepartamento,
                    DataHoraRegistro = r.DataHoraRegistro
                })
                .ToListAsync();

            return Ok(registros);
        }
    }
}