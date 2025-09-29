using Microsoft.AspNetCore.Mvc;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AzureDebugController : ControllerBase
    {
        private readonly FaceApiService _faceApiService;
        private readonly ILogger<AzureDebugController> _logger;

        public AzureDebugController(FaceApiService faceApiService, ILogger<AzureDebugController> logger)
        {
            _faceApiService = faceApiService;
            _logger = logger;
        }

        [HttpGet("training-status")]
        public async Task<IActionResult> GetTrainingStatus()
        {
            var status = await _faceApiService.GetTrainingStatusAsync();
            return Ok(status);
        }

        [HttpPost("ensure-group")]
        public async Task<IActionResult> EnsureGroup()
        {
            await _faceApiService.EnsurePersonGroupExistsAsync();
            return Ok("Grupo de pessoas verificado/criado com sucesso.");
        }

        [HttpPost("train-group")]
        public async Task<IActionResult> TrainGroup()
        {
            await _faceApiService.TrainPersonGroupAsync();
            return Ok("Treinamento do grupo iniciado.");
        }

        [HttpDelete("delete-group")]
        public async Task<IActionResult> DeleteGroup()
        {
            await _faceApiService.DeletePersonGroupAsync();
            return Ok("Grupo de pessoas deletado (se existia).");
        }

        [HttpPost("detect")]
        public async Task<IActionResult> Detect(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Arquivo não enviado.");

            using var stream = file.OpenReadStream();
            var faceId = await _faceApiService.DetectFace(stream);

            if (faceId != null)
                return Ok(new { faceId });

            return NotFound("Nenhuma face detetada.");
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify(IFormFile file1, IFormFile file2)
        {
            if (file1 == null || file2 == null) return BadRequest("Envie dois arquivos de imagem.");

            string faceId1, faceId2;

            using (var stream1 = file1.OpenReadStream())
            {
                faceId1 = await _faceApiService.DetectFaceAndGetId(stream1);
            }

            using (var stream2 = file2.OpenReadStream())
            {
                faceId2 = await _faceApiService.DetectFaceAndGetId(stream2);
            }

            if (string.IsNullOrEmpty(faceId1) || string.IsNullOrEmpty(faceId2))
            {
                return NotFound("Não foi possível detetar faces em uma ou ambas as imagens.");
            }

            var (isIdentical, confidence) = await _faceApiService.VerifyFaces(Guid.Parse(faceId1), Guid.Parse(faceId2));

            return Ok(new { isIdentical, confidence });
        }
    }
}