using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Services;

namespace ApiRefeicoes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReconhecimentoController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;
        private readonly FaceApiService _faceApiService;

        public ReconhecimentoController(ApiRefeicoesDbContext context, FaceApiService faceApiService)
        {
            _context = context;
            _faceApiService = faceApiService;
        }

        [HttpPost("verificar")]
        public async Task<IActionResult> VerificarColaborador([FromForm] ReconhecimentoDto reconhecimentoDto)
        {
            // 1. Validar a requisição
            if (reconhecimentoDto.FotoFile == null || reconhecimentoDto.FotoFile.Length == 0)
            {
                return BadRequest("Nenhuma foto foi enviada.");
            }

            // 2. Encontrar o colaborador pelo cartão de ponto
            var colaborador = await _context.Colaboradores
                .FirstOrDefaultAsync(c => c.CartaoPonto == reconhecimentoDto.CartaoPonto);

            if (colaborador == null)
            {
                return NotFound("Colaborador não encontrado com este cartão de ponto.");
            }

            if (colaborador.Foto == null || colaborador.Foto.Length == 0)
            {
                return BadRequest("Colaborador não possui foto de cadastro para comparação.");
            }

            // 3. Converter a foto recebida para bytes
            using var memoryStream = new MemoryStream();
            await reconhecimentoDto.FotoFile.CopyToAsync(memoryStream);
            var fotoRecebidaBytes = memoryStream.ToArray();

            // 4. Detectar as faces em ambas as imagens para obter os faceIds
            var faceIdFotoRecebida = await _faceApiService.DetectFaceAndGetId(fotoRecebidaBytes);
            if (faceIdFotoRecebida == null)
            {
                return BadRequest("Não foi possível detectar um rosto na foto enviada.");
            }

            var faceIdFotoCadastro = await _faceApiService.DetectFaceAndGetId(colaborador.Foto);
            if (faceIdFotoCadastro == null)
            {
                return BadRequest("Não foi possível detectar um rosto na foto de cadastro do colaborador.");
            }

            // 5. Comparar as duas faces usando o serviço da Azure
            var (isIdentical, confidence) = await _faceApiService.VerifyFaces(faceIdFotoRecebida, faceIdFotoCadastro);

            // 6. Retornar o resultado
            if (isIdentical)
            {
                // Aqui você pode adicionar a lógica para registrar a refeição
                return Ok(new
                {
                    message = "Verificação facial bem-sucedida!",
                    colaborador = colaborador.Nome,
                    confianca = confidence
                });
            }
            else
            {
                return Unauthorized(new
                {
                    message = "Reconhecimento facial falhou. As faces não correspondem.",
                    confianca = confidence
                });
            }
        }
    }

    // DTO para receber os dados do app .NET MAUI
    public class ReconhecimentoDto
    {
        public string CartaoPonto { get; set; }
        public IFormFile FotoFile { get; set; }
    }
}