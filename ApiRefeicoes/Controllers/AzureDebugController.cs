using Microsoft.AspNetCore.Mvc;
using ApiRefeicoes.Services;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

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
        public async Task<ActionResult<object>> GetTrainingStatus()
        {
            try
            {
                var status = await _faceApiService.GetTrainingStatusAsync();
                return Ok(new
                {
                    Status = status.Status.ToString(),
                    Created = status.Created,
                    LastAction = status.LastAction,
                    Message = status.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao verificar o estado do treino.");
                return StatusCode(500, "Ocorreu um erro interno. Verifique os logs do servidor.");
            }
        }

        [HttpGet("force-train")]
        public async Task<IActionResult> ForceTrain()
        {
            try
            {
                _logger.LogInformation("A forçar o treino do grupo de pessoas manualmente.");
                await _faceApiService.TrainPersonGroupAsync();
                return Ok(new { Message = "Comando de treino enviado com sucesso. Verifique o estado em alguns segundos." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao forçar o treino.");
                return StatusCode(500, new { Message = "Ocorreu um erro ao tentar iniciar o treino.", Details = ex.Message });
            }
        }

        // O método HardReset agora chama EnsurePersonGroupExistsAsync diretamente,
        // o que é mais seguro do que chamar GetTrainingStatusAsync para um grupo novo.
        [HttpGet("hard-reset")]
        public async Task<IActionResult> HardReset()
        {
            try
            {
                _logger.LogWarning("!!! INICIANDO HARD RESET DO PERSON GROUP !!!");
                await _faceApiService.DeletePersonGroupAsync();
                // Apenas garante que o grupo seja recriado.
                await _faceApiService.EnsurePersonGroupExistsAsync();
                _logger.LogInformation("!!! HARD RESET CONCLUÍDO. O GRUPO FOI APAGADO E RECRIADO. !!!");
                return Ok(new { Message = "Hard Reset concluído. O grupo 'colaboradores' foi apagado e recriado. Por favor, cadastre um novo colaborador para iniciar o treino." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o Hard Reset.");
                return StatusCode(500, new { Message = "Ocorreu um erro durante o Hard Reset.", Details = ex.Message });
            }
        }

        [HttpPost("verify-faces")]
        public async Task<ActionResult<object>> VerifyFaces([FromForm] IFormFile fotoCadastro, [FromForm] IFormFile fotoApp)
        {
            if (fotoCadastro == null || fotoApp == null)
            {
                return BadRequest("É necessário enviar duas imagens: 'fotoCadastro' e 'fotoApp'.");
            }

            try
            {
                using var ms1 = new MemoryStream();
                await fotoCadastro.CopyToAsync(ms1);
                var bytesCadastro = ms1.ToArray();

                using var ms2 = new MemoryStream();
                await fotoApp.CopyToAsync(ms2);
                var bytesApp = ms2.ToArray();

                var faceIdCadastroStr = await _faceApiService.DetectFaceAndGetId(bytesCadastro, bypassIdentify: true);
                var faceIdAppStr = await _faceApiService.DetectFaceAndGetId(bytesApp, bypassIdentify: true);

                if (string.IsNullOrEmpty(faceIdCadastroStr) || string.IsNullOrEmpty(faceIdAppStr))
                {
                    return BadRequest("Não foi possível detetar um rosto em uma ou ambas as imagens.");
                }

                Guid.TryParse(faceIdCadastroStr, out Guid faceIdCadastro);
                Guid.TryParse(faceIdAppStr, out Guid faceIdApp);

                var (isIdentical, confidence) = await _faceApiService.VerifyFaces(faceIdCadastro, faceIdApp);

                return Ok(new
                {
                    IsIdentical = isIdentical,
                    Confidence = confidence,
                    Message = isIdentical
                        ? "As faces correspondem! O problema é 100% o estado do treino do Grupo de Pessoas."
                        : "As faces não correspondem. Verifique a qualidade de ambas as imagens."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar faces.");
                return StatusCode(500, new { Message = "Ocorreu um erro interno durante a verificação.", Details = ex.Message });
            }
        }
    }
}