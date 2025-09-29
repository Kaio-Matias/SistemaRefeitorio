using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                    .FirstOrDefaultAsync(c => c.PersonId == personIdIdentificado.Value);

                if (colaborador == null)
                {
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

                // --- INÍCIO DA CORREÇÃO ---
                var registro = new RegistroRefeicao
                {
                    ColaboradorId = colaborador.Id,
                    // Corrigido para 'DataHoraRegistro' conforme o novo modelo (CS0117)
                    DataHoraRegistro = DateTime.Now,
                    TipoRefeicao = tipoRefeicao,
                    // Valores padrão para os novos campos
                    ValorRefeicao = 0,
                    ParadaDeFabrica = false
                };
                // --- FIM DA CORREÇÃO ---

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
            var horaAtual = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")).Hour;

            if (horaAtual >= 6 && horaAtual < 8) return "Café da Manhã";
            if (horaAtual >= 11 && horaAtual < 14) return "Almoço";
            if (horaAtual >= 18 && horaAtual < 20) return "Janta";
            if (horaAtual >= 22 && horaAtual < 24) return "Ceia";

            return null;
        }
    }
}