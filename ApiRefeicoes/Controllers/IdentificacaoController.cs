using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class IdentificacaoController : ControllerBase
    {
        private readonly FaceApiService _faceApiService;
        private readonly ApiRefeicoesDbContext _context;
        private readonly ILogger<IdentificacaoController> _logger;
        private const double LimiarDeConfianca = 0.7; // Limiar de 50%

        public IdentificacaoController(FaceApiService faceApiService, ApiRefeicoesDbContext context, ILogger<IdentificacaoController> logger)
        {
            _faceApiService = faceApiService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("registrar-ponto")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarPonto([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhuma imagem foi enviada." });
            }

            try
            {
                await using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                var (faceIdTemporario, detectMessage) = await _faceApiService.DetectFaceWithFeedback(memoryStream);

                if (!faceIdTemporario.HasValue)
                {
                    return NotFound(new { Sucesso = false, Mensagem = detectMessage });
                }

                var colaboradores = await _context.Colaboradores
                                                  .Where(c => c.PersonId != null && c.Ativo)
                                                  .Include(c => c.Departamento)
                                                  .Include(c => c.Funcao)
                                                  .AsNoTracking()
                                                  .ToListAsync();

                if (!colaboradores.Any())
                {
                    _logger.LogWarning("Nenhum colaborador com cadastro facial ativo encontrado no banco de dados.");
                    return NotFound(new { Sucesso = false, Mensagem = "Nenhum colaborador com cadastro facial ativo encontrado." });
                }

                Colaborador colaboradorVerificado = null;

                foreach (var colaborador in colaboradores)
                {
                    _logger.LogInformation("Verificando se a face pertence ao colaborador: {Nome} (PersonId: {PersonId})", colaborador.Nome, colaborador.PersonId);

                    var (isIdentical, confidence) = await _faceApiService.VerifyFaceToPersonAsync(faceIdTemporario.Value, colaborador.PersonId.Value);

                    if (isIdentical && confidence > LimiarDeConfianca)
                    {
                        _logger.LogInformation("VERIFICAÇÃO BEM-SUCEDIDA: Colaborador {Nome} verificado com confiança de {Confidence}", colaborador.Nome, confidence);
                        colaboradorVerificado = colaborador;
                        break;
                    }
                }

                if (colaboradorVerificado == null)
                {
                    _logger.LogWarning("VERIFICAÇÃO FALHOU: Nenhuma correspondência encontrada para a face detectada.");
                    return NotFound(new { Sucesso = false, Mensagem = "Face não reconhecida no sistema." });
                }

                var tipoRefeicao = DeterminarTipoRefeicao();

                var registro = new RegistroRefeicao
                {
                    ColaboradorId = colaboradorVerificado.Id,
                    DataHoraRegistro = DateTime.UtcNow,
                    TipoRefeicao = tipoRefeicao,
                    NomeColaborador = colaboradorVerificado.Nome,
                    NomeDepartamento = colaboradorVerificado.Departamento?.Nome ?? "N/A",
                    DepartamentoGenerico = colaboradorVerificado.Departamento?.DepartamentoGenerico,
                    NomeFuncao = colaboradorVerificado.Funcao?.Nome ?? "N/A",
                    ValorRefeicao = 17,
                    ParadaDeFabrica = false
                };

                _context.RegistroRefeicoes.Add(registro);
                await _context.SaveChangesAsync();

                _logger.LogInformation("{TipoRefeicao} registrada para {Nome}", tipoRefeicao, colaboradorVerificado.Nome);
                return Ok(new { Sucesso = true, Mensagem = $"{tipoRefeicao} registrada com sucesso para {colaboradorVerificado.Nome}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao registrar ponto.");
                return StatusCode(500, new { Sucesso = false, Mensagem = "Ocorreu um erro interno no servidor." });
            }
        }

        private string DeterminarTipoRefeicao()
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                var horaLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Hour;

                // Lógica ajustada para cobrir 24 horas
                if (horaLocal >= 6 && horaLocal < 11) return "Café da Manhã"; // 06:00 - 10:59
                if (horaLocal >= 11 && horaLocal < 18) return "Almoço";       // 11:00 - 17:59
                if (horaLocal >= 18 && horaLocal < 22) return "Janta";        // 18:00 - 21:59

                // Todos os outros horários (22, 23, 0, 1, 2, 3, 4, 5) são considerados Ceia.
                return "Ceia";
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogError(ex, "Fuso horário 'E. South America Standard Time' não encontrado. Usando UTC como fallback.");
                var horaUtc = DateTime.UtcNow.Hour;

                // Lógica de fallback ajustada para cobrir 24 horas (considerando UTC-3)
                if (horaUtc >= 9 && horaUtc < 14) return "Café da Manhã";
                if (horaUtc >= 14 && horaUtc < 21) return "Almoço";
                if (horaUtc >= 21 || horaUtc < 1) return "Janta";

                return "Ceia";
            }
        }
    }
}