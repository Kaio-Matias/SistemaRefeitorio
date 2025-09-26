using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReconhecimentoController : ControllerBase
    {
        private readonly FaceApiService _faceApiService;

        public ReconhecimentoController(FaceApiService faceApiService)
        {
            _faceApiService = faceApiService;
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

            var faceId1 = await _faceApiService.DetectFace(imageBytes1);
            var faceId2 = await _faceApiService.DetectFace(imageBytes2);

            if (faceId1 == null || faceId2 == null)
            {
                return BadRequest("Não foi possível detectar faces em uma ou ambas as imagens.");
            }

            var (isIdentical, confidence) = await _faceApiService.VerifyFaces(faceId1.Value, faceId2.Value);

            return Ok(new { isIdentical, confidence });
        }
    }
}