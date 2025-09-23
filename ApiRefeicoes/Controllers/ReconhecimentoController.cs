using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyFaces([FromForm] IFormFile file1, [FromForm] IFormFile file2)
        {
            if (file1 == null || file2 == null)
            {
                return BadRequest("Duas imagens são necessárias.");
            }

            using var memoryStream1 = new MemoryStream();
            await file1.CopyToAsync(memoryStream1);
            var imageBytes1 = memoryStream1.ToArray();

            using var memoryStream2 = new MemoryStream();
            await file2.CopyToAsync(memoryStream2);
            var imageBytes2 = memoryStream2.ToArray();

            var faceId1 = await _faceApiService.DetectFaceAndGetId(imageBytes1);
            var faceId2 = await _faceApiService.DetectFaceAndGetId(imageBytes2);

            if (string.IsNullOrEmpty(faceId1) || string.IsNullOrEmpty(faceId2))
            {
                return BadRequest("Não foi possível detectar faces em uma ou ambas as imagens.");
            }

            var (isIdentical, confidence) = await _faceApiService.VerifyFaces(faceId1, faceId2);

            return Ok(new { isIdentical, confidence });
        }

        // ==================================================================
        // INÍCIO DO NOVO CÓDIGO
        // ==================================================================
        [HttpPost("registrar-refeicao")]
        public async Task<IActionResult> RegistrarRefeicao(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhum arquivo de imagem enviado." });
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var azureId = await _faceApiService.DetectFaceAndGetId(imageBytes);

            if (string.IsNullOrEmpty(azureId))
            {
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhuma face detectada na imagem ou erro na API do Azure." });
            }

            var colaborador = await _context.Colaboradores.FirstOrDefaultAsync(c => c.AzureId == azureId);

            if (colaborador == null)
            {
                return NotFound(new { Sucesso = false, Mensagem = "Colaborador não encontrado com a face detectada." });
            }

            var registro = new RegistroRefeicao
            {
                ColaboradorId = colaborador.Id,
                HorarioRegistro = DateTime.Now,
                ValorRefeicao = 17
            };

            _context.RegistrosRefeicoes.Add(registro);
            await _context.SaveChangesAsync();

            return Ok(new { Sucesso = true, Mensagem = $"Refeição registrada com sucesso para o colaborador: {colaborador.Nome}" });
        }
        // ==================================================================
        // FIM DO NOVO CÓDIGO
        // ==================================================================
    }
}