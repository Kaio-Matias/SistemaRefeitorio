using Microsoft.AspNetCore.Mvc;
using ApiRefeicoes.Services;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        [HttpGet("group-details")]
        public async Task<ActionResult<object>> GetGroupDetails()
        {
            try
            {
                var group = await _faceApiService.GetPersonGroupAsync();
                return Ok(new
                {
                    group.PersonGroupId,
                    group.Name,
                    group.RecognitionModel,
                    group.UserData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter detalhes do grupo.");
                return StatusCode(500, new { Message = "Ocorreu um erro ao obter detalhes do grupo.", Details = ex.Message });
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

        // --- INÍCIO DA CORREÇÃO ---
        [HttpGet("hard-reset")]
        public async Task<IActionResult> HardReset()
        {
            try
            {
                _logger.LogWarning("!!! INICIANDO HARD RESET DO PERSON GROUP !!!");

                // Passo 1: Apagar o grupo existente (se houver).
                await _faceApiService.DeletePersonGroupAsync();

                // Passo 2: Chamar EnsurePersonGroupExistsAsync que agora contém a lógica de criação correta.
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
        // --- FIM DA CORREÇÃO ---
    }
}