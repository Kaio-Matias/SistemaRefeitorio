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
        // private const double LimiarDeConfianca = 0.75; // REMOVIDO - Lógica de verificação 1:1 não é mais usada
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

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

        // REMOVIDO - A lógica agora identifica primeiro e depois verifica a permissão
        // private async Task<List<Colaborador>> GetColaboradoresParaRefeicaoAsync(TipoRefeicao tipoRefeicao) { ... }

        // --- INÍCIO DA LÓGICA DE VERIFICAÇÃO DE PERMISSÃO (NOVA) ---
        private bool ColaboradorTemPermissao(Colaborador colaborador, TipoRefeicao tipoRefeicao)
        {
            switch (tipoRefeicao)
            {
                case TipoRefeicao.CafeDaManha:
                    return colaborador.AcessoCafeDaManha;
                case TipoRefeicao.Almoco:
                    return colaborador.AcessoAlmoco;
                case TipoRefeicao.Janta:
                    return colaborador.AcessoJanta;
                case TipoRefeicao.Ceia:
                    return colaborador.AcessoCeia;
                default:
                    return false;
            }
        }
        // --- FIM DA LÓGICA DE VERIFICAÇÃO DE PERMISSÃO (NOVA) ---


        [HttpPost("registrar-ponto")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarPonto(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhuma imagem foi enviada." });
            }

            // ADICIONADO - Verificação de tamanho de arquivo
            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { Sucesso = false, Mensagem = $"A imagem não pode exceder {MaxFileSize / 1024 / 1024} MB." });
            }

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // --- INÍCIO DA LÓGICA OTIMIZADA (1:N) ---

            // Passo 1: Detectar e Identificar a face (1:N)
            // Isso chama a API do Azure uma única vez e compara com o grupo de pessoas
            var personId = await _faceApiService.IdentificarFaceAsync(memoryStream);

            if (personId == null)
            {
                _logger.LogWarning("IDENTIFICAÇÃO 1:N FALHOU: Nenhuma correspondência encontrada no grupo.");
                return Unauthorized(new { Sucesso = false, Mensagem = "Colaborador não reconhecido." });
            }

            _logger.LogInformation("IDENTIFICAÇÃO 1:N SUCESSO: PersonId encontrado: {PersonId}", personId);

            // Passo 2: Buscar o colaborador no banco de dados local
            var colaborador = await _context.Colaboradores
                                    .Include(c => c.Departamento) // Inclui dados para o registro
                                    .Include(c => c.Funcao)     // Inclui dados para o registro
                                    .FirstOrDefaultAsync(c => c.PersonId == personId && c.Ativo);

            if (colaborador == null)
            {
                _logger.LogError("Colaborador com PersonId {PersonId} foi encontrado no Azure, mas não existe ou está inativo no banco de dados local.", personId);
                return NotFound(new { Sucesso = false, Mensagem = "Colaborador não encontrado no sistema." });
            }

            // Passo 3: Verificar a hora local e o tipo de refeição
            var horaLocal = GetHoraLocalBrasil();
            var tipoRefeicaoAtual = GetTipoRefeicaoAtual(horaLocal);

            // Passo 4: Verificar se o colaborador identificado tem permissão para esta refeição
            if (!ColaboradorTemPermissao(colaborador, tipoRefeicaoAtual))
            {
                _logger.LogWarning("Colaborador {Nome} (ID: {Id}) identificado, mas não possui permissão para {TipoRefeicao}.", colaborador.Nome, colaborador.Id, tipoRefeicaoAtual);
                return Unauthorized(new { Sucesso = false, Mensagem = $"Colaborador {colaborador.Nome} não tem permissão para {tipoRefeicaoAtual}." });
            }

            _logger.LogInformation("PERMISSÃO CONCEDIDA: Colaborador {Nome} autorizado para {TipoRefeicao}.", colaborador.Nome, tipoRefeicaoAtual);

            // Passo 5: Registrar a refeição
            var registro = new RegistroRefeicao
            {
                ColaboradorId = colaborador.Id,
                DataHoraRegistro = horaLocal,
                TipoRefeicao = tipoRefeicaoAtual.ToString(),
                NomeColaborador = colaborador.Nome,
                NomeDepartamento = colaborador.Departamento?.Nome ?? "N/A",
                DepartamentoGenerico = colaborador.Departamento?.DepartamentoGenerico,
                NomeFuncao = colaborador.Funcao?.Nome ?? "N/A",
                ValorRefeicao = 0, // TODO: Buscar de uma configuração
                ParadaDeFabrica = false
            };

            _context.RegistroRefeicoes.Add(registro);
            await _context.SaveChangesAsync();

            return Ok(new { Sucesso = true, Mensagem = $"{tipoRefeicaoAtual} registrada com sucesso para {colaborador.Nome}." });

            // --- FIM DA LÓGICA OTIMIZADA (1:N) ---
        }
    }
}