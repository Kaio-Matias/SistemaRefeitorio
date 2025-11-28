using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly IConfiguration _configuration;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public IdentificacaoController(
            FaceApiService faceApiService,
            ApiRefeicoesDbContext context,
            ILogger<IdentificacaoController> logger,
            IConfiguration configuration)
        {
            _faceApiService = faceApiService;
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        private enum TipoRefeicao { Café_da_Manha, Almoço, Janta, Ceia }

        private TipoRefeicao GetTipoRefeicaoAtual(DateTime horaRegistro)
        {
            var hora = horaRegistro.Hour;
            // Ajuste os horários conforme sua regra de negócio
            if (hora >= 4 && hora < 10) return TipoRefeicao.Café_da_Manha;
            if (hora >= 10 && hora < 15) return TipoRefeicao.Almoço;
            if (hora >= 15 && hora < 22) return TipoRefeicao.Janta;
            return TipoRefeicao.Ceia;
        }

        [HttpPost("registrar-ponto")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarPonto(IFormFile file, [FromForm] DateTime? dataHora)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhuma imagem foi enviada." });

            if (file.Length > MaxFileSize)
                return BadRequest(new { Sucesso = false, Mensagem = $"A imagem excede o limite de {MaxFileSize / 1024 / 1024} MB." });

            // Variável para armazenar os bytes da foto
            byte[] fotoBytes;

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            // --- NOVO: CAPTURA OS BYTES PARA SALVAR NO BANCO ---
            fotoBytes = memoryStream.ToArray();
            // ---------------------------------------------------

            memoryStream.Position = 0; // Reseta posição para o Azure ler

            // --- PASSO 1: IDENTIFICAÇÃO (1:N) ---
            var personId = await _faceApiService.IdentificarFaceAsync(memoryStream);

            if (personId == null)
            {
                _logger.LogWarning("IDENTIFICAÇÃO FALHOU: Face não reconhecida no grupo.");
                return Unauthorized(new { Sucesso = false, Mensagem = "Colaborador não reconhecido." });
            }

            // --- PASSO 2: BUSCA NO BANCO ---
            var colaborador = await _context.Colaboradores
                                            .Include(c => c.Departamento)
                                            .Include(c => c.Funcao)
                                            .FirstOrDefaultAsync(c => c.PersonId == personId && c.Ativo);

            if (colaborador == null)
            {
                _logger.LogError("PersonId {PersonId} existe no Azure mas não no banco local.", personId);
                return NotFound(new { Sucesso = false, Mensagem = "Cadastro local não encontrado." });
            }

            // --- PASSO 3: REGRAS DE NEGÓCIO ---
            DateTime horaRegistro;
            if (dataHora.HasValue)
            {
                horaRegistro = dataHora.Value;
                _logger.LogInformation("Usando data/hora do dispositivo: {DataHora}", horaRegistro);
            }
            else
            {
                horaRegistro = DateTime.UtcNow.AddHours(-3);
                _logger.LogWarning("Data/hora não enviada pelo dispositivo. Usando horário do servidor: {DataHora}", horaRegistro);
            }

            var tipoRefeicaoAtual = GetTipoRefeicaoAtual(horaRegistro);
            var dataHoje = horaRegistro.Date;

            var jaComeuHoje = await _context.RegistroRefeicoes
                .AnyAsync(r => r.ColaboradorId == colaborador.Id
                            && r.TipoRefeicao == tipoRefeicaoAtual.ToString()
                            && r.DataHoraRegistro >= dataHoje
                            && r.DataHoraRegistro < dataHoje.AddDays(1));

            bool gerarAlerta = jaComeuHoje;
            string mensagemRetorno = jaComeuHoje
                ? $"ALERTA: {tipoRefeicaoAtual} já registrada hoje."
                : $"{tipoRefeicaoAtual} registrada com sucesso.";

            // --- PASSO 4: REGISTRO (SEMPRE GRAVA) ---
            var valorRefeicao = _configuration.GetValue<decimal>("Configuracoes:ValorRefeicaoPadrao", 17m);

            var registro = new RegistroRefeicao
            {
                ColaboradorId = colaborador.Id,
                DataHoraRegistro = horaRegistro,
                TipoRefeicao = tipoRefeicaoAtual.ToString(),
                NomeColaborador = colaborador.Nome,
                NomeDepartamento = colaborador.Departamento?.Nome ?? "N/A",
                DepartamentoGenerico = colaborador.Departamento?.DepartamentoGenerico,
                NomeFuncao = colaborador.Funcao?.Nome ?? "N/A",
                ValorRefeicao = valorRefeicao,
                ParadaDeFabrica = false,
                RefeicaoExcedente = gerarAlerta,

                // --- NOVO: SALVA A FOTO ---
                FotoRegistro = fotoBytes
            };

            _context.RegistroRefeicoes.Add(registro);

            try
            {
                var linhasAfetadas = await _context.SaveChangesAsync();

                if (linhasAfetadas == 0)
                {
                    _logger.LogError("PERIGO: O Entity Framework disse que salvou, mas retornou 0 linhas afetadas!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO FATAL AO CHAMAR SaveChangesAsync()");
                throw;
            }

            if (gerarAlerta)
                _logger.LogWarning("Refeição EXCEDENTE registrada para {Nome}.", colaborador.Nome);
            else
                _logger.LogInformation("Sucesso: {Refeicao} registrada para {Nome}.", tipoRefeicaoAtual, colaborador.Nome);

            return Ok(new { Sucesso = true, Mensagem = mensagemRetorno, Alerta = gerarAlerta });
        }
    }
}