using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
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
                using var stream = file.OpenReadStream();
                var faceIdTemporario = await _faceApiService.DetectFace(stream);

                if (!faceIdTemporario.HasValue)
                {
                    return NotFound(new { Sucesso = false, Mensagem = "Nenhuma face foi detectada." });
                }

                var personIdIdentificado = await _faceApiService.IdentifyFaceAsync(faceIdTemporario.Value);

                if (!personIdIdentificado.HasValue)
                {
                    return NotFound(new { Sucesso = false, Mensagem = "Face não reconhecida no sistema." });
                }

                var colaborador = await _context.Colaboradores
                    .Include(c => c.Departamento)
                    .Include(c => c.Funcao)
                    .FirstOrDefaultAsync(c => c.PersonId == personIdIdentificado.Value);

                if (colaborador == null)
                {
                    _logger.LogError("PersonId {personId} identificado, mas não encontrado no BD.", personIdIdentificado.Value);
                    return NotFound(new { Sucesso = false, Mensagem = "Colaborador não encontrado no banco de dados." });
                }

                if (!colaborador.Ativo)
                {
                    _logger.LogWarning("Tentativa de registro negada para colaborador inativo: {Nome}", colaborador.Nome);
                    return StatusCode(403, new { Sucesso = false, Mensagem = $"Acesso negado. O colaborador {colaborador.Nome} está inativo." });
                }

                var tipoRefeicao = DeterminarTipoRefeicao();
                if (string.IsNullOrEmpty(tipoRefeicao))
                {
                    return BadRequest(new { Sucesso = false, Mensagem = "Fora do horário de refeição." });
                }

                // Popula os novos campos, incluindo o DepartamentoGenerico
                var registro = new RegistroRefeicao
                {
                    ColaboradorId = colaborador.Id,
                    DataHoraRegistro = DateTime.UtcNow,
                    TipoRefeicao = tipoRefeicao,
                    NomeColaborador = colaborador.Nome,
                    NomeDepartamento = colaborador.Departamento?.Nome ?? "N/A",
                    DepartamentoGenerico = colaborador.Departamento?.DepartamentoGenerico, // Adicionado
                    NomeFuncao = colaborador.Funcao?.Nome ?? "N/A",
                    ValorRefeicao = 0,
                    ParadaDeFabrica = false
                };

                _context.RegistroRefeicoes.Add(registro);
                await _context.SaveChangesAsync();

                _logger.LogInformation("{TipoRefeicao} registrada para {Nome}", tipoRefeicao, colaborador.Nome);
                return Ok(new { Sucesso = true, Mensagem = $"{tipoRefeicao} registrada com sucesso para {colaborador.Nome}." });
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

                if (horaLocal >= 6 && horaLocal < 8) return "Café da Manhã";
                if (horaLocal >= 11 && horaLocal < 14) return "Almoço";
                if (horaLocal >= 18 && horaLocal < 20) return "Janta";
                if (horaLocal >= 22 || horaLocal < 1) return "Ceia";

                return null;
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogError(ex, "Fuso horário 'E. South America Standard Time' não encontrado. Usando UTC como fallback.");
                var horaUtc = DateTime.UtcNow.Hour;

                if (horaUtc >= 9 && horaUtc < 11) return "Café da Manhã";
                if (horaUtc >= 14 && horaUtc < 17) return "Almoço";

                return "Horário Indeterminado (UTC)";
            }
        }
    }
}