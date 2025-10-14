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

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReconhecimentoController : ControllerBase
    {
        private readonly FaceApiService _faceApiService;
        private readonly ApiRefeicoesDbContext _context;

        public ReconhecimentoController(FaceApiService faceApiService, ApiRefeicoesDbContext context)
        {
            _faceApiService = faceApiService;
            _context = context;
        }

        /// <summary>
        /// Identifica um colaborador a partir de uma foto, registra a refeição e mede o tempo da operação.
        /// </summary>
        [HttpPost("identificar")]
        public async Task<IActionResult> IdentificarEVerificarTempo([FromForm] IFormFile foto)
        {
            if (foto == null || foto.Length == 0)
            {
                return BadRequest("A foto é necessária.");
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                using var memoryStream = new MemoryStream();
                await foto.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

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

                var registroRefeicao = new RegistroRefeicao
                {
                    ColaboradorId = colaborador.Id,
                    DataHoraRegistro = DateTime.Now,
                    NomeColaborador = colaborador.Nome,
                    NomeDepartamento = colaborador.Departamento?.Nome,
                    NomeFuncao = colaborador.Funcao?.Nome,
                    TipoRefeicao = "Almoço", // Valor Padrão
                    ValorRefeicao = 0,     // Valor Padrão
                    ParadaDeFabrica = false // Valor Padrão
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
        /// <summary>
        /// Verifica se duas faces pertencem à mesma pessoa.
        /// </summary>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyFaces([FromForm] IFormFile file1, [FromForm] IFormFile file2)
        {
            if (file1 == null || file2 == null)
            {
                return BadRequest("Duas imagens são necessárias.");
            }

            using var memoryStream1 = new MemoryStream();
            await file1.CopyToAsync(memoryStream1);

            using var memoryStream2 = new MemoryStream();
            await file2.CopyToAsync(memoryStream2);

            var faceId1Str = await _faceApiService.DetectFaceAndGetId(memoryStream1);
            var faceId2Str = await _faceApiService.DetectFaceAndGetId(memoryStream2);

            if (string.IsNullOrEmpty(faceId1Str) || string.IsNullOrEmpty(faceId2Str))
            {
                return BadRequest("Não foi possível detectar faces em uma ou ambas as imagens.");
            }

            if (!Guid.TryParse(faceId1Str, out Guid faceId1Guid) || !Guid.TryParse(faceId2Str, out Guid faceId2Guid))
            {
                return BadRequest("Os IDs de face retornados não são válidos.");
            }

            var (isIdentical, confidence) = await _faceApiService.VerifyFaces(faceId1Guid, faceId2Guid);

            return Ok(new { isIdentical, confidence });
        }
    }
}