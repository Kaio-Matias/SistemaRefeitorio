using ApiRefeicoes.Data;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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

            var faceId1Str = await _faceApiService.DetectFaceAndGetId(imageBytes1);
            var faceId2Str = await _faceApiService.DetectFaceAndGetId(imageBytes2);

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