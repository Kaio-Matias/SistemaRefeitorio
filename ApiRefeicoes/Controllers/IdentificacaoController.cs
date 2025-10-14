using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IdentificacaoController : ControllerBase
    {
        private readonly FaceApiService _faceApiService;
        private readonly ApiRefeicoesDbContext _context;
        private readonly ILogger<IdentificacaoController> _logger;
        private const double LimiarDeConfianca = 0.75;

        public IdentificacaoController(FaceApiService faceApiService, ApiRefeicoesDbContext context, ILogger<IdentificacaoController> logger)
        {
            _faceApiService = faceApiService;
            _context = context;
            _logger = logger;
        }

        private enum TipoRefeicao { CafeDaManha, Almoco, Janta, Ceia }

        private DateTime GetHoraLocalBrasil()
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Nenhum fuso horário para o Brasil foi encontrado. Usando UTC-3 como fallback manual.");
                    return DateTime.UtcNow.AddHours(-3);
                }
            }
        }

        private TipoRefeicao GetTipoRefeicaoAtual(DateTime horaLocal)
        {
            var hora = horaLocal.Hour;
            if (hora >= 6 && hora < 11) return TipoRefeicao.CafeDaManha;
            if (hora >= 11 && hora < 18) return TipoRefeicao.Almoco;
            if (hora >= 18 && hora < 22) return TipoRefeicao.Janta;
            return TipoRefeicao.Ceia;
        }

        private async Task<List<Colaborador>> GetColaboradoresParaRefeicaoAsync(TipoRefeicao tipoRefeicao)
        {
            var query = _context.Colaboradores.Where(c => c.Ativo && c.PersonId.HasValue);

            switch (tipoRefeicao)
            {
                case TipoRefeicao.CafeDaManha:
                    query = query.Where(c => c.AcessoCafeDaManha);
                    break;
                case TipoRefeicao.Almoco:
                    query = query.Where(c => c.AcessoAlmoco);
                    break;
                case TipoRefeicao.Janta:
                    query = query.Where(c => c.AcessoJanta);
                    break;
                case TipoRefeicao.Ceia:
                    query = query.Where(c => c.AcessoCeia);
                    break;
            }
            return await query.AsNoTracking().ToListAsync();
        }

        [HttpPost("registrar-ponto")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarPonto(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhuma imagem foi enviada." });
            }

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var (faceIdTemporario, detectMessage) = await _faceApiService.DetectFaceWithFeedback(memoryStream);

            if (!faceIdTemporario.HasValue)
            {
                return BadRequest(new { Sucesso = false, Mensagem = detectMessage });
            }

            var horaLocal = GetHoraLocalBrasil();
            var tipoRefeicaoAtual = GetTipoRefeicaoAtual(horaLocal);

            _logger.LogInformation("Iniciando busca para a refeição: {TipoRefeicao}", tipoRefeicaoAtual);
            var colaboradoresCandidatos = await GetColaboradoresParaRefeicaoAsync(tipoRefeicaoAtual);

            if (!colaboradoresCandidatos.Any())
            {
                _logger.LogWarning("Nenhum colaborador ativo com permissão para a refeição {TipoRefeicao}", tipoRefeicaoAtual);
                return NotFound(new { Sucesso = false, Mensagem = "Nenhum colaborador ativo com permissão para esta refeição." });
            }

            _logger.LogInformation("Verificando face contra {Count} colaboradores candidatos.", colaboradoresCandidatos.Count);

            foreach (var colaborador in colaboradoresCandidatos)
            {
                var (isIdentical, confidence) = await _faceApiService.VerifyFaceToPersonAsync(faceIdTemporario.Value, colaborador.PersonId.Value);

                if (isIdentical && confidence >= LimiarDeConfianca)
                {
                    _logger.LogInformation("VERIFICAÇÃO BEM-SUCEDIDA: Colaborador {Nome} verificado com confiança de {Confidence}", colaborador.Nome, confidence);

                    var registro = new RegistroRefeicao
                    {
                        ColaboradorId = colaborador.Id,
                        DataHoraRegistro = horaLocal,
                        TipoRefeicao = tipoRefeicaoAtual.ToString(),
                        // Preencha aqui os outros campos do seu modelo `RegistroRefeicao`
                    };

                    _context.RegistroRefeicoes.Add(registro);
                    await _context.SaveChangesAsync();

                    return Ok(new { Sucesso = true, Mensagem = $"{tipoRefeicaoAtual} registrada com sucesso para {colaborador.Nome}." });
                }
            }

            _logger.LogWarning("VERIFICAÇÃO FALHOU: Nenhuma correspondência encontrada para a face detectada.");
            return Unauthorized(new { Sucesso = false, Mensagem = "Colaborador não reconhecido ou sem permissão para esta refeição." });
        }
    }
}