using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReconhecimentoController : ControllerBase
    {
        private readonly FaceApiService _faceApiService;
        private readonly ApiRefeicoesDbContext _context;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public ReconhecimentoController(FaceApiService faceApiService, ApiRefeicoesDbContext context)
        {
            _faceApiService = faceApiService;
            _context = context;
        }

        /// <summary>
        /// Identifica um colaborador a partir de uma foto, registra a refeição e mede o tempo da operação.
        /// </summary>
        [HttpPost("identificar")]
        [AllowAnonymous]
        public async Task<IActionResult> IdentificarEVerificarTempo([FromForm] IFormFile foto)
        {
            if (foto == null || foto.Length == 0)
            {
                return BadRequest("A foto é necessária.");
            }

            if (foto.Length > MaxFileSize)
            {
                return BadRequest($"A foto não pode exceder {MaxFileSize / 1024 / 1024} MB.");
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                using var memoryStream = new MemoryStream();
                await foto.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Usa o método 1:N atualizado
                var personId = await _faceApiService.IdentificarFaceAsync(memoryStream);

                if (personId == null)
                {
                    stopwatch.Stop();
                    return NotFound(new { message = "Colaborador não encontrado.", tempoDecorridoMs = stopwatch.ElapsedMilliseconds });
                }

                var colaborador = await _context.Colaboradores
                                                .Include(c => c.Funcao)
                                                .Include(c => c.Departamento)
                                                .FirstOrDefaultAsync(c => c.PersonId == personId);

                if (colaborador == null)
                {
                    stopwatch.Stop();
                    return NotFound(new { message = "Colaborador encontrado no Azure, mas não no banco de dados local.", tempoDecorridoMs = stopwatch.ElapsedMilliseconds });
                }

                // Cria registro simples para teste de performance
                var registroRefeicao = new RegistroRefeicao
                {
                    ColaboradorId = colaborador.Id,
                    DataHoraRegistro = DateTime.Now,
                    NomeColaborador = colaborador.Nome,
                    NomeDepartamento = colaborador.Departamento?.Nome,
                    NomeFuncao = colaborador.Funcao?.Nome,
                    TipoRefeicao = "Almoço", // Padrão para este endpoint de teste
                    ValorRefeicao = 0,
                    ParadaDeFabrica = false
                };
                _context.RegistroRefeicoes.Add(registroRefeicao);
                await _context.SaveChangesAsync();

                stopwatch.Stop();

                var resultado = new
                {
                    colaborador = new
                    {
                        id = colaborador.Id,
                        nome = colaborador.Nome,
                        cartaoPonto = colaborador.CartaoPonto,
                        funcao = colaborador.Funcao?.Nome,
                        departamento = colaborador.Departamento?.Nome
                    },
                    tempoDecorridoMs = stopwatch.ElapsedMilliseconds
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return StatusCode(500, new { message = $"Ocorreu um erro: {ex.Message}", tempoDecorridoMs = stopwatch.ElapsedMilliseconds });
            }
        }

        /// <summary>
        /// Endpoint para acionar manualmente o treinamento do grupo de pessoas no Azure.
        /// </summary>
        [HttpPost("treinar-grupo")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TreinarGrupo()
        {
            try
            {
                await _faceApiService.TrainPersonGroupAsync();
                return Ok("O treinamento do grupo de pessoas foi iniciado e concluído com sucesso.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocorreu um erro ao treinar o grupo: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica o status atual do treinamento do PersonGroup.
        /// </summary>
        [HttpGet("status-treinamento")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerificarStatusTreinamento()
        {
            try
            {
                var status = await _faceApiService.GetTrainingStatusAsync();
                return Ok(new
                {
                    status = status.Status.ToString(),
                    criadoEm = status.Created,
                    ultimaAcao = status.LastAction,
                    mensagem = status.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocorreu um erro ao verificar o status: {ex.Message}");
            }
        }

        // O endpoint "verify" (1:1) foi REMOVIDO para corrigir os erros CS1061 e CS8130
    }
}